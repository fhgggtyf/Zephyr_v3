/*
 * JumpUpToPeakConditionSO.cs
 * --------------------------
 * Module:  Player / StateMachine / Conditions
 * Purpose: Condition for transitioning from JumpUp to JumpPeak.
 *          Triggers when vertical velocity drops to zero or below,
 *          meaning the player has reached the apex of the jump.
 * Scene:    GameManager (attached to player prefab).
 * Ch.Ref:   Ch.6.5 State Conditions, Ch.6.8 Physics / Jump Mechanics.
 */
using UnityEngine;
using Zephyr.Core.StateMachine;
using CoreSM = Zephyr.Core.StateMachine.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using Zephyr.Gameplay.Player.Core;

namespace Zephyr.Gameplay.Player.StateMachine
{
    [CreateAssetMenu(fileName = "JumpUpToPeakCondition", menuName = "State Machines/Conditions/Player/Jump Up To Peak")]
    public class JumpUpToPeakConditionSO : StateConditionSO
    {
        protected override Condition CreateCondition() => new JumpUpToPeakCondition();
    }

    public class JumpUpToPeakCondition : Condition
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
                Debug.LogWarning("[JumpUpToPeakCondition] _movementCore is null!");
                return false;
            }

            var result = _movementCore.VerticalVelocity <= 0f;
            if (result)
            {
                Debug.Log($"[JumpUpToPeakCondition] TRUE: vel.y={_movementCore.VerticalVelocity:F2}");
            }
            return result;
        }
    }
}