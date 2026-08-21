/*
 * StatsCore.cs
 * ------------
 * Module:  Core / Stats
 * Purpose: Central stat container MonoBehaviour implementing IStatSource. Manages stat values
 *          with FOUR lifetime layers:
 *
 *          ==== LIFETIME MODEL ====
 *
 *          [Profile Lifetime — permanent, never reset]
 *          Layer 1a — Permanent Base:
 *              SetBase() → meta upgrades set Base permanently. Never reset.
 *              Stored in StatRecord.Base.
 *          Layer 1b — MetaStatType values:
 *              SetMetaStat() / AddMetaStat() → non-Potential permanent stats
 *              (crit chance, piercing, max shield, etc.). Never reset.
 *
 *          [Run Lifetime — reset at run start]
 *          Potential — per-stat Potential values:
 *              InitializeRunPotentials() sets each stat's Potential for this run.
 *              Derived from player's total Potential budget (MetaStatType.PotentialBudget)
 *              — some chosen by player, rest randomized. Re-rolled every run.
 *          Layer 2 — In-Run Gains (Potential-gated):
 *              ApplyPotentialScaledGain() → relics, weapons, room rewards.
 *              actualGain = rawGain × (Potential / PotentialMax).
 *          Layer 3 — In-Run Modifiers:
 *              AddModifier() / RemoveModifier() → room events, status effects.
 *          Category Multipliers — per-category multiplicative buffs:
 *              SetCategoryMultiplier() = meta base (permanent, Layer 1)
 *              AddCategoryMultiplier() = in-run addend (run lifetime, Layer 2)
 *
 *          [Temporary Lifetime — timer/condition expiry]
 *          Layer 4 — Temporary Modifiers:
 *              AddTemporaryModifier() / RemoveTemporaryModifier() → short-lived buffs.
 *              Expiry managed by external systems (Timer, StatusEffectManager).
 *
 *          ==== FORMULA ====
 *          GetStatValue(stat) = Base + RunGains + Modifiers + TempModifiers
 *
 *          All run-lifetime layers (Potential, RunGains, Modifiers, category addends,
 *          TempModifiers) are cleared by ResetForRunStart() at the start of each run.
 *          After ResetForRunStart(), call InitializeRunPotentials() to set the run's
 *          per-stat Potential values from the player's total budget.
 * Dependencies: IStatSource (interface), StatRecord (value type), StatType, MetaStatType,
 *               GameConstants (tuning values).
 * Scene:    GameManager (attached to player and enemy prefabs).
 * Ch.Ref:   Ch.5 Stat System, Ch.5.3 Potential Mechanic, Ch.5.10 Luck Mechanic,
 *           Ch.2.8 Damage Calculation Pipeline, Ch.16 Meta Progression.
 */
using System.Collections.Generic;
using UnityEngine;
using Zephyr.Core.DamageSystem;
using Zephyr.Core.Interfaces;

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
    /// The total Potential budget is stored in MetaStatType.PotentialBudget (permanent).
    /// Per-stat Potential values are set via InitializeRunPotentials() at run start.
    /// </summary>
    public class StatsCore : MonoBehaviour, IStatSource
    {
        // StatRecord holds Base (permanent) + Potential (run-level).
        private readonly Dictionary<StatType, StatRecord> _stats = new Dictionary<StatType, StatRecord>();

        // Run-lifetime in-run gains (Potential-gated)
        private readonly Dictionary<StatType, float> _runGains = new Dictionary<StatType, float>();

        // Run-lifetime in-run modifiers (no Potential gating)
        private readonly Dictionary<StatType, float> _modifiers = new Dictionary<StatType, float>();

        // Temporary modifiers (timer/condition expiry)
        private readonly Dictionary<StatType, float> _tempModifiers = new Dictionary<StatType, float>();

        // Meta-only stats (profile lifetime, no Potential)
        private readonly Dictionary<MetaStatType, float> _metaStats = new Dictionary<MetaStatType, float>();

        // Category multipliers: meta base (permanent) + in-run addends (run lifetime)
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
        /// Total stat value = Permanent Base + In-Run Gains + In-Run Modifiers + Temporary Modifiers.
        /// Single source of truth for damage calculation (Ch.2.8).
        /// </summary>
        public float GetStatValue(StatType statType)
        {
            if (!_stats.ContainsKey(statType)) return 0f;
            return _stats[statType].Base
                 + _runGains.GetValueOrDefault(statType, 0f)
                 + _modifiers.GetValueOrDefault(statType, 0f)
                 + _tempModifiers.GetValueOrDefault(statType, 0f);
        }

        /// <summary>
        /// Get the current run's Potential for a stat. Re-rolled each run via
        /// InitializeRunPotentials(). Range: 0 to GameConstants.Stats.PotentialMax (10).
        /// </summary>
        public float GetPotential(StatType statType)
        {
            if (!_stats.ContainsKey(statType)) return 0f;
            return _stats[statType].Potential;
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
        public float GetBase(StatType statType)
        {
            if (!_stats.ContainsKey(statType)) return 0f;
            return _stats[statType].Base;
        }

        #endregion

        #region Run-Level Potential (reset + re-rolled each run)

        /// <summary>
        /// Initialize per-stat Potential values for a new run. Called at run start
        /// after ResetForRunStart(). The total Potential budget is read from
        /// MetaStatType.PotentialBudget (permanent, set by meta upgrades).
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
                    float clamped = Mathf.Clamp(kvp.Value, 0f, GameConstants.Stats.PotentialMax);
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
                        share = Mathf.Clamp(share, 0f, GameConstants.Stats.PotentialMax);

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
            record.Potential = Mathf.Clamp(value, 0f, GameConstants.Stats.PotentialMax);
            _stats[statType] = record;
        }

        #endregion

        #region In-Run Gains (run lifetime, Potential-gated)

        /// <summary>
        /// Apply a Potential-scaled in-run gain. Sources: relics, weapons, room rewards.
        /// actualGain = rawGain × (Potential / PotentialMax).
        /// Persists for the run — cleared on ResetForRunStart().
        /// </summary>
        public void ApplyPotentialScaledGain(StatType statType, float rawGain)
        {
            if (!_stats.ContainsKey(statType)) return;
            var record = _stats[statType];
            float scaled = record.GetScaledGain(rawGain);
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
            foreach (var key in _stats.Keys)
            {
                var record = _stats[key];
                record.Potential = 0f;
                _stats[key] = record;
            }

            // Clear in-run gains
            foreach (var key in _runGains.Keys)
                _runGains[key] = 0f;

            // Clear in-run modifiers
            foreach (var key in _modifiers.Keys)
                _modifiers[key] = 0f;

            // Clear temporary modifiers
            foreach (var key in _tempModifiers.Keys)
                _tempModifiers[key] = 0f;

            // Clear category in-run addends (meta bases preserved in _categoryMetaBases)
            foreach (var key in _categoryInRunAddends.Keys)
                _categoryInRunAddends[key] = 0f;
        }

        #endregion

        #region Meta-Only Stats (profile lifetime, no Potential)

        public float GetMetaStat(MetaStatType statType)
        {
            return _metaStats.GetValueOrDefault(statType, 0f);
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
            float metaBase = _categoryMetaBases.GetValueOrDefault(category, 0f);
            float inRunAddend = _categoryInRunAddends.GetValueOrDefault(category, 0f);
            return metaBase + inRunAddend;
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
