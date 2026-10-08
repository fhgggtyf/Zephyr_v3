using UnityEngine;

namespace Zephyr.Core.Meta
{
    public enum StoryFactValueType
    {
        Bool,
        Int
    }

    [CreateAssetMenu(fileName = "StoryFact", menuName = "Zephyr/Progression/Story Fact")]
    public sealed class StoryFactSO : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private StoryFactValueType _valueType = StoryFactValueType.Bool;

        public string Id => string.IsNullOrWhiteSpace(_id) ? name : _id;
        public StoryFactValueType ValueType => _valueType;
    }
}
