using UnityEngine;

namespace Zephyr.Core.Meta
{
    [CreateAssetMenu(fileName = "Objective", menuName = "Zephyr/Story/Objective")]
    public sealed class ObjectiveSO : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _fulfillEventId;
        public string Id => string.IsNullOrWhiteSpace(_id) ? name : _id;
        public string FulfillEventId => _fulfillEventId;
    }
}
