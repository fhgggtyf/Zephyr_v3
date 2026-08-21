using UnityEngine;

namespace Zephyr.Core.Meta
{
    [CreateAssetMenu(fileName = "MechanicUnlock", menuName = "Zephyr/Meta/Mechanic Unlock")]
    public sealed class MechanicUnlockSO : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayNameKey;
        [SerializeField] private string _descriptionKey;
        [Min(0)] [SerializeField] private long _cost;
        [SerializeField] private string _requiredObjectiveId;

        public string Id => string.IsNullOrWhiteSpace(_id) ? name : _id;
        public string DisplayNameKey => _displayNameKey;
        public string DescriptionKey => _descriptionKey;
        public long Cost => _cost;
        public string RequiredObjectiveId => _requiredObjectiveId;
    }
}
