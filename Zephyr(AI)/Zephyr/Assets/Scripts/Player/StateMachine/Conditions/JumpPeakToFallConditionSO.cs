/*
 * JumpPeakToFallConditionSO.cs
 * ----------------------------
 * Module:  Player / StateMachine / Conditions
 * Purpose: Condition for transitioning from JumpPeak to JumpFall.
 *          Triggers when the peak transition animation completes (via
 *          SpriteAnimator.IsPeakComplete flag set by an Animation Event).
 * Scene:    GameManager (attached to player prefab).
 * Ch.Ref:   Ch.6.5 State Conditions, Ch.6.10 Presentation Layer.
 */
using UnityEngine;
using Zephyr.Core.StateMachine;
using CoreSM = Zephyr.Core.StateMachine.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using Zephyr.Gameplay.Player.Visual;

namespace Zephyr.Gameplay.Player.StateMachine
{
    [CreateAssetMenu(fileName = "JumpPeakToFallCondition", menuName = "State Machines/Conditions/Player/Jump Peak To Fall")]
    public class JumpPeakToFallConditionSO : StateConditionSO
    {
        protected override Condition CreateCondition() => new JumpPeakToFallCondition();
    }

    public class JumpPeakToFallCondition : Condition
    {
        private SpriteAnimator _spriteAnimator;

        public override void Awake(CoreSM stateMachine)
        {
            _spriteAnimator = stateMachine.GetCachedComponent<SpriteAnimator>();
        }

        protected override bool Statement()
        {
            if (_spriteAnimator == null)
            {
                Debug.LogWarning("[JumpPeakToFallCondition] _spriteAnimator is null!");
                return false;
            }

            // JumpMid must finish its presentation animation before entering
            // the falling phase. Prefer the authored animation event when one
            // exists, but also accept the clip's normalized completion. The
            // current JumpDown clip has no event, so relying only on
            // IsPeakComplete would leave the player permanently in JumpMid.
            // Landing is handled independently by JumpAnyToGrounded and does
            // not shorten this presentation phase.
            return _spriteAnimator.IsPeakComplete
                || _spriteAnimator.IsCurrentAnimationComplete();
        }
    }
}
