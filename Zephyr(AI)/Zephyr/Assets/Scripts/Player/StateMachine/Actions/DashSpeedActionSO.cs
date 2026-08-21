/*
 * DashSpeedActionSO.cs
 * ---------------------
 * Module:  Player / StateMachine / Actions
 * Purpose: Sets the dash move speed on MovementCore when entering
 *          the Dash state. Speed value is read from PlayerMovementSpeedDataSO.
 *          Follows the same separation pattern as RollSpeedActionSO.
 * Scene:    GameManager (attached to player prefab).
 * Ch.Ref:   Ch.6.4 State Actions, Ch.6.8 Physics / Speed Control.
 */
using UnityEngine;
using Zephyr.Core.StateMachine;
using CoreSM = Zephyr.Core.StateMachine.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using Zephyr.Gameplay.Player.Core;
using Zephyr.Gameplay.Player.Data;

namespace Zephyr.Gameplay.Player.StateMachine
{
    [CreateAssetMenu(fileName = "DashSpeedAction", menuName = "State Machines/Actions/Player/Dash Speed")]
    public class DashSpeedActionSO : StateActionSO
    {
        [Tooltip("Reference to the player movement speed configuration SO.")]
        public PlayerMovementSpeedDataSO speedData;

        protected override StateAction CreateAction() => new DashSpeedAction(speedData);
    }

    public class DashSpeedAction : StateAction
    {
        private MovementCore _movementCore;
        private readonly PlayerMovementSpeedDataSO _speedData;

        public DashSpeedAction(PlayerMovementSpeedDataSO speedData)
        {
            _speedData = speedData;
        }

        public override void Awake(CoreSM stateMachine)
        {
            _movementCore = stateMachine.GetCachedComponent<MovementCore>();
        }

        public override void OnStateEnter()
        {
            _movementCore.SetMoveSpeed(_speedData != null ? _speedData.dashSpeed : 14f);
        }

        public override void OnUpdate() { }
    }
}
