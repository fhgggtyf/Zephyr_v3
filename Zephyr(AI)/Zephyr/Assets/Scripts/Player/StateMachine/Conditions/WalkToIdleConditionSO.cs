/*
 * WalkToIdleCondition.cs
 * ----------------------
 * Module:  Player / StateMachine / Conditions
 * Purpose: Condition for the Walk → Idle transition.
 *          The SO (WalkToIdleConditionSO) creates the runtime
 *          WalkToIdleCondition which returns true when the player
 *          has stopped providing horizontal movement input.
 * Scene:    GameManager (attached to player prefab).
 * Ch.Ref:   Ch.6.5 State Conditions, Ch.6.7 Owner Binding.
 */
using UnityEngine;
using Zephyr.Core.StateMachine;
using CoreSM = Zephyr.Core.StateMachine.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using Zephyr.Gameplay.Player.Input;

namespace Zephyr.Gameplay.Player.StateMachine
{
    [CreateAssetMenu(fileName = "WalkToIdleCondition", menuName = "State Machines/Conditions/Player/Walk To Idle")]
    public class WalkToIdleConditionSO : StateConditionSO
    {
        protected override Condition CreateCondition() => new WalkToIdleCondition();
    }

    public class WalkToIdleCondition : Condition
    {
        private PlayerInputReader _inputReader;

        public override void Awake(CoreSM stateMachine)
        {
            _inputReader = stateMachine.GetCachedComponent<PlayerInputReader>();
        }

        protected override bool Statement()
        {
            return _inputReader != null && !_inputReader.HasHorizontalInput;
        }
    }
}
