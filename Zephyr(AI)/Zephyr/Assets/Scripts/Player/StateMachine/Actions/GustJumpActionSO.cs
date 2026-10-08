using UnityEngine;
using Zephyr.Core.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using Zephyr.Gameplay.Player.Interaction;
using CoreSM = Zephyr.Core.StateMachine.StateMachine;

namespace Zephyr.Gameplay.Player.StateMachine
{
    [CreateAssetMenu(fileName = "GustJumpAction", menuName = "State Machines/Actions/Player/Gust Jump")]
    public sealed class GustJumpActionSO : StateActionSO
    {
        [SerializeField, Min(0.05f)] private float _duration = 0.8f;
        [SerializeField, Min(0.1f)] private float _easeOutPower = 2.4f;

        protected override StateAction CreateAction() => new GustJumpAction(_duration, _easeOutPower);
    }

    public sealed class GustJumpAction : StateAction
    {
        private readonly float _duration;
        private readonly float _easeOutPower;
        private PlayerGustJumpController _controller;

        public GustJumpAction(float duration, float easeOutPower)
        {
            _duration = duration;
            _easeOutPower = easeOutPower;
        }

        public override void Awake(CoreSM stateMachine)
        {
            _controller = stateMachine.GetCachedComponent<PlayerGustJumpController>();
        }

        public override void OnStateEnter() => _controller?.BeginGustJump(_duration, _easeOutPower);
        public override void OnUpdate() => _controller?.TickGustJump();
        public override void OnStateExit() => _controller?.EndGustJump();
    }
}
