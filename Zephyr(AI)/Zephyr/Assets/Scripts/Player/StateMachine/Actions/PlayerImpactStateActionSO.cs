using UnityEngine;
using Zephyr.Core.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using Zephyr.Gameplay.Player.Core;
using CoreSM = Zephyr.Core.StateMachine.StateMachine;

namespace Zephyr.Gameplay.Player.StateMachine
{
    [CreateAssetMenu(fileName = "PlayerImpactStateAction", menuName = "State Machines/Actions/Player/Impact State")]
    public sealed class PlayerImpactStateActionSO : StateActionSO
    {
        [SerializeField] private PlayerImpactController.ImpactState _state;
        protected override StateAction CreateAction() => new PlayerImpactStateAction(_state);
    }

    public sealed class PlayerImpactStateAction : StateAction
    {
        private readonly PlayerImpactController.ImpactState _state;
        private PlayerImpactController _controller;

        public PlayerImpactStateAction(PlayerImpactController.ImpactState state) => _state = state;

        public override void Awake(CoreSM stateMachine)
        {
            stateMachine.TryGetCachedComponent(out _controller);
        }

        public override void OnStateEnter() => _controller?.EnterImpactState(_state);
        public override void OnUpdate() => _controller?.UpdateImpactState(_state);
        public override void OnStateExit() => _controller?.ExitImpactState(_state);
    }
}
