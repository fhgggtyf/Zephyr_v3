using UnityEngine;
using Zephyr.Core.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using CoreStateMachine = Zephyr.Core.StateMachine.StateMachine;

namespace Zephyr.Gameplay.Enemies
{
    public enum EnemyActionKind { Idle, Patrol, Alert, Chase, Telegraph, Attack, Hurt, Death }

    [CreateAssetMenu(fileName = "EnemyAction", menuName = "Zephyr/Enemies/State Action")]
    public sealed class EnemyActionSO : StateActionSO
    {
        [SerializeField] private EnemyActionKind _kind;
        public EnemyActionKind Kind => _kind;
        protected override StateAction CreateAction() => new EnemyAction(_kind);
    }

    internal sealed class EnemyAction : StateAction
    {
        private readonly EnemyActionKind _kind;
        private EnemyBrain _brain;
        private EnemyAnimation _animation;
        private EnemyAttackController _attackController;
        private Vector2 _patrolOrigin;
        private float _patrolDirection = 1f;
        public EnemyAction(EnemyActionKind kind) => _kind = kind;
        public override void Awake(CoreStateMachine stateMachine)
        {
            _brain = stateMachine.GetCachedComponent<EnemyBrain>();
            _animation = stateMachine.GetCachedComponent<EnemyAnimation>();
            stateMachine.TryGetCachedComponent(out _attackController);
        }

        public override void OnStateEnter()
        {
            if (_brain == null) return;
            if (_kind == EnemyActionKind.Attack)
            {
                EnemyAttackDefinition attack = _attackController?.GetAttack(0);
                _animation?.PlayAttack(attack?.AnimationState, attack?.TotalDuration ?? _brain.Stats.AttackDuration);
            }
            else
            {
                _animation?.Play(_kind);
            }
            if (_kind != EnemyActionKind.Chase) _brain.Motor.Stop();
            if (_kind == EnemyActionKind.Patrol)
            {
                _patrolOrigin = _brain.transform.position;
            }
            else if (_kind == EnemyActionKind.Alert)
            {
                _brain.BeginAlert();
                _brain.FaceTarget();
            }
            else if (_kind == EnemyActionKind.Telegraph)
            {
                _brain.BeginTelegraph();
                _brain.FaceTarget();
            }
            else if (_kind == EnemyActionKind.Hurt)
            {
                _brain.BeginHurt();
            }
            else if (_kind == EnemyActionKind.Attack)
            {
                _brain.FaceTarget();
                if (_attackController == null || !_attackController.TryStartAttack())
                    _brain.MarkAttackFinished();
            }
            else if (_kind == EnemyActionKind.Death)
            {
                foreach (Collider2D collider in _brain.GetComponentsInChildren<Collider2D>())
                    collider.enabled = false;
            }
        }

        public override void OnUpdate()
        {
            if (_brain == null || _brain.IsDead && _kind != EnemyActionKind.Death) return;
            switch (_kind)
            {
                case EnemyActionKind.Idle:
                case EnemyActionKind.Alert:
                case EnemyActionKind.Telegraph:
                case EnemyActionKind.Hurt:
                case EnemyActionKind.Death:
                    _brain.Motor.Stop();
                    break;
                case EnemyActionKind.Patrol:
                    UpdatePatrol();
                    break;
                case EnemyActionKind.Chase:
                    UpdateChase();
                    break;
                case EnemyActionKind.Attack:
                    _brain.Motor.Stop();
                    break;
            }
        }

        public override void OnStateExit()
        {
            if (_kind == EnemyActionKind.Chase || _kind == EnemyActionKind.Attack)
                _brain?.Motor.Stop();
            if (_kind == EnemyActionKind.Telegraph)
                _brain?.EndTelegraph();
            if (_kind == EnemyActionKind.Attack)
                _attackController?.CancelAttack();
        }

        private void UpdateChase()
        {
            if (!_brain.HasTarget) return;
            if (_brain.PlayerWithin(_brain.Stats.AttackRange)) _brain.Motor.Stop();
            else _brain.Motor.SetMoveDirection((_brain.Target.position - _brain.transform.position).normalized);
        }

        private void UpdatePatrol()
        {
            float distance = _brain.Stats.PatrolDistance;
            float offset = _brain.transform.position.x - _patrolOrigin.x;
            if (offset >= distance) _patrolDirection = -1f;
            else if (offset <= -distance) _patrolDirection = 1f;
            _brain.Motor.SetMoveDirection(new Vector2(_patrolDirection, 0f), _brain.Stats.PatrolSpeed);
        }

    }
}
