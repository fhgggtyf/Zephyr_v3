/*
 * VariableSpeedActionSO.cs
 * ------------------------
 * Module:  Player / StateMachine / Actions
 * Purpose: Dynamically adjusts move speed based on sprint input each frame.
 *          Runs in OnUpdate() to allow in-air speed control: pressing sprint
 *          yields sprint speed, pressing direction only yields walk speed,
 *          no input yields idle (0) speed. Unlike static speed actions
 *          (IdleSpeed, WalkSpeed, SprintSpeed) which set speed once on
 *          state enter, this action re-evaluates speed every frame.
 * Scene:    GameManager (attached to player prefab).
 * Ch.Ref:   Ch.6.4 State Actions, Ch.6.8 Physics / Air Control.
 */
using UnityEngine;
using Zephyr.Core.StateMachine;
using CoreSM = Zephyr.Core.StateMachine.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using Zephyr.Gameplay.Player.Core;
using Zephyr.Gameplay.Player.Data;
using Zephyr.Gameplay.Player.Input;

namespace Zephyr.Gameplay.Player.StateMachine
{
    [CreateAssetMenu(fileName = "VariableSpeedAction", menuName = "State Machines/Actions/Player/Variable Speed")]
    public class VariableSpeedActionSO : StateActionSO
    {
        [Tooltip("Reference to the player movement speed configuration SO.")]
        public PlayerMovementSpeedDataSO speedData;

        protected override StateAction CreateAction() => new VariableSpeedAction(speedData);
    }

    public class VariableSpeedAction : StateAction
    {
        private MovementCore _movementCore;
        private PlayerInputReader _inputReader;
        private readonly PlayerMovementSpeedDataSO _speedData;

        public VariableSpeedAction(PlayerMovementSpeedDataSO speedData)
        {
            _speedData = speedData;
        }

        public override void Awake(CoreSM stateMachine)
        {
            _movementCore = stateMachine.GetCachedComponent<MovementCore>();
            _inputReader = stateMachine.GetCachedComponent<PlayerInputReader>();
        }

        public override void OnUpdate()
        {
            if (_movementCore == null || _speedData == null) return;

            var hasInput = _inputReader != null && _inputReader.HasHorizontalInput;
            var isSprinting = _inputReader != null && _inputReader.HasSprintInput;

            if (!hasInput)
            {
                _movementCore.SetMoveSpeed(_speedData.idleSpeed);
            }
            else if (isSprinting)
            {
                _movementCore.SetMoveSpeed(_speedData.sprintSpeed);
            }
            else
            {
                _movementCore.SetMoveSpeed(_speedData.walkSpeed);
            }
        }

        public override void OnStateEnter()
        {
            // Initialize speed on entry; OnUpdate will refine it each frame
            if (_movementCore == null || _speedData == null) return;
            _movementCore.SetMoveSpeed(_speedData.idleSpeed);
        }

        public override void OnStateExit() { }
    }
}