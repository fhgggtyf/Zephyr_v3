/*
 * SprintToWalkCondition.cs
 * ------------------------
 * Module:  Player / StateMachine / Conditions
 * Purpose: State condition for transitioning Sprint → Walk. Returns true
 *          when sprint input is released (Shift not held) OR movement stops.
 *          This allows exiting sprint either by releasing Shift or stopping.
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
    [CreateAssetMenu(fileName = "SprintToWalkCondition", menuName = "State Machines/Conditions/Player/Sprint To Walk")]
    public class SprintToWalkConditionSO : StateConditionSO
    {
        protected override Condition CreateCondition() => new SprintToWalkCondition();
    }

    public class SprintToWalkCondition : Condition
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
            if (_inputReader == null || _resources == null || !_resources.CanSprint) return true;

            return !_inputReader.HasSprintInput
                || !_inputReader.HasHorizontalInput;
        }
    }
}
