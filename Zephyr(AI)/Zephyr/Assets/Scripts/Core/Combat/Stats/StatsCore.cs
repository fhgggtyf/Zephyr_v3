/*
 * StatsCore.cs
 * ------------
 * Module:  Core / Stats
 * Purpose: Central stat container MonoBehaviour implementing IStatSource. Manages stat values
 *          with five gameplay layers: PlayerBaseStats, MetaUpgrades, RunGains,
 *          TimedModifiers, and SceneModifiers.
 *
 *          ==== LIFETIME MODEL ====
 *
 *          [Profile Lifetime — permanent, never reset]
 *          Layer 1 — PlayerBaseStats:
 *              SetBase() stores the new player's permanent value before Meta upgrades.
 *          Layer 2 — MetaUpgrades:
 *              AddMetaBase()/AddMetaBasePercent() stores purchased permanent additions.
 *          Layer 1b — MetaStatType values:
 *              SetMetaStat() / AddMetaStat() → non-Potential permanent stats
 *              (crit chance, piercing, max shield, etc.). Never reset.
 *
 *          [Run Lifetime — reset at run start]
 *          Potential — per-stat Potential values:
 *              InitializeRunPotentials() sets each stat's Potential for this run.
 *              Derived from the player's total Potential budget (MetaUpgradeTarget.PotentialBudget)
 *              — some chosen by player, rest randomized. Re-rolled every run.
 *          Layer 3 — In-Run Gains (Potential-gated):
 *              ApplyPotentialScaledGain() → relics, weapons, room rewards.
 *              actualGain = rawGain × (Potential / PotentialMax).
 *          Layer 4 — Timed Modifiers:
 *              AddModifier() / RemoveModifier() → room events, status effects.
 *          Scene modifiers are applied last. Category multipliers are separate:
 *              SetCategoryMultiplier() = meta base (permanent, Layer 1)
 *              AddCategoryMultiplier() = in-run addend (run lifetime, Layer 2)
 *
 *          [Temporary Lifetime — timer/condition expiry]
 *          Layer 4 — Temporary Modifiers:
 *              AddTemporaryModifier() / RemoveTemporaryModifier() → short-lived buffs.
 *              Expiry managed by external systems (Timer, StatusEffectManager).
 *
 *          ==== FORMULA ====
 *          additive = Base + MetaFlat + RunFlat + TimedFlat + SceneFlat
 *          final = additive * (1 + MetaPercent + RunPercent + TimedPercent + ScenePercent)
 *          Scene Override returns its value directly without recalculating lower layers.
 *
 *          Run-local layers are cleared by ResetForRunStart() at the start of each run.
 *          After ResetForRunStart(), call InitializeRunPotentials() to set the run's
 *          per-stat Potential values from the player's total budget.
 * Dependencies: IStatSource (interface), StatRecord (value type), StatType, MetaStatType,
 *               StatConstants (tuning values).
 * Scene:    GameManager (attached to player and enemy prefabs).
 * Ch.Ref:   Ch.5 Stat System, Ch.5.3 Potential Mechanic, Ch.5.10 Luck Mechanic,
 *           Ch.2.8 Damage Calculation Pipeline, Ch.16 Meta Progression.
 */
using System.Collections.Generic;
using UnityEngine;
using Zephyr.Core;
using Zephyr.Core.DamageSystem;
using Zephyr.Contracts.Damage;
using Zephyr.Contracts.Interfaces;
using Zephyr.Core.Runtime.Stats;

namespace Zephyr.Core.Stats
{
    /// <summary>
    /// Core stats container. Implements IStatSource.
    ///
    /// Lifetime layers (in order of persistence):
    ///   Profile:  Base values (StatRecord.Base), MetaStatType, category meta bases
    ///   Run:      Per-stat Potential, In-Run Gains, In-Run Modifiers, category addends
    ///   Temp:     Temporary Modifiers (timer/condition expiry)
    ///
    /// Potential is a per-run value (re-rolled each run from total budget).
    /// The total Potential budget is stored in MetaData.PotentialBudgetUpgrades (permanent).
    /// Per-stat Potential values are set via InitializeRunPotentials() at run start.
    /// </summary>
    public class StatsCore : MonoBehaviour, IStatSource, IMetaStatSource, IStatModifierTarget
    {
        [Header("Runtime identity")]
        [SerializeField] private StatsOwnerRole _ownerRole = StatsOwnerRole.Unknown;

        public StatsOwnerRole OwnerRole => _ownerRole;

        public void SetOwnerRole(StatsOwnerRole role)
        {
            _ownerRole = role;
        }
        // StatRecord holds PlayerBaseStats Base plus Potential for the current run.
        private readonly Dictionary<StatType, StatRecord> _stats = new Dictionary<StatType, StatRecord>();

        // Permanent Meta additions to core stats, separate from PlayerBaseStats.
        private readonly Dictionary<StatType, float> _metaBaseAdds = new Dictionary<StatType, float>();
        private readonly Dictionary<StatType, float> _metaBasePercents = new Dictionary<StatType, float>();

        // Run-lifetime additive and percentage layers.
        private readonly Dictionary<StatType, float> _runGains = new Dictionary<StatType, float>();
        private readonly Dictionary<StatType, float> _runGainPercents = new Dictionary<StatType, float>();
        private readonly Dictionary<StatType, float> _modifiers = new Dictionary<StatType, float>();
        private readonly Dictionary<StatType, float> _modifierPercents = new Dictionary<StatType, float>();
        private readonly Dictionary<StatType, float> _tempModifiers = new Dictionary<StatType, float>();
        private readonly Dictionary<StatType, float> _temporaryModifierPercents = new Dictionary<StatType, float>();

        // Meta-only stats (profile lifetime, no Potential).
        private readonly Dictionary<MetaStatType, float> _metaStats = new Dictionary<MetaStatType, float>();

        // Category multipliers: meta base (permanent) + in-run addends (run lifetime).
        private readonly Dictionary<string, float> _categoryMetaBases = new Dictionary<string, float>();
        private readonly Dictionary<string, float> _categoryInRunAddends = new Dictionary<string, float>();
        private bool _isInitialized;

        private void Awake()
        {
            InitializeDefaults();
        }

        private void InitializeDefaults()
        {
            if (_isInitialized) return;

            for (int i = 0; i < System.Enum.GetValues(typeof(StatType)).Length; i++)
            {
                var type = (StatType)i;
                _stats[type] = new StatRecord { Base = 0f, Potential = 0f };
                _runGains[type] = 0f;
                _modifiers[type] = 0f;
                _tempModifiers[type] = 0f;
            }

            for (int i = 0; i < System.Enum.GetValues(typeof(MetaStatType)).Length; i++)
            {
                _metaStats[(MetaStatType)i] = 0f;
            }

            _isInitialized = true;
        }

        /// <summary>
        /// Total stat value = Base + Meta + Run + Timed, with flat values summed before
        /// percentage values. Scene modifiers are applied last; an Override short-circuits.
        /// Single source of truth for damage calculation (Ch.2.8).
        /// </summary>
        public float GetStatValue(StatType statType)
        {
            if (!_stats.ContainsKey(statType)) return 0f;
            if (StatModifierRegistry.TryGetOverride(this, StatModifierTargetKind.CoreStat, statType, default, null, out float overrideValue))
                return overrideValue;

            float additive = _stats[statType].Base
                + _metaBaseAdds.GetValueOrDefault(statType, 0f)
                + _runGains.GetValueOrDefault(statType, 0f)
                + _modifiers.GetValueOrDefault(statType, 0f)
                + _tempModifiers.GetValueOrDefault(statType, 0f);
            float percent = _metaBasePercents.GetValueOrDefault(statType, 0f)
                + _runGainPercents.GetValueOrDefault(statType, 0f)
                + _modifierPercents.GetValueOrDefault(statType, 0f)
                + _temporaryModifierPercents.GetValueOrDefault(statType, 0f);
            StatModifierRegistry.Collect(this, StatModifierTargetKind.CoreStat, statType, default, null,
                out float sceneAdditive, out float scenePercent);
            return (additive + sceneAdditive) * (1f + percent + scenePercent);
        }

        /// <summary>
        /// Get the current run's Potential for a stat. Re-rolled each run via
        /// InitializeRunPotentials(). Range: 0 to StatConstants.PotentialMax (10).
        /// </summary>
        public float GetPotential(StatType statType)
        {
            if (!_stats.ContainsKey(statType)) return 0f;
            return _stats[statType].Potential;
        }

        public RuntimeState CaptureRuntimeState()
        {
            var state = new RuntimeState();
            foreach (StatType type in System.Enum.GetValues(typeof(StatType)))
                state.Potentials[type] = _stats.GetValueOrDefault(type).Potential;

            CopyValues(_runGains, state.RunGains);
            CopyValues(_runGainPercents, state.RunGainPercents);
            CopyValues(_modifiers, state.Modifiers);
            CopyValues(_modifierPercents, state.ModifierPercents);
            CopyValues(_categoryInRunAddends, state.CategoryAddends);
            return state;
        }

        public void RestoreRuntimeState(RuntimeState state)
        {
            if (state == null) return;

            foreach (StatType type in System.Enum.GetValues(typeof(StatType)))
            {
                var record = _stats.GetValueOrDefault(type);
                record.Potential = Mathf.Clamp(state.Potentials.GetValueOrDefault(type), 0f,
                    StatConstants.PotentialMax);
                _stats[type] = record;
            }

            ReplaceValues(_runGains, state.RunGains);
            ReplaceValues(_runGainPercents, state.RunGainPercents);
            ReplaceValues(_modifiers, state.Modifiers);
            ReplaceValues(_modifierPercents, state.ModifierPercents);
            ReplaceValues(_categoryInRunAddends, state.CategoryAddends);
        }

        private static void CopyValues<TKey>(Dictionary<TKey, float> source,
            Dictionary<TKey, float> destination)
        {
            foreach (KeyValuePair<TKey, float> entry in source)
                destination[entry.Key] = entry.Value;
        }

        private static void ReplaceValues<TKey>(Dictionary<TKey, float> destination,
            Dictionary<TKey, float> source)
        {
            destination.Clear();
            CopyValues(source, destination);
        }

        public sealed class RuntimeState
        {
            internal readonly Dictionary<StatType, float> Potentials = new Dictionary<StatType, float>();
            internal readonly Dictionary<StatType, float> RunGains = new Dictionary<StatType, float>();
            internal readonly Dictionary<StatType, float> RunGainPercents = new Dictionary<StatType, float>();
            internal readonly Dictionary<StatType, float> Modifiers = new Dictionary<StatType, float>();
            internal readonly Dictionary<StatType, float> ModifierPercents = new Dictionary<StatType, float>();
            internal readonly Dictionary<string, float> CategoryAddends = new Dictionary<string, float>();
        }

        #region Permanent Base (profile lifetime, never reset)

        /// <summary>
        /// Set the permanent base value for a stat. Called by meta upgrades.
        /// Persists across runs — never reset.
        /// </summary>
        public void SetBase(StatType statType, float value)
        {
            InitializeDefaults();
            if (!_stats.ContainsKey(statType))
                _stats[statType] = new StatRecord();
            var record = _stats[statType];
            record.Base = value;
            _stats[statType] = record;
        }

        /// <summary>
        /// Get the permanent base value for a stat (excluding run-gains, modifiers, etc.).
        /// Useful for meta upgrade calculations that need to know the underlying value.
        /// </summary>
        public float GetMetaBaseAdd(StatType statType) => _metaBaseAdds.GetValueOrDefault(statType, 0f);
        public float GetMetaBasePercent(StatType statType) => _metaBasePercents.GetValueOrDefault(statType, 0f);

        public void AddMetaBase(StatType statType, float value)
        {
            _metaBaseAdds[statType] = _metaBaseAdds.GetValueOrDefault(statType, 0f) + value;
        }

        public void AddMetaBasePercent(StatType statType, float value)
        {
            _metaBasePercents[statType] = _metaBasePercents.GetValueOrDefault(statType, 0f) + value;
        }

        #endregion
        public float GetBase(StatType statType)
        {
            return _stats.GetValueOrDefault(statType).Base;
        }

        #region Run-Level Potential (reset + re-rolled each run)

        /// <summary>
        /// Initialize per-stat Potential values for a new run. Called at run start
        /// after ResetForRunStart(). The total Potential budget is read from
        /// MetaUpgradeSystem.GetPotentialBudget() (permanent, set by meta upgrades).
        /// Parameters:
        ///   playerChosen — stat→potential pairs the player explicitly allocated
        ///   remainingBudget — leftover budget after player choices (will be randomized)
        ///   randomizer — optional custom random source for tests
        /// The remaining budget is randomly distributed among stats not in playerChosen.
        /// Each stat's Potential is clamped to [0, PotentialMax].
        /// </summary>
        public void InitializeRunPotentials(
            Dictionary<StatType, float> playerChosen,
            float remainingBudget)
        {
            // Step 1: Apply player-chosen Potential values
            float usedBudget = 0f;
            if (playerChosen != null)
            {
                foreach (var kvp in playerChosen)
                {
                    float clamped = Mathf.Clamp(kvp.Value, 0f, StatConstants.PotentialMax);
                    var record = _stats[kvp.Key];
                    record.Potential = clamped;
                    _stats[kvp.Key] = record;
                    usedBudget += clamped;
                }
            }

            // Step 2: Randomly distribute remaining budget among unassigned stats
            float leftover = Mathf.Max(0f, remainingBudget);
            leftover = Mathf.Max(0f, leftover - usedBudget);

            if (leftover > 0f)
            {
                // Collect unassigned stats
                var unassigned = new List<StatType>();
                foreach (StatType st in System.Enum.GetValues(typeof(StatType)))
                {
                    if (playerChosen == null || !playerChosen.ContainsKey(st))
                        unassigned.Add(st);
                }

                if (unassigned.Count > 0)
                {
                    // Random weights — each stat gets a random share
                    float[] weights = new float[unassigned.Count];
                    float totalWeight = 0f;
                    for (int i = 0; i < weights.Length; i++)
                    {
                        weights[i] = Random.value + 0.1f; // avoid zero weights
                        totalWeight += weights[i];
                    }

                    for (int i = 0; i < unassigned.Count; i++)
                    {
                        float share = (weights[i] / totalWeight) * leftover;
                        // Round to nearest 0.5 for granularity, clamp to max
                        share = Mathf.Round(share * 2f) / 2f;
                        share = Mathf.Clamp(share, 0f, StatConstants.PotentialMax);

                        var record = _stats[unassigned[i]];
                        record.Potential = share;
                        _stats[unassigned[i]] = record;
                    }
                }
            }
        }

        /// <summary>
        /// Directly set a stat's Potential for this run. Used when the allocation
        /// logic is handled externally (e.g., by the Potential allocation UI).
        /// Clamped to [0, PotentialMax]. Value will be cleared on ResetForRunStart().
        /// </summary>
        public void SetRunPotential(StatType statType, float value)
        {
            if (!_stats.ContainsKey(statType))
                _stats[statType] = new StatRecord();
            var record = _stats[statType];
            record.Potential = Mathf.Clamp(value, 0f, StatConstants.PotentialMax);
            _stats[statType] = record;
        }

        #endregion

        #region In-Run Gains (run lifetime, Potential-gated)

        /// <summary>
        /// Apply a Potential-scaled in-run gain. Sources: relics, weapons, room rewards.
        /// actualGain = rawGain × (Potential / PotentialMax).
        /// Persists for the run — cleared on ResetForRunStart().
        /// </summary>
        public void AddRunGainPercent(StatType statType, float value)
        {
            _runGainPercents[statType] = _runGainPercents.GetValueOrDefault(statType, 0f) + value;
        }

        public void RemoveRunGainPercent(StatType statType, float value)
        {
            _runGainPercents[statType] = _runGainPercents.GetValueOrDefault(statType, 0f) - value;
        }

        public void ApplyPotentialScaledGain(StatType statType, float rawGain)
        {
            if (!_stats.ContainsKey(statType)) return;
            float scaled = _stats[statType].GetScaledGain(rawGain);
            _runGains[statType] = _runGains.GetValueOrDefault(statType, 0f) + scaled;
        }

        public void RemoveRunGain(StatType statType, float scaledGain)
        {
            _runGains[statType] = _runGains.GetValueOrDefault(statType, 0f) - scaledGain;
        }

        #endregion

        #region In-Run Modifiers (run lifetime, no Potential gating)

        /// <summary>
        /// Add an in-run modifier. Persists for the run — cleared on ResetForRunStart().
        /// Sources: room events, status effects, environmental buffs.
        /// Example: "All attack +10% for this floor."
        /// </summary>
        public void AddModifier(StatType statType, float value)
        {
            _modifiers[statType] = _modifiers.GetValueOrDefault(statType, 0f) + value;
        }

        public void AddModifierPercent(StatType statType, float value)
        {
            _modifierPercents[statType] = _modifierPercents.GetValueOrDefault(statType, 0f) + value;
        }

        public void RemoveModifierPercent(StatType statType, float value)
        {
            _modifierPercents[statType] = _modifierPercents.GetValueOrDefault(statType, 0f) - value;
        }

        public void RemoveModifier(StatType statType, float value)
        {
            _modifiers[statType] = _modifiers.GetValueOrDefault(statType, 0f) - value;
        }

        #endregion

        #region Temporary Modifiers (timer/condition lifetime)

        /// <summary>
        /// Add a temporary modifier. Expires when the timer/condition is handled
        /// by external systems (Timer, StatusEffectManager). Also cleared on run start.
        /// Example: "Potion: +20% attack speed for 30 seconds."
        /// </summary>
        public void AddTemporaryModifier(StatType statType, float value)
        {
            _tempModifiers[statType] = _tempModifiers.GetValueOrDefault(statType, 0f) + value;
        }

        public void AddTemporaryModifierPercent(StatType statType, float value)
        {
            _temporaryModifierPercents[statType] = _temporaryModifierPercents.GetValueOrDefault(statType, 0f) + value;
        }

        public void RemoveTemporaryModifierPercent(StatType statType, float value)
        {
            _temporaryModifierPercents[statType] = _temporaryModifierPercents.GetValueOrDefault(statType, 0f) - value;
        }

        public void RemoveTemporaryModifier(StatType statType, float value)
        {
            _tempModifiers[statType] = _tempModifiers.GetValueOrDefault(statType, 0f) - value;
        }

        #endregion

        #region Run Lifecycle

        /// <summary>
        /// Reset ALL run-lifetime layers in preparation for a new run.
        /// Call this at the START of a new run, BEFORE InitializeRunPotentials().
        ///
        /// Cleared:
        ///   - Per-stat Potential (all zeros — will be re-rolled by InitializeRunPotentials)
        ///   - In-Run Gains (Layer 2)
        ///   - In-Run Modifiers (Layer 3)
        ///   - Temporary Modifiers (Layer 4)
        ///   - Category in-run addends
        ///
        /// Preserved:
        ///   - Permanent Base values (Layer 1a)
        ///   - MetaStatType values (Layer 1b)
        ///   - Category meta bases (permanent)
        /// </summary>
        public void ResetForRunStart()
        {
            // Clear per-stat Potential (will be re-rolled by InitializeRunPotentials)
            foreach (StatType key in System.Enum.GetValues(typeof(StatType)))
            {
                var record = _stats[key];
                record.Potential = 0f;
                _stats[key] = record;
            }

            // Clear in-run gains
            foreach (StatType key in System.Enum.GetValues(typeof(StatType)))
                _runGains[key] = 0f;

            // Clear in-run modifiers
            foreach (StatType key in System.Enum.GetValues(typeof(StatType)))
                _modifiers[key] = 0f;

            foreach (StatType key in System.Enum.GetValues(typeof(StatType)))
            {
                _runGainPercents[key] = 0f;
                _modifierPercents[key] = 0f;
                _temporaryModifierPercents[key] = 0f;
                _tempModifiers[key] = 0f;
            }

            // Clear category in-run addends (meta bases preserved in _categoryMetaBases)
            foreach (string key in new List<string>(_categoryInRunAddends.Keys))
                _categoryInRunAddends[key] = 0f;
        }

        #endregion

        #region Meta-Only Stats (profile lifetime, no Potential)

        public float GetMetaStat(MetaStatType statType)
        {
            if (StatModifierRegistry.TryGetOverride(this, StatModifierTargetKind.MetaStat, default, statType, null, out float overrideValue))
                return overrideValue;
            float value = _metaStats.GetValueOrDefault(statType, 0f);
            StatModifierRegistry.Apply(this, ref value, StatModifierTargetKind.MetaStat, default, statType, null);
            return value;
        }

        public void SetMetaStat(MetaStatType statType, float value)
        {
            _metaStats[statType] = value;
        }

        public void AddMetaStat(MetaStatType statType, float increment)
        {
            _metaStats[statType] = _metaStats.GetValueOrDefault(statType, 0f) + increment;
        }

        public void ResetMetaStats()
        {
            foreach (var key in _metaStats.Keys)
                _metaStats[key] = 0f;
        }

        #endregion

        #region Category Multipliers

        /// <summary>
        /// Get the total category multiplier (meta base + in-run addends).
        /// Returns 0f if no multiplier is set for this category.
        /// Category keys are typically enum .ToString() values (e.g., "Melee", "Fire", "Rare").
        /// </summary>
        public float GetCategoryMultiplier(string category)
        {
            if (StatModifierRegistry.TryGetOverride(this, StatModifierTargetKind.CategoryMultiplier, default, default, category, out float overrideValue))
                return overrideValue;
            float value = _categoryMetaBases.GetValueOrDefault(category, 0f)
                + _categoryInRunAddends.GetValueOrDefault(category, 0f);
            StatModifierRegistry.Apply(this, ref value, StatModifierTargetKind.CategoryMultiplier, default, default, category);
            return value;
        }

        /// <summary>
        /// Set the PERMANENT meta base for a category multiplier. Overwrites existing.
        /// Use this for meta upgrades. Survives across runs — NOT cleared by ResetForRunStart().
        /// </summary>
        public void SetCategoryMultiplier(string category, float value)
        {
            _categoryMetaBases[category] = value;
        }

        /// <summary>
        /// Add an IN-RUN addend to a category multiplier. Used by in-run buffs (relics,
        /// weapon affixes, room rewards). Cleared by ResetForRunStart().
        /// Multiple sources for the same category naturally sum together.
        /// </summary>
        public void AddCategoryMultiplier(string category, float value)
        {
            _categoryInRunAddends[category] = _categoryInRunAddends.GetValueOrDefault(category, 0f) + value;
        }

        public void RemoveCategoryMultiplier(string category, float value)
        {
            _categoryInRunAddends[category] = _categoryInRunAddends.GetValueOrDefault(category, 0f) - value;
        }

        /// <summary>
        /// Get all non-zero category multipliers for the given category keys.
        /// Returns the already-summed per-category totals (meta base + in-run addends).
        /// Cross-category stacking (Melee × Fire × Rare) happens multiplicatively in
        /// DamageCalculator — this method only returns the per-category sums.
        /// Returns null if no matching multipliers are non-zero.
        /// </summary>
        public float[] GetMultipliersForCategories(params string[] categories)
        {
            var result = new List<float>();
            for (int i = 0; i < categories.Length; i++)
            {
                float val = GetCategoryMultiplier(categories[i]);
                if (Mathf.Abs(val) > 0.0001f)
                    result.Add(val);
            }

            return result.Count > 0 ? result.ToArray() : null;
        }

        /// <summary>
        /// Get all non-zero category multipliers matching the given weapon's properties.
        /// Constructs the lookup key list from the weapon's category, elemental type,
        /// and rarity (using enum .ToString() values), then returns per-category totals.
        /// Returns null if no matching multipliers are non-zero.
        /// Use this at attack creation to populate DamageInfo.AdditionalMultipliers.
        /// </summary>
        public float[] GetMultipliersForWeapon(WeaponCategory? category, ElementalType? element, WeaponRarity? rarity)
        {
            var keys = new List<string>();
            if (category.HasValue) keys.Add(category.Value.ToString());
            if (element.HasValue) keys.Add(element.Value.ToString());
            if (rarity.HasValue) keys.Add(rarity.Value.ToString());

            if (keys.Count == 0) return null;
            return GetMultipliersForCategories(keys.ToArray());
        }

        /// <summary>
        /// Reset ALL category multipliers (both meta bases and in-run addends).
        /// Called only on full profile reset. Use ResetForRunStart() for normal run transitions.
        /// </summary>
        public void ResetCategoryMultipliers()
        {
            foreach (var key in _categoryMetaBases.Keys)
                _categoryMetaBases[key] = 0f;
            foreach (var key in _categoryInRunAddends.Keys)
                _categoryInRunAddends[key] = 0f;
        }

        #endregion
    }
}
