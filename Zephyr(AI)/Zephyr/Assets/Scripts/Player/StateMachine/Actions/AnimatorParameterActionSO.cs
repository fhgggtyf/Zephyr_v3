/*
 * AnimatorParameterAction.cs
 * --------------------------
 * Module:  Player / StateMachine / Actions
 * Purpose: Controls animation state transitions via SpriteAnimator.
 *          The SO (AnimatorParameterActionSO) exposes animName, whenToRun,
 *          and playSpeed in the Inspector. The runtime AnimatorParameterAction
 *          gets SpriteAnimator from the StateMachine cache and calls
 *          PlayAnimation() at the specified moment (OnStateEnter/Exit/Update).
 * Scene:    GameManager (attached to player prefab).
 * Ch.Ref:   Ch.6.4 State Actions, Ch.6.10 Presentation Layer.
 */
using UnityEngine;
using Zephyr.Core.StateMachine;
using CoreSM = Zephyr.Core.StateMachine.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using Zephyr.Gameplay.Player.Visual;
using Moment = Zephyr.Core.StateMachine.StateAction.SpecificMoment;

namespace Zephyr.Gameplay.Player.StateMachine
{
    [CreateAssetMenu(fileName = "AnimatorParameterAction", menuName = "State Machines/Actions/Player/Set Animator Parameter")]
    public class AnimatorParameterActionSO : StateActionSO
    {
        [Tooltip("Name of the animation state to play (must match Animator state name)")]
        public string animName = default;

        [Tooltip("When to trigger the animation change during the state lifecycle")]
        public Moment whenToRun = default;

        [Tooltip("Playback speed multiplier. 1 = normal, 2 = double, 0.5 = half")]
        public float playSpeed = 1f;

        protected override StateAction CreateAction() =>
            new AnimatorParameterAction(animName, playSpeed);
    }

    public class AnimatorParameterAction : StateAction
    {
        private SpriteAnimator _spriteAnimator;
        private AnimatorParameterActionSO _originSO => (AnimatorParameterActionSO)OriginSO;
        private readonly string _animName;
        private readonly float _playSpeed;

        public AnimatorParameterAction(string animName, float playSpeed)
        {
            _animName = animName;
            _playSpeed = playSpeed;
        }

        public override void Awake(CoreSM stateMachine)
        {
            _spriteAnimator = stateMachine.GetCachedComponent<SpriteAnimator>();
        }

        public override void OnStateEnter()
        {
            if (_originSO.whenToRun == SpecificMoment.OnStateEnter)
                PlayAnimation(true);
        }

        public override void OnStateExit()
        {
            if (_originSO.whenToRun == SpecificMoment.OnStateExit)
                PlayAnimation();
        }

        public override void OnUpdate()
        {
            if (_originSO.whenToRun == SpecificMoment.OnUpdate)
                PlayAnimation();
        }

        private void PlayAnimation(bool restart = false)
        {
            if (_spriteAnimator == null) return;
            _spriteAnimator.PlayAnimation(_animName, _playSpeed, restart);
        }
    }
}
