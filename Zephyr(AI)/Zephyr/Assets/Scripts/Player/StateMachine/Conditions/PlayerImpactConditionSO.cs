using UnityEngine;
using Zephyr.Core.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using Zephyr.Gameplay.Player.Core;
using CoreSM = Zephyr.Core.StateMachine.StateMachine;

namespace Zephyr.Gameplay.Player.StateMachine
{
    public enum PlayerImpactConditionKind
    {
        Knockback,
        Hitback,
        Getup,
        Complete
    }

    [CreateAssetMenu(fileName = "PlayerImpactCondition", menuName = "State Machines/Conditions/Player/Impact")]
    public sealed class PlayerImpactConditionSO : StateConditionSO
    {
        [SerializeField] private PlayerImpactConditionKind _kind;
        protected override Condition CreateCondition() => new PlayerImpactCondition(_kind);
    }

    public sealed class PlayerImpactCondition : Condition
    {
        private readonly PlayerImpactConditionKind _kind;
        private PlayerImpactController _controller;

        public PlayerImpactCondition(PlayerImpactConditionKind kind) => _kind = kind;

        public override void Awake(CoreSM stateMachine)
        {
            stateMachine.TryGetCachedComponent(out _controller);
        }

        protected override bool Statement()
        {
            if (_controller == null) return false;
            return _kind switch
            {
                PlayerImpactConditionKind.Knockback => _controller.WantsKnockbackState,
                PlayerImpactConditionKind.Hitback => _controller.WantsHitbackState,
                PlayerImpactConditionKind.Getup => _controller.WantsGetupState,
                PlayerImpactConditionKind.Complete => _controller.IsImpactComplete,
                _ => false
            };
        }
    }
}
