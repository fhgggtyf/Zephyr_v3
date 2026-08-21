/*
 * RollCompleteConditionSO.cs
 * ---------------------------
 * Module:  Player / StateMachine / Conditions
 * Purpose: State condition for exiting the Roll state. Returns true when
 *          the roll animation reaches normalized time 1 or its Animation
 *          Event marks completion. Roll cannot be interrupted; it must play
 *          through completely.
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
    [CreateAssetMenu(fileName = "RollCompleteCondition", menuName = "State Machines/Conditions/Player/Roll Complete")]
    public class RollCompleteConditionSO : StateConditionSO
    {
        protected override Condition CreateCondition() => new RollCompleteCondition();
    }

    public class RollCompleteCondition : Condition
    {
        private SpriteAnimator _spriteAnimator;

        public override void Awake(CoreSM stateMachine)
        {
            _spriteAnimator = stateMachine.GetCachedComponent<SpriteAnimator>();
        }

        protected override bool Statement()
        {
            return _spriteAnimator.IsRollComplete
                || _spriteAnimator.IsCurrentAnimationComplete();
        }
    }
}
