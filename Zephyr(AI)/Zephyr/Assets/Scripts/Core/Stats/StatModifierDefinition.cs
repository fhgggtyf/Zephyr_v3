using System.Collections.Generic;
using UnityEngine;

namespace Zephyr.Core.Stats
{
    public enum StatModifierTargetKind
    {
        CoreStat,
        MetaStat,
        CategoryMultiplier
    }

    public enum StatModifierOperation
    {
        Add,
        Multiply,
        Override
    }

    public enum StatsOwnerRole
    {
        Unknown,
        Player,
        Enemy,
        Other
    }

    public enum StatModifierScope
    {
        All,
        Player,
        Enemy,
        NonPlayer
    }

    [System.Serializable]
    public sealed class StatModifierDefinition
    {
        [SerializeField] private StatModifierTargetKind _targetKind = StatModifierTargetKind.CoreStat;
        [SerializeField] private StatModifierOperation _operation = StatModifierOperation.Add;
        [SerializeField] private StatModifierScope _scope = StatModifierScope.All;
        [SerializeField] private StatType _statType = StatType.Attack;
        [SerializeField] private MetaStatType _metaStatType = MetaStatType.DamageReduction;
        [SerializeField] private string _category;
        [SerializeField] private float _value;

        public StatModifierTargetKind TargetKind => _targetKind;
        public StatModifierOperation Operation => _operation;
        public StatModifierScope Scope => _scope;
        public StatType StatType => _statType;
        public MetaStatType MetaStatType => _metaStatType;
        public string Category => _category;
        public float Value => _value;

        public bool AppliesTo(StatsCore stats)
        {
            if (stats == null) return false;
            switch (_scope)
            {
                case StatModifierScope.All: return true;
                case StatModifierScope.Player: return stats.OwnerRole == StatsOwnerRole.Player;
                case StatModifierScope.Enemy: return stats.OwnerRole == StatsOwnerRole.Enemy;
                case StatModifierScope.NonPlayer: return stats.OwnerRole != StatsOwnerRole.Player;
                default: return false;
            }
        }
    }

    public interface IStatModifierSource
    {
        IReadOnlyList<StatModifierDefinition> Modifiers { get; }
        bool AppliesTo(StatsCore stats);
    }
}
