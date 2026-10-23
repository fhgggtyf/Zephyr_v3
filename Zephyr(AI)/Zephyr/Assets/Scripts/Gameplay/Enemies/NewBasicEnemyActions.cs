using UnityEngine;
using Zephyr.Core.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using CoreSM = Zephyr.Core.StateMachine.StateMachine;

namespace Zephyr.Gameplay.Enemies
{

    public sealed class NewBasicEnemyIdleAction : StateAction
    {
        private EnemyBrain _brain;
        private EnemyAnimation _animation;
        public override void Awake(CoreSM sm) { _brain = sm.GetCachedComponent<EnemyBrain>(); sm.TryGetCachedComponent(out _animation); }
        public override void OnStateEnter() { _brain?.Motor.Stop(); _animation?.Play(EnemyActionKind.Idle); }
        public override void OnUpdate() { _brain?.Motor.Stop(); }
        public override void OnStateExit() { _brain?.Motor.Stop(); }
    }

    public sealed class NewBasicEnemyPatrolAction : StateAction
    {
        private EnemyBrain _brain;
        private EnemyAnimation _animation;
        private float _direction;
        public override void Awake(CoreSM sm) { _brain = sm.GetCachedComponent<EnemyBrain>(); sm.TryGetCachedComponent(out _animation); }
public override void OnStateEnter()
        {
            _direction = _brain != null && _brain.Motor.Facing.x >= 0f ? 1f : -1f;
        }
public override void OnUpdate()
        {
            if (_brain == null) return;
            if (!_brain.Motor.CanPatrolInDirection(_direction))
            {
                _direction *= -1f;
                if (!_brain.Motor.CanPatrolInDirection(_direction))
                {
                    _brain.Motor.Stop();
                    _animation?.Play(EnemyActionKind.Idle);
                    return;
                }
            }
            _brain.Motor.SetMoveDirection(Vector2.right * _direction, _brain.Stats.PatrolSpeed);
            _animation?.Play(EnemyActionKind.Patrol);
        }
        public override void OnStateExit() { _brain?.Motor.Stop(); }
    }

    public sealed class NewBasicEnemyAlertAction : StateAction
    {
        private EnemyBrain _brain;
        private EnemyAnimation _animation;
        public override void Awake(CoreSM sm) { _brain = sm.GetCachedComponent<EnemyBrain>(); sm.TryGetCachedComponent(out _animation); }
        public override void OnStateEnter() { _brain?.Motor.Stop(); _brain?.BeginAlert(); _brain?.FaceTarget(); _animation?.Play(EnemyActionKind.Alert); }
        public override void OnUpdate() { _brain?.Motor.Stop(); }
        public override void OnStateExit() { _brain?.Motor.Stop(); }
    }

    public sealed class NewBasicEnemyChaseAction : StateAction
    {
        private EnemyBrain _brain;
        private EnemyAnimation _animation;
        public override void Awake(CoreSM sm) { _brain = sm.GetCachedComponent<EnemyBrain>(); sm.TryGetCachedComponent(out _animation); }
        public override void OnStateEnter() { _animation?.Play(EnemyActionKind.Chase); }
public override void OnUpdate()
        {
            if (_brain == null || !_brain.HasTarget)
            {
                _brain?.Motor.Stop();
                _animation?.Play(EnemyActionKind.Idle);
                return;
            }

            Vector2 delta = _brain.Target.position - _brain.transform.position;
            if (Mathf.Abs(delta.x) <= _brain.Stats.AttackRange)
            {
                _brain.Motor.Stop();
                _brain.FaceTarget();
                _animation?.Play(EnemyActionKind.Idle);
                return;
            }

            _brain.Motor.SetMoveDirection(new Vector2(Mathf.Sign(delta.x), 0f));
            _animation?.Play(EnemyActionKind.Chase);
        }
        public override void OnStateExit() { _brain?.Motor.Stop(); }
    }

    public sealed class NewBasicEnemyTelegraphAction : StateAction
    {
        private EnemyBrain _brain;
        private EnemyAnimation _animation;
        public override void Awake(CoreSM sm) { _brain = sm.GetCachedComponent<EnemyBrain>(); sm.TryGetCachedComponent(out _animation); }
        public override void OnStateEnter() { _brain?.Motor.Stop(); _brain?.BeginTelegraph(); _brain?.FaceTarget(); _animation?.Play(EnemyActionKind.Telegraph); }
        public override void OnUpdate() { _brain?.Motor.Stop(); }
        public override void OnStateExit() { _brain?.EndTelegraph(); _brain?.Motor.Stop(); }
    }

    public sealed class NewBasicEnemyAttackAction : StateAction
    {
        private EnemyBrain _brain;
        private EnemyAnimation _animation;
        private EnemyAttackController _attack;
        public override void Awake(CoreSM sm)
        {
            _brain = sm.GetCachedComponent<EnemyBrain>();
            sm.TryGetCachedComponent(out _animation);
            sm.TryGetCachedComponent(out _attack);
        }
        public override void OnStateEnter()
        {
            _brain?.Motor.Stop();
            _brain?.FaceTarget();
            EnemyAttackDefinition definition = _attack?.GetAttack(0);
            _animation?.PlayAttack(definition?.AnimationState, definition?.TotalDuration ?? _brain?.Stats.AttackDuration ?? 0.6f);
            if (_attack == null || !_attack.TryStartAttack(0)) _brain?.MarkAttackFinished();
        }
        public override void OnUpdate() { _brain?.Motor.Stop(); }
        public override void OnStateExit() { _attack?.CancelAttack(); _brain?.Motor.Stop(); }
    }

    public sealed class NewBasicEnemyHurtAction : StateAction
    {
        private EnemyBrain _brain;
        private EnemyAnimation _animation;
        public override void Awake(CoreSM sm) { _brain = sm.GetCachedComponent<EnemyBrain>(); sm.TryGetCachedComponent(out _animation); }
        public override void OnStateEnter() { _brain?.Motor.Stop(); _brain?.BeginHurt(); _animation?.Play(EnemyActionKind.Hurt); }
        public override void OnUpdate() { _brain?.Motor.Stop(); }
        public override void OnStateExit() { _brain?.Motor.Stop(); }
    }

    public sealed class NewBasicEnemyDeathAction : StateAction
    {
        private EnemyBrain _brain;
        private EnemyAnimation _animation;
        public override void Awake(CoreSM sm) { _brain = sm.GetCachedComponent<EnemyBrain>(); sm.TryGetCachedComponent(out _animation); }
        public override void OnStateEnter()
        {
            if (_brain == null) return;
            _brain.Motor.Stop();
            _animation?.Play(EnemyActionKind.Death);
            foreach (Collider2D collider in _brain.GetComponentsInChildren<Collider2D>()) collider.enabled = false;
        }
        public override void OnUpdate() { _brain?.Motor.Stop(); }
        public override void OnStateExit() { }
    }
}
