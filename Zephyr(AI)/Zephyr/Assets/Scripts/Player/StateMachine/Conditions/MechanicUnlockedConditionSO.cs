using UnityEngine;
using Zephyr.Core.Meta;
using Zephyr.Core.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using CoreStateMachine = Zephyr.Core.StateMachine.StateMachine;

namespace Zephyr.Gameplay.Player.StateMachine
{
    /// <summary>Transition gate for profile-persistent mechanic unlocks.</summary>
    [CreateAssetMenu(fileName = "MechanicUnlockedCondition", menuName = "State Machines/Conditions/Mechanic Unlocked")]
    public sealed class MechanicUnlockedConditionSO : StateConditionSO
    {
        [SerializeField] private string _mechanicId;
        protected override Condition CreateCondition() => new MechanicUnlockedCondition(_mechanicId);
    }

    public sealed class MechanicUnlockedCondition : Condition
    {
        private readonly string _mechanicId;
        public MechanicUnlockedCondition(string mechanicId) => _mechanicId = mechanicId;
        protected override bool Statement() => MechanicUnlockSystem.IsUnlocked(_mechanicId);
    }
}
