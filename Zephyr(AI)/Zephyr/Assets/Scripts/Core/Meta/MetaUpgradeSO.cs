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
        [SerializeField] private string _targetKey;
        [SerializeField] private float _amount = 1f;
        [SerializeField] private bool _repeatable;

        public string Id => string.IsNullOrWhiteSpace(_id) ? name : _id;
        public string DisplayNameKey => _displayNameKey;
        public string DescriptionKey => _descriptionKey;
        public long Cost => _cost;
        public MetaUpgradeTarget Target => _target;
        public string TargetKey => _targetKey;
        public float Amount => _amount;
        public bool Repeatable => _repeatable;
    }
}
