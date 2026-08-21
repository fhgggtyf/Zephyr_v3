/*
 * IdleSpeedActionSO.cs
 * -------------------
 * Module:  Player / StateMachine / Actions
 * Purpose: Sets the player's move speed to idle speed when entering the Idle state.
 *          Speed value is read from PlayerMovementSpeedDataSO (typically 0).
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
    [CreateAssetMenu(fileName = "IdleSpeedAction", menuName = "State Machines/Actions/Player/Idle Speed")]
    public class IdleSpeedActionSO : StateActionSO
    {
        [Tooltip("Reference to the player movement speed configuration SO.")]
        public PlayerMovementSpeedDataSO speedData;

        protected override StateAction CreateAction() => new IdleSpeedAction(speedData);
    }

    public class IdleSpeedAction : StateAction
    {
        private MovementCore _movementCore;
        private readonly PlayerMovementSpeedDataSO _speedData;

        public IdleSpeedAction(PlayerMovementSpeedDataSO speedData)
        {
            _speedData = speedData;
        }

        public override void Awake(CoreSM stateMachine)
        {
            _movementCore = stateMachine.GetCachedComponent<MovementCore>();
        }

        public override void OnStateEnter()
        {
            _movementCore.SetMoveSpeed(_speedData != null ? _speedData.idleSpeed : 0f);
        }

        public override void OnUpdate() { }
    }
}