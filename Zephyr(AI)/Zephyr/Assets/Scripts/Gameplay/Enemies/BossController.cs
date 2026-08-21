using System.Collections.Generic;
using UnityEngine;
using Zephyr.Core;
using Zephyr.Core.Combat;
using Zephyr.Core.DamageSystem;
using Zephyr.Core.Health;
using Zephyr.Core.Interfaces;

namespace Zephyr.Gameplay.Enemies
{
    /// <summary>
    /// Timeline-driven boss brain. It deliberately composes with EnemyBrain
    /// rather than changing regular enemy state tables: bosses can author
    /// arbitrarily many hitbox windows and movement phases in BossSO.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyBrain), typeof(EnemyStats), typeof(HealthComponent))]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class BossController : MonoBehaviour, IPotentialAttackSource
    {
        [SerializeField] private BossSO _config;
        [SerializeField] private EnemyBrain _brain;
        [SerializeField] private EnemyStats _stats;
        [SerializeField] private HealthComponent _health;
        [SerializeField] private EnemyMotor _motor;
        [SerializeField] private EnemyAnimation _animation;
        [SerializeField] private Rigidbody2D _body;
        [SerializeField] private Transform _spearVisual;
        [SerializeField, Min(0f)] private float _closeRange = 2.2f;
        [SerializeField, Min(0f)] private float _approachSpeed = 3f;
        [SerializeField, Min(0f)] private float _initialAttackDelay = 0.5f;
        [SerializeField] private LayerMask _groundLayers = ~0;
        [SerializeField, Min(1f)] private float _spearGroundProbeDistance = 30f;

        private readonly HashSet<int> _hitTargets = new HashSet<int>();
        private BossAttackDefinition _activeAttack;
        private Vector2 _spearAnchor;
        private float _attackStartedAt;
        private float _nextAttackAt;
        private bool _spearThrown;
        private bool _jumpStarted;
        private bool _dashStarted;
        private bool _stageTwo;
        private bool _surroundSlash;
        private float _surroundStartedAt;
        private int _attackCursor;
        private bool _dead;

        public StatType TargetPotential => _config != null ? _config.TargetPotential : StatType.MaxHp;
        public float PotentialAttack => _config != null ? _config.PotentialAttack : 0f;
        public bool IsStageTwo => _stageTwo;
        public BossAttackDefinition CurrentAttack => _activeAttack;
        public float AttackCooldownRemaining => Mathf.Max(0f, _nextAttackAt - Time.time);

        private void Awake()
        {
            _brain ??= GetComponent<EnemyBrain>();
            _stats ??= GetComponent<EnemyStats>();
            _health ??= GetComponent<HealthComponent>();
            _motor ??= GetComponent<EnemyMotor>();
            _animation ??= GetComponent<EnemyAnimation>();
            _body ??= GetComponent<Rigidbody2D>();
            _nextAttackAt = Time.time + _initialAttackDelay;
            if (_body != null) _body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        private void OnEnable()
        {
            if (_health != null) _health.Died += HandleDeath;
        }

        private void OnDisable()
        {
            if (_health != null) _health.Died -= HandleDeath;
            FinishAttack();
        }

        private void Update()
        {
            if (_dead || _health == null || _health.CurrentHp <= 0f) return;
            UpdateStage();

            if (_surroundSlash)
            {
                UpdateSurroundSlash();
                return;
            }

            if (_activeAttack != null)
            {
                UpdateAttack();
                return;
            }

            if (_brain == null || !_brain.HasTarget)
            {
                _motor?.Stop();
                return;
            }

            if (Time.time < _nextAttackAt)
            {
                ApproachPlayer();
                return;
            }

            BossAttackDefinition next = ChooseAttack();
            if (next == null)
            {
                ApproachPlayer();
                return;
            }

            StartAttack(next);
        }

        private void UpdateStage()
        {
            float maxHp = Mathf.Max(1f, _health.MaxHp);
            if (!_stageTwo && _health.CurrentHp / maxHp <= (_config != null ? _config.StageTwoHealthThreshold : 0.5f))
                _stageTwo = true;
        }

        private void ApproachPlayer()
        {
            if (_brain.Target == null || _motor == null) return;
            Vector2 toPlayer = _brain.Target.position - transform.position;
            if (Mathf.Abs(toPlayer.x) > 0.05f)
                _motor.SetMoveDirection(new Vector2(Mathf.Sign(toPlayer.x), 0f), _approachSpeed);
            else
                _motor.Stop();
        }

        private BossAttackDefinition ChooseAttack()
        {
            IReadOnlyList<BossAttackDefinition> attacks = _config != null ? _config.Attacks : null;
            if (attacks == null || attacks.Count == 0 || _brain.Target == null) return null;

            float distance = Vector2.Distance(transform.position, _brain.Target.position);
            float height = Mathf.Abs(transform.position.y - _brain.Target.position.y);
            bool close = distance <= _closeRange;
            for (int offset = 0; offset < attacks.Count; offset++)
            {
                int index = (_attackCursor + offset) % attacks.Count;
                BossAttackDefinition candidate = attacks[index];
                if (candidate == null || distance > candidate.SelectionRange) continue;
                bool isCloseAttack = candidate.Kind == BossAttackKind.CloseSlashCombo || candidate.Kind == BossAttackKind.CloseGroundStab;
                if (isCloseAttack != close) continue;
                if (!isCloseAttack && height > candidate.SameLevelTolerance) continue;
                _attackCursor = (index + 1) % attacks.Count;
                return candidate;
            }

            return null;
        }

        private void StartAttack(BossAttackDefinition attack)
        {
            _activeAttack = attack;
            _attackStartedAt = Time.time;
            _spearThrown = false;
            _jumpStarted = false;
            _dashStarted = false;
            if (_spearVisual != null) _spearVisual.gameObject.SetActive(false);
            _hitTargets.Clear();
            _motor?.Stop();
            if (_motor != null) _motor.enabled = false;
            FacePlayer();
            _animation?.PlayAttack(attack.AnimationState, attack.TotalDuration);
        }

        private void UpdateAttack()
        {
            float elapsed = Time.time - _attackStartedAt;
            if (_activeAttack.Kind == BossAttackKind.Dash && elapsed >= _activeAttack.TelegraphDuration && !_dashStarted)
            {
                _dashStarted = true;
                LaunchDash(_activeAttack.MovementSpeed);
            }
            else if (_activeAttack.Kind == BossAttackKind.HighJump && elapsed >= _activeAttack.TelegraphDuration && !_dashStarted)
            {
                _dashStarted = true;
                LaunchHighJump();
            }
            else if (_activeAttack.Kind == BossAttackKind.SpearThrowAndDash)
            {
                if (!_jumpStarted && elapsed >= _activeAttack.TelegraphDuration)
                {
                    _jumpStarted = true;
                    LaunchHighJump();
                }
                if (!_spearThrown && elapsed >= _activeAttack.ThrowTime) ThrowSpear();
                if (!_dashStarted && elapsed >= _activeAttack.DashToSpearTime)
                {
                    _dashStarted = true;
                    LaunchDashToSpear();
                }
            }

            if (elapsed >= _activeAttack.TelegraphDuration)
                ProcessHitboxes(elapsed, _activeAttack.Hitboxes);

            if (elapsed >= _activeAttack.TotalDuration)
                FinishAttack();
        }

        private void ThrowSpear()
        {
            _spearThrown = true;
            Vector2 target = _brain != null && _brain.Target != null ? _brain.Target.position : transform.position;
            RaycastHit2D ground = Physics2D.Raycast(new Vector2(target.x, target.y + 2f), Vector2.down,
                _spearGroundProbeDistance, _groundLayers);
            _spearAnchor = ground.collider != null ? ground.point : new Vector2(target.x, target.y);
            if (_spearVisual != null)
            {
                _spearVisual.position = _spearAnchor;
                _spearVisual.gameObject.SetActive(true);
            }
        }

        private void LaunchDashToSpear()
        {
            if (_body == null) return;
            Vector2 direction = (_spearAnchor - (Vector2)transform.position).normalized;
            _body.linearVelocity = direction * Mathf.Max(0.1f, _activeAttack.MovementSpeed);
        }

        private void LaunchDash(float speed)
        {
            if (_body == null) return;
            Vector2 target = _brain != null && _brain.Target != null ? _brain.Target.position : transform.position + (Vector3)_motor.Facing;
            Vector2 direction = (target - (Vector2)transform.position).normalized;
            _body.linearVelocity = direction * Mathf.Max(0.1f, speed);
        }

        private void LaunchHighJump()
        {
            if (_body == null) return;
            float horizontal = _brain != null && _brain.Target != null
                ? Mathf.Sign(_brain.Target.position.x - transform.position.x) * _activeAttack.JumpHorizontalSpeed
                : 0f;
            _body.linearVelocity = new Vector2(horizontal, _activeAttack.JumpVelocity);
        }

        private void ProcessHitboxes(float elapsed, IReadOnlyList<BossHitboxSegment> hitboxes)
        {
            if (hitboxes == null) return;
            for (int i = 0; i < hitboxes.Count; i++)
            {
                BossHitboxSegment segment = hitboxes[i];
                if (segment == null || elapsed < segment.StartTime || elapsed > segment.EndTime) continue;
                Vector2 origin = segment.UseSpearAnchor ? _spearAnchor : transform.position;
                Vector2 center = origin + FacingOffset(segment.LocalOffset);
                Collider2D[] hits = Physics2D.OverlapBoxAll(center, segment.Size, 0f, segment.TargetLayers);
                foreach (Collider2D hit in hits)
                {
                    HealthComponent target = hit != null ? hit.GetComponentInParent<HealthComponent>() : null;
                    if (target == null || target == _health || !IsPlayerTarget(target)) continue;
                    int id = target.GetHashCode() ^ (i * 397);
                    if (segment.DamageOncePerTarget && !_hitTargets.Add(id)) continue;
                    target.TakeDamage(new DamageInfo(
                        _activeAttack.Damage * segment.DamageMultiplier,
                        DamageType.AD,
                        sourcePosition: transform.position,
                        source: gameObject,
                        category: WeaponCategory.Melee));
                }
            }
        }

        private void UpdateSurroundSlash()
        {
            float elapsed = Time.time - _surroundStartedAt;
            if (_config != null && elapsed <= _config.StageTwoSurroundDuration)
            {
                Vector2 size = _config.StageTwoSurroundSize;
                float offset = _config.StageTwoSurroundOffset;
                ProcessSpecialHitbox(new Vector2(offset, 0f), size, _config.StageTwoSurroundDamage, 1001);
                ProcessSpecialHitbox(new Vector2(-offset, 0f), size, _config.StageTwoSurroundDamage, 1002);
            }
            else
            {
                _surroundSlash = false;
                _nextAttackAt = Time.time + (_config != null ? _config.StageTwoAttackInterval : 1.5f);
            }
        }

        private void ProcessSpecialHitbox(Vector2 localOffset, Vector2 size, float damageMultiplier, int id)
        {
            Collider2D[] hits = Physics2D.OverlapBoxAll((Vector2)transform.position + FacingOffset(localOffset), size, 0f);
            foreach (Collider2D hit in hits)
            {
                HealthComponent target = hit != null ? hit.GetComponentInParent<HealthComponent>() : null;
                if (target == null || target == _health || !IsPlayerTarget(target) || !_hitTargets.Add(target.GetHashCode() ^ id)) continue;
                target.TakeDamage(new DamageInfo(
                    (_stats != null ? _stats.AttackDamage : 0f) * damageMultiplier,
                    DamageType.AD,
                    sourcePosition: transform.position,
                    source: gameObject,
                    category: WeaponCategory.Melee));
            }
        }

        private void FinishAttack()
        {
            if (_activeAttack == null && !_surroundSlash) return;
            _activeAttack = null;
            if (_spearVisual != null) _spearVisual.gameObject.SetActive(false);
            _motor?.Stop();
            if (_motor != null) _motor.enabled = true;
            if (_body != null && Mathf.Abs(_body.linearVelocity.x) > 0.01f) _body.linearVelocity = new Vector2(0f, _body.linearVelocity.y);
            _hitTargets.Clear();
            if (_stageTwo && _config != null && _config.StageTwoSurroundDuration > 0f)
            {
                _surroundSlash = true;
                _surroundStartedAt = Time.time;
                _hitTargets.Clear();
            }
            else
            {
                _nextAttackAt = Time.time + (_config != null ? _config.StageOneAttackInterval : 2.5f);
            }
        }

        private void FacePlayer()
        {
            if (_brain == null || _brain.Target == null || _motor == null) return;
            Vector2 direction = _brain.Target.position - transform.position;
            if (Mathf.Abs(direction.x) > 0.01f) _motor.SetMoveDirection(new Vector2(Mathf.Sign(direction.x), 0f), 0f);
        }

        private Vector2 FacingOffset(Vector2 localOffset)
        {
            float sign = _motor != null && _motor.Facing.x >= 0f ? 1f : -1f;
            return new Vector2(localOffset.x * sign, localOffset.y);
        }

        private static bool IsPlayerTarget(HealthComponent target)
        {
            if (target == null || !target.gameObject.CompareTag("Player")) return false;
            CombatTargetAvailability availability = target.GetComponentInParent<CombatTargetAvailability>();
            return availability == null || availability.IsAvailable;
        }

        private void HandleDeath()
        {
            _dead = true;
            _activeAttack = null;
            _surroundSlash = false;
            if (_spearVisual != null) _spearVisual.gameObject.SetActive(false);
            _motor?.Stop();
            if (_body != null) _body.linearVelocity = Vector2.zero;
        }

        private void OnDrawGizmosSelected()
        {
            if (_config == null) return;
            IReadOnlyList<BossAttackDefinition> attacks = _config.Attacks;
            if (attacks == null) return;
            foreach (BossAttackDefinition attack in attacks)
            {
                if (attack?.Hitboxes == null) continue;
                foreach (BossHitboxSegment segment in attack.Hitboxes)
                {
                    if (segment == null || !segment.ShowGizmo) continue;
                    Gizmos.color = new Color(1f, 0.3f, 0.1f, 0.35f);
                    Gizmos.DrawWireCube((Vector2)transform.position + segment.LocalOffset, segment.Size);
                }
            }
        }
    }
}
