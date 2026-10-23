using UnityEngine;
using Zephyr.Contracts.Events;
using Zephyr.Core.DamageSystem;
using Zephyr.Contracts.Damage;
using Zephyr.Core.Health;
using Zephyr.Core.Combat;
using Zephyr.Contracts.Interfaces;

using Zephyr.Core.Items;

namespace Zephyr.Gameplay.Enemies
{
    public sealed class EnemyBrain : MonoBehaviour, ILevelObjective, IDropSpawnGatewayConsumer, IPlayerTargetContextConsumer
    {
        [SerializeField] private EnemyStats _stats;
        [SerializeField] private EnemyMotor _motor;
        [SerializeField] private HealthComponent _health;
        private EnemyAttackController _attackController;
        private IDropSpawnGateway _dropSpawnGateway;
        private Transform _target;
        private float _lastDamageTime = -100f;
        private float _lastAttackTime = -100f;
        private bool _dead;
        private bool _dropsEmitted;
        private bool _attackFinished = true;
        private bool _isTelegraphing;
        private float _phaseEndsAt;
        private bool _subscribedToPlayerTarget;
        private IPlayerTargetContext _playerTargetContext;

        public EnemyStats Stats => _stats;
        public EnemyMotor Motor => _motor;
        public HealthComponent Health => _health;
        public Transform Target => _target;
        public bool HasTarget => _target != null;
        public bool IsDead => _dead || (_health != null && _health.CurrentHp <= 0f);
        public bool IsComplete => IsDead;
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

        public void Configure(IPlayerTargetContext context)
        {
            if (_subscribedToPlayerTarget)
                _playerTargetContext?.UnsubscribePlayerTarget(HandlePlayerTargetChanged);

            _playerTargetContext = context;
            _subscribedToPlayerTarget = false;
            SubscribeToPlayerTarget();
        }

        public void Configure(IDropSpawnGateway gateway)
        {
            _dropSpawnGateway = gateway;
            if (_dead) TryEmitDrops();
        }

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

        private void OnEnable()
        {
            SubscribeToPlayerTarget();
        }

        private void Start()
        {
            SubscribeToPlayerTarget();
        }

        private void OnDisable()
        {
            if (_subscribedToPlayerTarget)
                _playerTargetContext?.UnsubscribePlayerTarget(HandlePlayerTargetChanged);
            _subscribedToPlayerTarget = false;
        }

        private void SubscribeToPlayerTarget()
        {
            if (_subscribedToPlayerTarget || _playerTargetContext == null) return;
            _playerTargetContext.SubscribePlayerTarget(HandlePlayerTargetChanged);
            _subscribedToPlayerTarget = true;
            if (_playerTargetContext.TryGetLatestPlayerTarget(out PlayerTargetChangedEvent current))
                HandlePlayerTargetChanged(current);
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
                _target = null;
        }

        private void HandlePlayerTargetChanged(PlayerTargetChangedEvent change)
        {
            _target = change.Target != null && IsTargetValid(change.Target) ? change.Target : null;
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
            TryEmitDrops();
        }

        private void TryEmitDrops()
        {
            if (_dropsEmitted || _dropSpawnGateway == null || _stats == null || _stats.Config == null)
                return;

            DropResult[] results = DropSystem.Roll(
                _stats.Config.Drops,
                new DropContext(_dropSpawnGateway.Seed, GetEntityId().GetHashCode(), _dropSpawnGateway.RoomDepth),
                _stats.Luck);
            Vector3 currencySpawnPosition = transform.position + Vector3.up * 0.25f;
            Vector3 weaponSpawnPosition = transform.position + Vector3.up;
            for (int i = 0; i < results.Length; i++)
            {
                DropResult result = results[i];
                Vector3 position = result.Kind == DropKind.Weapon
                    ? weaponSpawnPosition + Vector3.right * (i * 0.2f)
                    : currencySpawnPosition + Vector3.right * (i * 0.2f);
                if (result.Kind == DropKind.Currency)
                    _dropSpawnGateway.SpawnCurrency(position, result.CurrencyAmount);
                else if (result.Kind == DropKind.Weapon)
                    _dropSpawnGateway.SpawnWeapon(position, result.WeaponId);
            }
            _dropsEmitted = true;
        }

        private static bool IsTargetValid(Transform target)
        {
            if (target == null || !target.gameObject.activeInHierarchy) return false;
            CombatTargetAvailability availability = target.GetComponentInParent<CombatTargetAvailability>();
            return availability == null || availability.IsAvailable;
        }
    }
}
