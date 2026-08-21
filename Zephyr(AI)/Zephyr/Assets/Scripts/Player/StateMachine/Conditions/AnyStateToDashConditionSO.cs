/*
 * AnyStateToDashConditionSO.cs
 * -----------------------------
 * Module:  Player / StateMachine / Conditions
 * Purpose: State condition for transitioning into Dash from any state.
 *          Shares the roll input button with roll: grounded → roll,
 *          airborne → dash. Requires: player is NOT grounded (dash is
 *          air-only), dash is not on cooldown, and the roll input was
 *          pressed (consumable edge-triggered request). The grounded check
 *          makes this mutually exclusive with AnyStateToRollCondition.
 *          Paired with DashCompleteConditionSO for exiting the dash state.
 * Scene:    GameManager (attached to player prefab).
 * Ch.Ref:   Ch.6.5 State Conditions, Ch.6.7 Owner Binding.
 */
using UnityEngine;
using Zephyr.Core.StateMachine;
using CoreSM = Zephyr.Core.StateMachine.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using Zephyr.Gameplay.Player.Core;
using Zephyr.Gameplay.Player.Input;

namespace Zephyr.Gameplay.Player.StateMachine
{
    [CreateAssetMenu(fileName = "AnyStateToDashCondition", menuName = "State Machines/Conditions/Player/Any State To Dash")]
    public class AnyStateToDashConditionSO : StateConditionSO
    {
        protected override Condition CreateCondition() => new AnyStateToDashCondition();
    }

    public class AnyStateToDashCondition : Condition
    {
        private PlayerInputReader _inputReader;
        private MovementCore _movementCore;

        public override void Awake(CoreSM stateMachine)
        {
            _inputReader = stateMachine.GetCachedComponent<PlayerInputReader>();
            _movementCore = stateMachine.GetCachedComponent<MovementCore>();
        }

        protected override bool Statement()
        {
            if (_inputReader == null || _movementCore == null) return false;

            // Dash is air-only — cannot dash while grounded
            if (_movementCore.IsGrounded) return false;

            if (_movementCore.IsDashOnCooldown) return false;

            // Shares the roll input button — grounded check above makes this
            // mutually exclusive with AnyStateToRollCondition's consumption
            return _inputReader.ConsumeRollInput();
        }
    }
}
