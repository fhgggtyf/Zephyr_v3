/*
 * MoveInputAction.cs
 * -------------------
 * Module:  Player / StateMachine / Actions
 * Purpose: Bridges PlayerInputReader.MoveInput to MovementCore.SetMoveInput().
 *          Runs every frame (OnUpdate) to feed the current input vector into
 *          MovementCore, which then applies velocity in FixedUpdate.
 *          This is the only point where input data enters the movement system.
 *          MovementCore itself has no knowledge of input systems.
 * Scene:    GameManager (attached to player prefab).
 * Ch.Ref:   Ch.6.4 State Actions, Ch.6.7 Owner Binding, Ch.6.8 Physics.
 */
using UnityEngine;
using Zephyr.Core.StateMachine;
using CoreSM = Zephyr.Core.StateMachine.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using Zephyr.Gameplay.Player.Core;
using Zephyr.Gameplay.Player.Input;

namespace Zephyr.Gameplay.Player.StateMachine
{
    [CreateAssetMenu(fileName = "MoveInputAction", menuName = "State Machines/Actions/Player/Move Input")]
    public class MoveInputActionSO : StateActionSO
    {
        protected override StateAction CreateAction() => new MoveInputAction();
    }

    public class MoveInputAction : StateAction
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

            _movementCore.SetMoveInput(_inputReader.MoveInput);
        }
    }
}
