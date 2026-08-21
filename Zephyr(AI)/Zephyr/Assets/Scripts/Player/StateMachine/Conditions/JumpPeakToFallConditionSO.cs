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

            var result = _spriteAnimator.IsPeakComplete;
            if (result)
            {
                Debug.Log($"[JumpPeakToFallCondition] TRUE: IsPeakComplete={_spriteAnimator.IsPeakComplete}");
            }
            return result;
        }
    }
}