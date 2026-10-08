/*
 * AnyStateToFallConditionSO.cs
 * ----------------------------
 * Module:  Player / StateMachine / Conditions
 * Purpose: Condition for transitioning into the JumpFall state from any
 *          grounded state when the player loses ground contact.
 *          Triggers when the player is NOT grounded, allowing walking
 *          off a ledge without jumping to enter the fall state.
 * Scene:    GameManager (attached to player prefab).
 * Ch.Ref:   Ch.6.5 State Conditions, Ch.6.8 Physics / Ground Detection.
 */
using UnityEngine;
using Zephyr.Core.StateMachine;
using CoreSM = Zephyr.Core.StateMachine.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using Zephyr.Gameplay.Player.Core;

namespace Zephyr.Gameplay.Player.StateMachine
{
    [CreateAssetMenu(fileName = "AnyStateToFallCondition", menuName = "State Machines/Conditions/Player/Any State To Fall")]
    public class AnyStateToFallConditionSO : StateConditionSO
    {
        protected override Condition CreateCondition() => new AnyStateToFallCondition();
    }

    public class AnyStateToFallCondition : Condition
    {
        private const float k_minimumFallingSpeed = -0.1f;
        private MovementCore _movementCore;

        public override void Awake(CoreSM stateMachine)
        {
            _movementCore = stateMachine.GetCachedComponent<MovementCore>();
        }

        protected override bool Statement()
        {
            if (_movementCore == null) return false;

            // A brief ground-probe miss at touchdown must not send the player
            // from a grounded locomotion state back into JumpEnd. Require actual
            // downward motion as well as lost contact; walking off a ledge still
            // enters the falling state as soon as gravity starts pulling down.
            return !_movementCore.IsGrounded
                   && !_movementCore.HasGroundedGrace
                   && _movementCore.VerticalVelocity < k_minimumFallingSpeed;
        }
    }
}
