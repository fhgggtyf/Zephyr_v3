using UnityEngine;
using Zephyr.Core.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using Zephyr.Gameplay.Player.Core;
using Zephyr.Gameplay.Player.Interaction;
using CoreSM = Zephyr.Core.StateMachine.StateMachine;

namespace Zephyr.Gameplay.Player.StateMachine
{
    public enum GustJumpConditionKind
    {
        Request,
        Complete
    }

    [CreateAssetMenu(fileName = "GustJumpCondition", menuName = "State Machines/Conditions/Player/Gust Jump")]
    public sealed class GustJumpConditionSO : StateConditionSO
    {
        [SerializeField] private GustJumpConditionKind _kind;
        protected override Condition CreateCondition() => new GustJumpCondition(_kind);
    }

    public sealed class GustJumpCondition : Condition
    {
        private readonly GustJumpConditionKind _kind;
        private PlayerGustJumpController _controller;
        private PlayerColliderController _colliderController;

        public GustJumpCondition(GustJumpConditionKind kind) => _kind = kind;

        public override void Awake(CoreSM stateMachine)
        {
            _controller = stateMachine.GetCachedComponent<PlayerGustJumpController>();
            stateMachine.TryGetCachedComponent(out _colliderController);
        }

        protected override bool Statement()
        {
            if (_controller == null) return false;

            if (_kind == GustJumpConditionKind.Request)
            {
                if (_colliderController != null && !_colliderController.CanUseStandingProfile())
                    return false;

                return _controller.HasRequest;
            }

            return _controller.IsComplete;
        }
    }
}
