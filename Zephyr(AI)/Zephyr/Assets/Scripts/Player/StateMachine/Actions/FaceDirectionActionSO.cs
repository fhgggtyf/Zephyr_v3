/*
 * FaceDirectionAction.cs
 * ----------------------
 * Module:  Player / StateMachine / Actions
 * Purpose: Controls the player's facing direction based on horizontal input.
 *          The SO (FaceDirectionActionSO) creates the runtime FaceDirectionAction
 *          which runs every frame, reading PlayerInputReader.MoveInput.x and
 *          delegating to MovementCore.SetFacing().
 *          Facing only changes on non-zero horizontal input; stopping preserves
 *          the last facing direction (natural feel).
 * Scene:    GameManager (attached to player prefab).
 * Ch.Ref:   Ch.6.4 State Actions, Ch.6.7 Owner Binding, Ch.11 Enemy AI.
 */
using UnityEngine;
using Zephyr.Core.StateMachine;
using CoreSM = Zephyr.Core.StateMachine.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using Zephyr.Gameplay.Player.Core;
using Zephyr.Gameplay.Player.Input;

namespace Zephyr.Gameplay.Player.StateMachine
{
    [CreateAssetMenu(fileName = "FaceDirectionAction", menuName = "State Machines/Actions/Player/Face Direction")]
    public class FaceDirectionActionSO : StateActionSO
    {
        protected override StateAction CreateAction() => new FaceDirectionAction();
    }

    public class FaceDirectionAction : StateAction
    {
        private MovementCore _movementCore;
        private PlayerInputReader _inputReader;

        public override void Awake(CoreSM stateMachine)
        {
            _movementCore = stateMachine.GetCachedComponent<MovementCore>();
            _inputReader = stateMachine.GetCachedComponent<PlayerInputReader>();
        }

        public override void OnUpdate()
        {
            if (_inputReader == null || _movementCore == null) return;

            var horizontalInput = _inputReader.MoveInput.x;
            _movementCore.SetFacing(horizontalInput);
        }
    }
}
