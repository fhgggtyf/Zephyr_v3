using Zephyr.Core.Stats;
using UnityEngine;

namespace Zephyr.Core.Meta
{
    public enum MetaUpgradeTarget
    {
        BaseStat,
        Combat,
        PotentialBudget,
        PotentialAllocatable
    }

    [CreateAssetMenu(fileName = "MetaUpgrade", menuName = "Zephyr/Meta/Upgrade")]
    public sealed class MetaUpgradeSO : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayNameKey;
        [SerializeField] private string _descriptionKey;
        [Min(0)] [SerializeField] private long _cost;
        [SerializeField] private MetaUpgradeTarget _target;
        [SerializeField] private StatModifierOperation _operation = StatModifierOperation.Add;
        [SerializeField] private StatType _baseStatType = StatType.Attack;
        [SerializeField] private MetaStatType _metaStatType = MetaStatType.CritChance;
        [Tooltip("Legacy key for existing assets. Leave empty for new upgrades; the enum above will be used.")]
        [SerializeField] private string _targetKey;
        [SerializeField] private float _amount = 1f;
        [SerializeField] private bool _repeatable;

        public string Id => string.IsNullOrWhiteSpace(_id) ? name : _id;
        public string DisplayNameKey => _displayNameKey;
        public string DescriptionKey => _descriptionKey;
        public long Cost => _cost;
        public MetaUpgradeTarget Target => _target;
        public StatModifierOperation Operation => _operation;
        public StatType BaseStatType => _baseStatType;
        public MetaStatType MetaStatType => _metaStatType;
        public string TargetKey
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_targetKey) && _targetKey != "default")
                    return _targetKey;

                if (_target == MetaUpgradeTarget.BaseStat)
                    return _baseStatType.ToString();
                if (_target == MetaUpgradeTarget.Combat)
                    return _metaStatType.ToString();
                return _targetKey;
            }
        }
        public float Amount => _amount;
        public bool Repeatable => _repeatable;
    }
}
