/*
 * AnyStateToRollConditionSO.cs
 * -----------------------------
 * Module:  Player / StateMachine / Conditions
 * Purpose: State condition for transitioning into Roll from any state.
 *          Requires the roll button to be held (roll is a triggered action
 *          that consumes the input). Paired with RollToIdleConditionSO for
 *          exiting the roll state.
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
    [CreateAssetMenu(fileName = "AnyStateToRollCondition", menuName = "State Machines/Conditions/Player/Any State To Roll")]
    public class AnyStateToRollConditionSO : StateConditionSO
    {
        protected override Condition CreateCondition() => new AnyStateToRollCondition();
    }

    public class AnyStateToRollCondition : Condition
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
            if (_movementCore.IsRollOnCooldown) return false;

            return _inputReader.ConsumeRollInput();
        }
    }
}
