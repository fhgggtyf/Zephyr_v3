/*
 * WalkSpeedActionSO.cs
 * -------------------
 * Module:  Player / StateMachine / Actions
 * Purpose: Sets the player's walk speed when entering the Walk state.
 *          Speed value is read from PlayerMovementSpeedDataSO.
 * Scene:    GameManager (attached to player prefab).
 * Ch.Ref:   Ch.6.4 State Actions, Ch.6.8 Physics / FixedUpdate.
 */
using UnityEngine;
using Zephyr.Core.StateMachine;
using CoreSM = Zephyr.Core.StateMachine.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using Zephyr.Gameplay.Player.Core;
using Zephyr.Gameplay.Player.Data;

namespace Zephyr.Gameplay.Player.StateMachine
{
    [CreateAssetMenu(fileName = "WalkSpeedAction", menuName = "State Machines/Actions/Player/Walk Speed")]
    public class WalkSpeedActionSO : StateActionSO
    {
        [Tooltip("Reference to the player movement speed configuration SO.")]
        public PlayerMovementSpeedDataSO speedData;

        protected override StateAction CreateAction() => new WalkSpeedAction(speedData);
    }

    public class WalkSpeedAction : StateAction
    {
        private MovementCore _movementCore;
        private readonly PlayerMovementSpeedDataSO _speedData;

        public WalkSpeedAction(PlayerMovementSpeedDataSO speedData)
        {
            _speedData = speedData;
        }

        public override void Awake(CoreSM stateMachine)
        {
            _movementCore = stateMachine.GetCachedComponent<MovementCore>();
        }

        public override void OnStateEnter()
        {
            _movementCore.SetMoveSpeed(_speedData != null ? _speedData.walkSpeed : 3f);
        }

        public override void OnUpdate() { }
    }
}