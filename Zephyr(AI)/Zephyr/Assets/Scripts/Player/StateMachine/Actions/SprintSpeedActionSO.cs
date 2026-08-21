/*
 * SprintSpeedActionSO.cs
 * ---------------------
 * Module:  Player / StateMachine / Actions
 * Purpose: Sets the sprint (run) move speed on MovementCore when entering
 *          the Sprint state. Speed value is read from PlayerMovementSpeedDataSO.
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
    [CreateAssetMenu(fileName = "SprintSpeedAction", menuName = "State Machines/Actions/Player/Sprint Speed")]
    public class SprintSpeedActionSO : StateActionSO
    {
        [Tooltip("Reference to the player movement speed configuration SO.")]
        public PlayerMovementSpeedDataSO speedData;

        protected override StateAction CreateAction() => new SprintSpeedAction(speedData);
    }

    public class SprintSpeedAction : StateAction
    {
        private MovementCore _movementCore;
        private PlayerResourceController _resources;
        private readonly PlayerMovementSpeedDataSO _speedData;

        public SprintSpeedAction(PlayerMovementSpeedDataSO speedData)
        {
            _speedData = speedData;
        }

        public override void Awake(CoreSM stateMachine)
        {
            _movementCore = stateMachine.GetCachedComponent<MovementCore>();
            stateMachine.TryGetCachedComponent(out _resources);
        }

        public override void OnStateEnter()
        {
            _movementCore.SetMoveSpeed(_speedData != null ? _speedData.sprintSpeed : 8f);
        }

        public override void OnUpdate()
        {
            _resources?.ConsumeSprintStamina(Time.deltaTime);
        }
    }
}
