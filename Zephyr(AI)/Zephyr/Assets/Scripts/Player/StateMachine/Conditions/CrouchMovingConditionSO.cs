/*
 * CrouchMovingConditionSO.cs
 * ---------------------------
 * Module:  Player / StateMachine / Conditions
 * Purpose: Condition for transitioning into CrouchMoving state.
 *          Triggers when: grounded, crouch is pressed, and there is horizontal input.
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
    [CreateAssetMenu(fileName = "CrouchMovingCondition", menuName = "State Machines/Conditions/Player/Crouch Moving")]
    public class CrouchMovingConditionSO : StateConditionSO
    {
        protected override Condition CreateCondition() => new CrouchMovingCondition();
    }

    public class CrouchMovingCondition : Condition
    {
        private PlayerInputReader _inputReader;
        private MovementCore _movementCore;
        private PlayerColliderController _colliderController;

        public override void Awake(CoreSM stateMachine)
        {
            _inputReader = stateMachine.GetCachedComponent<PlayerInputReader>();
            _movementCore = stateMachine.GetCachedComponent<MovementCore>();
            stateMachine.TryGetCachedComponent(out _colliderController);
        }

        protected override bool Statement()
        {
            if (_inputReader == null || _movementCore == null) return false;

            return _movementCore.IsGrounded
                && (_inputReader.HasCrouchInput
                    || (_colliderController != null && _colliderController.MustRemainCrouched))
                && _inputReader.HasHorizontalInput;
        }
    }
}
