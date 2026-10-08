using UnityEngine;

namespace Zephyr.Core.Meta
{
    public abstract class MilestoneEffectSO : ScriptableObject
    {
        public abstract void Apply(StoryProgressionService service);
    }

    [CreateAssetMenu(fileName = "SetFactEffect", menuName = "Zephyr/Progression/Effects/Set Fact")]
    public sealed class SetFactEffectSO : MilestoneEffectSO
    {
        [SerializeField] private StoryFactSO _fact;
        [SerializeField] private bool _boolValue = true;
        [SerializeField] private int _intValue;

        public override void Apply(StoryProgressionService service)
        {
            if (service == null || _fact == null) return;
            if (_fact.ValueType == StoryFactValueType.Bool)
                service.SetBool(_fact, _boolValue);
            else
                service.SetInt(_fact, _intValue);
        }
    }
}
