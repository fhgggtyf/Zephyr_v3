/*
 * IdleToWalkCondition.cs
 * ----------------------
 * Module:  Player / StateMachine / Conditions
 * Purpose: Condition for the Idle → Walk transition.
 *          The SO (IdleToWalkConditionSO) creates the runtime
 *          IdleToWalkCondition which returns true when the player
 *          has horizontal movement input (|MoveInput.x| > 0.01).
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
    [CreateAssetMenu(fileName = "IdleToWalkCondition", menuName = "State Machines/Conditions/Player/Idle To Walk")]
    public class IdleToWalkConditionSO : StateConditionSO
    {
        protected override Condition CreateCondition() => new IdleToWalkCondition();
    }

    public class IdleToWalkCondition : Condition
    {
        private PlayerInputReader _inputReader;

        public override void Awake(CoreSM stateMachine)
        {
            _inputReader = stateMachine.GetCachedComponent<PlayerInputReader>();
        }

        protected override bool Statement()
        {
            return _inputReader != null && _inputReader.HasHorizontalInput;
        }
    }
}
