using UnityEngine;
using Zephyr.Core.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using CoreStateMachine = Zephyr.Core.StateMachine.StateMachine;

namespace Zephyr.Gameplay.Enemies
{
    public enum EnemyConditionKind
    {
        CanEngage, AlertFinished, InAttackRange, AttackReady, TelegraphFinished,
        AttackFinished, Hurt, HurtFinished, Dead, LoseTarget
    }

    [CreateAssetMenu(fileName = "EnemyCondition", menuName = "Zephyr/Enemies/State Condition")]
    public sealed class EnemyConditionSO : StateConditionSO
    {
        [SerializeField] private EnemyConditionKind _kind;
        public EnemyConditionKind Kind => _kind;
        protected override Condition CreateCondition() => new EnemyCondition(_kind);
    }

    internal sealed class EnemyCondition : Condition
    {
        private readonly EnemyConditionKind _kind;
        private EnemyBrain _brain;
        public EnemyCondition(EnemyConditionKind kind) => _kind = kind;
        public override void Awake(CoreStateMachine stateMachine) => _brain = stateMachine.GetCachedComponent<EnemyBrain>();
        protected override bool Statement()
        {
            if (_brain == null) return _kind == EnemyConditionKind.LoseTarget;
            switch (_kind)
            {
                case EnemyConditionKind.CanEngage:
                    return !_brain.IsDead && _brain.HasTarget && _brain.PlayerWithin(_brain.Stats.DetectionRange);
                case EnemyConditionKind.InAttackRange:
                    return !_brain.IsDead && _brain.PlayerWithin(_brain.Stats.AttackRange);
                case EnemyConditionKind.AttackReady:
                    return !_brain.IsDead && _brain.IsAttackReady;
                case EnemyConditionKind.AttackFinished:
                    return _brain.AttackFinished;
                case EnemyConditionKind.AlertFinished:
                case EnemyConditionKind.TelegraphFinished:
                case EnemyConditionKind.HurtFinished:
                    return _brain.PhaseFinished;
                case EnemyConditionKind.Hurt:
                    return !_brain.IsDead && _brain.WasRecentlyDamaged;
                case EnemyConditionKind.Dead:
                    return _brain.IsDead;
                case EnemyConditionKind.LoseTarget:
                    return !_brain.PlayerWithinLoseRange();
                default:
                    return false;
            }
        }
    }
}
