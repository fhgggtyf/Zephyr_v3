using UnityEngine;
using Zephyr.Core.DamageSystem;
using Zephyr.Core.Health;
using Zephyr.Core.Combat;

namespace Zephyr.Gameplay.Enemies
{
    public sealed class EnemyBrain : MonoBehaviour
    {
        [SerializeField] private EnemyStats _stats;
        [SerializeField] private EnemyMotor _motor;
        [SerializeField] private HealthComponent _health;
        private EnemyAttackController _attackController;
        private Transform _target;
        private float _lastDamageTime = -100f;
        private float _lastAttackTime = -100f;
        private bool _dead;
        private bool _attackFinished = true;
        private bool _isTelegraphing;
        private float _phaseEndsAt;

        public EnemyStats Stats => _stats;
        public EnemyMotor Motor => _motor;
        public HealthComponent Health => _health;
        public Transform Target => _target;
        public bool HasTarget => _target != null;
        public bool IsDead => _dead || (_health != null && _health.CurrentHp <= 0f);
        public bool WasRecentlyDamaged => Time.time - _lastDamageTime < 0.15f;
        public EnemyAttackController AttackController => _attackController != null
            ? _attackController
            : (_attackController = GetComponent<EnemyAttackController>());
        public bool IsAttackReady => AttackController != null
            ? AttackController.CanStartAttack
            : Time.time - _lastAttackTime >= (_stats != null ? _stats.AttackCooldown : 1f);
        public bool AttackFinished => AttackController != null
            ? AttackController.AttackFinished
            : _attackFinished;
        public bool IsTelegraphing => _isTelegraphing;
        public bool PhaseFinished => Time.time >= _phaseEndsAt;

        private void Awake()
        {
            _stats ??= GetComponent<EnemyStats>();
            _motor ??= GetComponent<EnemyMotor>();
            _health ??= GetComponent<HealthComponent>();
            _stats?.ApplyToStatsCore();
            _health?.ResetHealth();
            if (_health != null)
            {
                _health.DamageTaken += HandleDamage;
                _health.Died += HandleDeath;
            }
        }
        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.DamageTaken -= HandleDamage;
                _health.Died -= HandleDeath;
            }
        }
        private void Update()
        {
            if (_target == null || !IsTargetValid(_target))
            {
                _target = null;
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null && IsTargetValid(player.transform)) _target = player.transform;
            }
        }
        public bool PlayerWithin(float range) => _target != null && Vector2.Distance(transform.position, _target.position) <= range;
        public bool PlayerWithinLoseRange()
            => _target != null && _stats != null && PlayerWithin(_stats.AggroRange * _stats.LoseAggroMultiplier);
        public bool IsFacingPlayer()
        {
            if (_target == null || _motor == null) return false;
            Vector2 toPlayer = _target.position - transform.position;
            return Mathf.Sign(toPlayer.x) == Mathf.Sign(_motor.Facing.x);
        }
        public void MarkAttackStarted()
        {
            _isTelegraphing = false;
            _lastAttackTime = Time.time;
            _attackFinished = false;
        }
        public void MarkAttackFinished() => _attackFinished = true;
        public void BeginAlert() => _phaseEndsAt = Time.time + (_stats != null ? _stats.AlertDuration : 0.5f);
        public void BeginTelegraph()
        {
            _isTelegraphing = true;
            _phaseEndsAt = Time.time + (_stats != null ? _stats.AttackWindup : 0.35f);
        }
        public void EndTelegraph() => _isTelegraphing = false;
        public void BeginHurt()
        {
            _isTelegraphing = false;
            _phaseEndsAt = Time.time + (_stats != null ? _stats.HurtDuration : 0.2f);
        }
        public void FaceTarget()
        {
            if (_target != null && _motor != null)
                _motor.SetMoveDirection(new Vector2(Mathf.Sign(_target.position.x - transform.position.x), 0f), 0f);
        }
        public void MarkDamage(DamageInfo info) => _lastDamageTime = Time.time;
        private void HandleDamage(DamageInfo info, DamageResult result) => MarkDamage(info);
        private void HandleDeath()
        {
            _dead = true;
            _isTelegraphing = false;
            _motor?.Stop();
        }
        private static bool IsTargetValid(Transform target)
        {
            if (target == null || !target.gameObject.activeInHierarchy) return false;
            CombatTargetAvailability availability = target.GetComponentInParent<CombatTargetAvailability>();
            return availability == null || availability.IsAvailable;
        }
    }
}
