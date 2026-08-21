/*
 * GroundedToJumpConditionSO.cs
 * ---------------------------
 * Module:  Player / StateMachine / Conditions
 * Purpose: Condition for transitioning from any grounded state (Idle, Walk,
 *          Sprint) into the JumpUp state. Requires the jump button to be
 *          pressed AND the player to be grounded.
 * Scene:    GameManager (attached to player prefab).
 * Ch.Ref:   Ch.6.5 State Conditions, Ch.6.8 Physics / Jump Mechanics.
 */
using UnityEngine;
using Zephyr.Core.StateMachine;
using CoreSM = Zephyr.Core.StateMachine.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using Zephyr.Gameplay.Player.Core;
using Zephyr.Gameplay.Player.Input;

namespace Zephyr.Gameplay.Player.StateMachine
{
    [CreateAssetMenu(fileName = "GroundedToJumpCondition", menuName = "State Machines/Conditions/Player/Grounded To Jump")]
    public class GroundedToJumpConditionSO : StateConditionSO
    {
        protected override Condition CreateCondition() => new GroundedToJumpCondition();
    }

    public class GroundedToJumpCondition : Condition
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
            if (_inputReader == null || _movementCore == null)
            {
                Debug.LogWarning("[GroundedToJumpCondition] Missing references: inputReader=" + (_inputReader != null) + ", movementCore=" + (_movementCore != null));
                return false;
            }

            if (!_movementCore.IsGrounded) return false;
            if (_resources == null || !_resources.CanJump) return false;

            return _inputReader.ConsumeJumpInput();
        }
    }
}
