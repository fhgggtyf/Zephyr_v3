/*
 * CrouchActionSO.cs
 * -----------------
 * Module:  Player / StateMachine / Actions
 * Purpose: Shared action for CrouchIdle and CrouchMoving states.
 *          Sets the crouch movement speed and marks the player as crouching.
 *          Speed is read from PlayerMovementSpeedDataSO (crouchSpeed field).
 * Scene:    GameManager (attached to player prefab).
 * Ch.Ref:   Ch.6.4 State Actions.
 */
using UnityEngine;
using Zephyr.Core.StateMachine;
using CoreSM = Zephyr.Core.StateMachine.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using Zephyr.Gameplay.Player.Core;
using Zephyr.Gameplay.Player.Data;

namespace Zephyr.Gameplay.Player.StateMachine
{
    [CreateAssetMenu(fileName = "CrouchAction", menuName = "State Machines/Actions/Player/Crouch")]
    public class CrouchActionSO : StateActionSO
    {
        [Tooltip("Reference to the player movement speed configuration SO.")]
        public PlayerMovementSpeedDataSO speedData;

        protected override StateAction CreateAction() => new CrouchAction(speedData);
    }

    public class CrouchAction : StateAction
    {
        private MovementCore _movementCore;
        private PlayerColliderController _colliderController;
        private readonly PlayerMovementSpeedDataSO _speedData;

        public CrouchAction(PlayerMovementSpeedDataSO speedData)
        {
            _speedData = speedData;
        }

        public override void Awake(CoreSM stateMachine)
        {
            _movementCore = stateMachine.GetCachedComponent<MovementCore>();
            stateMachine.TryGetCachedComponent(out _colliderController);
        }

        public override void OnStateEnter()
        {
            if (_movementCore == null) return;

            _movementCore.SetMoveSpeed(_speedData != null ? _speedData.crouchSpeed : 3f);
            _movementCore.SetCrouching(true);
            _colliderController?.RequestCrouch(true);
        }

        public override void OnStateExit()
        {
            if (_movementCore == null) return;

            _movementCore.SetCrouching(false);
            _colliderController?.RequestCrouch(false);
        }

        public override void OnUpdate() { }
    }
}
