/*
 * JumpForceActionSO.cs
 * -------------------
 * Module:  Player / StateMachine / Actions
 * Purpose: Applies the jump force to MovementCore when entering a jump state.
 *          Reads jump force and gravity settings from JumpDataSO.
 *          Also sets the gravity scale to the jump value on enter and restores
 *          it on exit.
 * Scene:    GameManager (attached to player prefab).
 * Ch.Ref:   Ch.6.4 State Actions, Ch.6.8 Physics / Jump Mechanics.
 */
using UnityEngine;
using Zephyr.Core.StateMachine;
using CoreSM = Zephyr.Core.StateMachine.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using Zephyr.Gameplay.Player.Core;
using Zephyr.Gameplay.Player.Data;

namespace Zephyr.Gameplay.Player.StateMachine
{
    [CreateAssetMenu(fileName = "JumpForceAction", menuName = "State Machines/Actions/Player/Jump Force")]
    public class JumpForceActionSO : StateActionSO
    {
        [Tooltip("Reference to the jump physics configuration SO.")]
        public JumpDataSO jumpData;

        protected override StateAction CreateAction() => new JumpForceAction(jumpData);
    }

    public class JumpForceAction : StateAction
    {
        private MovementCore _movementCore;
        private PlayerResourceController _resources;
        private readonly JumpDataSO _jumpData;
        private bool _ownsGravity;
        private float _ownedGravityScale;
        private float _previousGravityScale;

        public JumpForceAction(JumpDataSO jumpData)
        {
            _jumpData = jumpData;
        }

        public override void Awake(CoreSM stateMachine)
        {
            _movementCore = stateMachine.GetCachedComponent<MovementCore>();
            stateMachine.TryGetCachedComponent(out _resources);
        }

        public override void OnStateEnter()
        {
            if (_jumpData == null || _movementCore == null)
            {
                Debug.LogWarning($"[JumpForceAction] Missing refs: jumpData={_jumpData != null}, movementCore={_movementCore != null}");
                return;
            }

            if (_resources == null || !_resources.TryConsumeJumpStamina())
            {
                Debug.LogWarning("[JumpForceAction] Jump blocked because stamina is unavailable.");
                return;
            }

            // 空中起跳消耗一次二段跳
            if (!_movementCore.IsGrounded)
            {
                _movementCore.ConsumeAirJump();
            }

            Debug.Log($"[JumpForceAction] Enter: jumpForce={_jumpData.jumpForce}, gravity={_jumpData.gravityScale}, grounded={_movementCore.IsGrounded}");
            _ownedGravityScale = _jumpData.gravityScale;
            _previousGravityScale = _movementCore.SetGravityScale(_ownedGravityScale);
            _ownsGravity = true;
            _movementCore.ApplyJumpForce(_jumpData.jumpForce);
        }

        public override void OnStateExit()
        {
            if (_movementCore == null || !_ownsGravity) return;

            _movementCore.RestoreGravityScale(_ownedGravityScale, _previousGravityScale);
            _ownsGravity = false;
        }

        public override void OnUpdate() { }
    }
}
