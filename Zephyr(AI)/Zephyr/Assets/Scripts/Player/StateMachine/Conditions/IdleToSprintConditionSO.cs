/*
 * IdleToSprintCondition.cs
 * ------------------------
 * Module:  Player / StateMachine / Conditions
 * Purpose: State condition for transitioning Idle → Sprint. Requires both
 *          sprint input held (Shift) and active horizontal movement input.
 *          Allows direct Idle → Sprint without going through Walk first.
 * Scene:    GameManager (attached to player prefab).
 * Ch.Ref:   Ch.6.5 State Conditions, Ch.6.7 Owner Binding.
 */
using UnityEngine;
using Zephyr.Core.StateMachine;
using CoreSM = Zephyr.Core.StateMachine.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using Zephyr.Gameplay.Player.Core;
using Zephyr.Gameplay.Player.Input;

namespace Zephyr.Gameplay.Player.StateMachine
{
    [CreateAssetMenu(fileName = "IdleToSprintCondition", menuName = "State Machines/Conditions/Player/Idle To Sprint")]
    public class IdleToSprintConditionSO : StateConditionSO
    {
        protected override Condition CreateCondition() => new IdleToSprintCondition();
    }

    public class IdleToSprintCondition : Condition
    {
        private PlayerInputReader _inputReader;
        private PlayerResourceController _resources;

        public override void Awake(CoreSM stateMachine)
        {
            _inputReader = stateMachine.GetCachedComponent<PlayerInputReader>();
            stateMachine.TryGetCachedComponent(out _resources);
        }

        protected override bool Statement()
        {
            if (_inputReader == null || _resources == null || !_resources.CanSprint) return false;

            return _inputReader.HasSprintInput
                && _inputReader.HasHorizontalInput;
        }
    }
}
