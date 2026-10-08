/*
 * AirborneToDoubleJumpConditionSO.cs
 * ----------------------------------
 * Module:  Player / StateMachine / Conditions
 * Purpose: Condition for transitioning to JumpUp from any airborne state
 *          (JumpUp, JumpPeak, JumpFall) when the player presses jump again
 *          and still has an air jump remaining.
 *          Requires: !IsGrounded + HasDoubleJumpRemaining + pending jump input.
 * Scene:    GameManager (attached to player prefab).
 * Ch.Ref:   Ch.6.5 State Conditions, Ch.6.8 Physics / Double Jump.
 */
using UnityEngine;
using Zephyr.Core.StateMachine;
using CoreSM = Zephyr.Core.StateMachine.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using Zephyr.Gameplay.Player.Core;
using Zephyr.Gameplay.Player.Input;

namespace Zephyr.Gameplay.Player.StateMachine
{
    [CreateAssetMenu(fileName = "AirborneToDoubleJumpCondition", menuName = "State Machines/Conditions/Player/Airborne To Double Jump")]
    public class AirborneToDoubleJumpConditionSO : StateConditionSO
    {
        protected override Condition CreateCondition() => new AirborneToDoubleJumpCondition();
    }

    public class AirborneToDoubleJumpCondition : Condition
    {
        private PlayerInputReader _inputReader;
        private MovementCore _movementCore;
        private PlayerResourceController _resources;

        public override void Awake(CoreSM stateMachine)
        {
            _inputReader = stateMachine.GetCachedComponent<PlayerInputReader>();
            _movementCore = stateMachine.GetCachedComponent<MovementCore>();
            stateMachine.TryGetCachedComponent(out _resources);
        }

        protected override bool Statement()
        {
            if (_inputReader == null || _movementCore == null) return false;

            if (_movementCore.IsGrounded) return false;
            if (!_movementCore.HasDoubleJumpRemaining) return false;
            if (_resources == null || !_resources.CanJump)
            {
                // Reject an unaffordable edge-triggered press immediately.
                _inputReader.ConsumeJumpInput();
                return false;
            }

            return _inputReader.ConsumeJumpInput();
        }
    }
}
