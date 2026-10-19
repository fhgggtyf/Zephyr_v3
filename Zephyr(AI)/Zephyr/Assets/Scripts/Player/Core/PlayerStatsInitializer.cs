using System.Collections.Generic;
using UnityEngine;
using Zephyr.Core;
using Zephyr.Core.Health;
using Zephyr.Contracts.Interfaces;
using Zephyr.Core.Save;
using Zephyr.Core.Stats;
using Zephyr.Core.Meta;
using Zephyr.Core.Flow;

namespace Zephyr.Gameplay.Player.Core
{
    /// <summary>
    /// Initializes the player's StatsCore with base values on startup.
    /// Attach to the Player prefab. Edit base values in the Inspector or
    /// modify the defaults in ResetToDefaults().
    /// </summary>
    [RequireComponent(typeof(StatsCore))]
    public class PlayerStatsInitializer : MonoBehaviour, IGameManagerDependencyConsumer
    {
        [Header("Base Stats")]
        [SerializeField] private float _baseAttack = 25f;
        [SerializeField] private float _baseDefense = 5f;
        [SerializeField] private float _baseMaxHp = 100f;
        [SerializeField] private float _baseStamina = 100f;
        [SerializeField] private float _baseEnergy = 50f;
        [SerializeField] private float _baseAttackSpeed = 1f;
        [Header("Impact resistance")]
        [Tooltip("Base tenacity percentage. Keep this at or below 20; in-run upgrades may add more up to 80% total.")]
        [Range(0f, 20f)] [SerializeField] private float _baseTenacity = 0f;

        [Header("Potential fallback")]
        [Tooltip("Used until run-entry Potential allocation is authored or when playing a scene directly.")]
        [Range(0f, StatConstants.PotentialMax)]
        [SerializeField] private float _defaultPotential = StatConstants.PotentialMax;

        private StatsCore _statsCore;
        private ISaveDataGateway _saveData;
        private IMetaGateway _metaGateway;
        private bool _initialized;

        private void Awake()
        {
            _statsCore = GetComponent<StatsCore>();
            _statsCore.SetOwnerRole(StatsOwnerRole.Player);
        }

        private void Start()
        {
            InitializeNow();
        }

        public void Configure(ISaveDataGateway saveData)
        {
            _saveData = saveData;
        }

        public void Configure(IGameManagerServices services)
        {
            _saveData = services?.SaveData;
            _metaGateway = _saveData != null ? new MetaGateway(_saveData) : null;
        }

        public IMetaGateway MetaGateway => _metaGateway;

        public void InitializeNow()
        {
            if (_initialized) return;
            _initialized = true;
            ApplyDefaults();
        }

        private void ApplyDefaults()
        {
            if (_statsCore == null) return;

            _statsCore.SetBase(StatType.Attack, _baseAttack);
            _statsCore.SetBase(StatType.Armor, _baseDefense);
            _statsCore.SetBase(StatType.MaxHp, _baseMaxHp);
            _statsCore.SetBase(StatType.Stamina, _baseStamina);
            _statsCore.SetBase(StatType.Energy, _baseEnergy);
            _statsCore.SetBase(StatType.AttackSpeed, _baseAttackSpeed);
            _statsCore.SetBase(StatType.Tenacity, _baseTenacity);
            ApplyMetaBaseUpgrades();
            ApplyRunPotentials();
            GetComponent<HealthComponent>()?.ResetHealth();

            Debug.Log($"[PlayerStatsInitializer] Base stats set: " +
                      $"ATK={_baseAttack}, DEF={_baseDefense}, " +
                      $"HP={_baseMaxHp}, STA={_baseStamina}, ENG={_baseEnergy}", this);
        }

        private void ApplyRunPotentials()
        {
            Dictionary<int, float> saved = _saveData?.CurrentRun?.Potentials;
            foreach (StatType statType in System.Enum.GetValues(typeof(StatType)))
            {
                float potential = saved != null && saved.TryGetValue((int)statType, out float value)
                    ? value
                    : _defaultPotential;
                _statsCore.SetRunPotential(statType, potential);
            }
        }

        private void ApplyMetaBaseUpgrades()
        {
            if (_saveData?.Meta == null) return;

            Dictionary<string, float> baseStatUpgrades = _saveData.Meta.BaseStatUpgrades;
            if (baseStatUpgrades != null)
            {
                foreach (KeyValuePair<string, float> upgrade in baseStatUpgrades)
                {
                    string key = upgrade.Key;
                    bool percent = key.EndsWith(".Percent", System.StringComparison.OrdinalIgnoreCase);
                    if (percent) key = key.Substring(0, key.Length - ".Percent".Length);
                    if (!System.Enum.TryParse(key, true, out StatType statType)) continue;
                    if (percent) _statsCore.AddMetaBasePercent(statType, upgrade.Value);
                    else _statsCore.AddMetaBase(statType, upgrade.Value);
                }
            }

            Dictionary<string, float> metaStatUpgrades = _saveData.Meta.CombatAcquiredUpgrades;
            if (metaStatUpgrades == null) return;

            foreach (KeyValuePair<string, float> upgrade in metaStatUpgrades)
            {
                if (!System.Enum.TryParse(upgrade.Key, true, out MetaStatType metaStatType)) continue;
                _statsCore.AddMetaStat(metaStatType, upgrade.Value);
            }
        }

        public void ResetToDefaults()
        {
            ApplyDefaults();
        }
    }
}
