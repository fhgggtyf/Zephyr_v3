/*
 * JumpAnyToGroundedConditionSO.cs
 * --------------------------------
 * Module:  Player / StateMachine / Conditions
 * Purpose: Condition for transitioning from any jump state (JumpUp, JumpPeak,
 *          JumpFall) back to a grounded state after a valid landing.
 *          Ground contact alone is not enough because the ground checker may
 *          remain true for one frame after takeoff. Vertical velocity must also
 *          have stopped, which still allows landing on a higher platform during
 *          the ascent or peak phases.
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
    [CreateAssetMenu(fileName = "JumpAnyToGroundedCondition", menuName = "State Machines/Conditions/Player/Jump Any To Grounded")]
    public class JumpAnyToGroundedConditionSO : StateConditionSO
    {
        protected override Condition CreateCondition() => new JumpAnyToGroundedCondition();
    }

    public class JumpAnyToGroundedCondition : Condition
    {
        private MovementCore _movementCore;

        public override void Awake(CoreSM stateMachine)
        {
            _movementCore = stateMachine.GetCachedComponent<MovementCore>();
        }

        protected override bool Statement()
        {
            if (_movementCore == null)
            {
                Debug.LogWarning("[JumpAnyToGroundedCondition] _movementCore is null!");
                return false;
            }

            var verticalVelocity = _movementCore.VerticalVelocity;
            var result = _movementCore.HasStableGroundContact && verticalVelocity <= 0f;
            if (result)
            {
                Debug.Log($"[JumpAnyToGroundedCondition] TRUE: StableGround={_movementCore.HasStableGroundContact}, VerticalVelocity={verticalVelocity:F2}");
            }
            return result;
        }
    }
}
