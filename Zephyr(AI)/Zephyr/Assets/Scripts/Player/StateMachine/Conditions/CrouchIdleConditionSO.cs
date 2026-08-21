/*
 * CrouchIdleConditionSO.cs
 * ------------------------
 * Module:  Player / StateMachine / Conditions
 * Purpose: Condition for transitioning into CrouchIdle state.
 *          Triggers when: grounded, crouch is pressed, and no horizontal input.
 * Scene:    GameManager (attached to player prefab).
 * Ch.Ref:   Ch.6.5 State Conditions.
 */
using UnityEngine;
using Zephyr.Core.StateMachine;
using CoreSM = Zephyr.Core.StateMachine.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using Zephyr.Gameplay.Player.Core;
using Zephyr.Gameplay.Player.Input;

namespace Zephyr.Gameplay.Player.StateMachine
{
    [CreateAssetMenu(fileName = "CrouchIdleCondition", menuName = "State Machines/Conditions/Player/Crouch Idle")]
    public class CrouchIdleConditionSO : StateConditionSO
    {
        protected override Condition CreateCondition() => new CrouchIdleCondition();
    }

    public class CrouchIdleCondition : Condition
    {
        private PlayerInputReader _inputReader;
        private MovementCore _movementCore;

        public override void Awake(CoreSM stateMachine)
        {
            _inputReader = stateMachine.GetCachedComponent<PlayerInputReader>();
            _movementCore = stateMachine.GetCachedComponent<MovementCore>();
        }

        protected override bool Statement()
        {
            if (_inputReader == null || _movementCore == null) return false;

            return _movementCore.IsGrounded
                && _inputReader.HasCrouchInput
                && !_inputReader.HasHorizontalInput;
        }
    }
}
