/*
 * SprintToIdleConditionSO.cs
 * --------------------------
 * Module:  Player / StateMachine / Conditions
 * Purpose: State condition for transitioning Sprint → Idle. Returns true
 *          when horizontal movement input is released. This allows the
 *          player to stop instantly from sprint without going through Walk
 *          (unlike releasing Shift which goes Sprint → Walk).
 * Scene:    GameManager (attached to player prefab).
 * Ch.Ref:   Ch.6.5 State Conditions, Ch.6.7 Owner Binding.
 */
using UnityEngine;
using Zephyr.Core.StateMachine;
using CoreSM = Zephyr.Core.StateMachine.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using Zephyr.Gameplay.Player.Input;

namespace Zephyr.Gameplay.Player.StateMachine
{
    [CreateAssetMenu(fileName = "SprintToIdleCondition", menuName = "State Machines/Conditions/Player/Sprint To Idle")]
    public class SprintToIdleConditionSO : StateConditionSO
    {
        protected override Condition CreateCondition() => new SprintToIdleCondition();
    }

    public class SprintToIdleCondition : Condition
    {
        private PlayerInputReader _inputReader;

        public override void Awake(CoreSM stateMachine)
        {
            _inputReader = stateMachine.GetCachedComponent<PlayerInputReader>();
        }

        protected override bool Statement()
        {
            if (_inputReader == null) return true;

            return !_inputReader.HasHorizontalInput;
        }
    }
}
