/*
 * ResetAirJumpsActionSO.cs
 * ------------------------
 * Module:  Player / StateMachine / Actions
 * Purpose: Resets the air jump (double jump) availability when entering any
 *          grounded state. Should be placed as an entry action on all grounded
 *          states (Idle, Walk, Sprint, CrouchIdle, CrouchMoving) so that
 *          landing always restores the player's air jump.
 * Scene:    GameManager (attached to player prefab).
 * Ch.Ref:   Ch.6.4 State Actions.
 */
using UnityEngine;
using Zephyr.Core.StateMachine;
using CoreSM = Zephyr.Core.StateMachine.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using Zephyr.Gameplay.Player.Core;

namespace Zephyr.Gameplay.Player.StateMachine
{
    [CreateAssetMenu(fileName = "ResetAirJumpsAction", menuName = "State Machines/Actions/Player/Reset Air Jumps")]
    public class ResetAirJumpsActionSO : StateActionSO
    {
        protected override StateAction CreateAction() => new ResetAirJumpsAction();
    }

    public class ResetAirJumpsAction : StateAction
    {
        private MovementCore _movementCore;

        public override void Awake(CoreSM stateMachine)
        {
            _movementCore = stateMachine.GetCachedComponent<MovementCore>();
        }

        public override void OnStateEnter()
        {
            if (_movementCore == null) return;

            _movementCore.ResetAirJumps();
        }

        public override void OnUpdate() { }
    }
}
