using UnityEngine;
using Zephyr.Contracts.Damage;
using Zephyr.Contracts.Events;
using Zephyr.Contracts.Interfaces;
using Zephyr.Core.Health;
using Zephyr.Core.Flow;

namespace Zephyr.Gameplay.Enemies.Testing
{
    /// <summary>
    /// Test-only attacker that applies a configurable burst to the current player.
    /// Hit timing is evenly distributed across the authored combo duration.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HitbackComboTestEnemy : MonoBehaviour, IPlayerTargetContextConsumer, IGameManagerDependencyConsumer
    {
        [Header("Combo")]
        [SerializeField, Min(0.05f)] private float _comboDuration = 1.2f;
        [SerializeField, Min(1)] private int _comboHits = 6;
        [SerializeField, Min(0f)] private float _damagePerHit = 12f;
        [SerializeField, Min(0f)] private float _startDelay = 0.5f;
        [SerializeField] private bool _repeat = true;

        [Header("Range")]
        [SerializeField, Min(0f)] private float _range = 2f;
        [SerializeField] private bool _faceTarget = true;

        [Header("Feedback payload")]
        [SerializeField, Min(0f)] private float _hitstopIntensity = 1f;
        [SerializeField, Min(0f)] private float _hitstopDuration;
        [SerializeField, Min(0f)] private float _shakeIntensity = 1f;
        [SerializeField] private int _impactVfxId = 1;
        [SerializeField] private int _feedbackPriority;
        [SerializeField] private int _feedbackCooldownGroup;

private IPlayerTargetContext _targetContext;
        private ICombatFeedbackGateway _feedback;
        private HealthComponent _targetHealth;
        private IDamageReceiver _targetDamageReceiver;
        private float _nextComboAt;
        private float _nextHitAt;
        private int _hitsDone;
        private bool _comboActive;

        public void Configure(IPlayerTargetContext context)
        {
            if (_targetContext != null) _targetContext.UnsubscribePlayerTarget(HandleTargetChanged);
            _targetContext = context;
            _targetContext?.SubscribePlayerTarget(HandleTargetChanged);
            if (_targetContext != null && _targetContext.TryGetLatestPlayerTarget(out PlayerTargetChangedEvent change))
                HandleTargetChanged(change);
        }

        public void Configure(IGameManagerServices services)
        {
            _feedback = services?.CombatFeedback;
        }

private void OnEnable()
        {
            _nextComboAt = Time.time + _startDelay;
        }

        private void OnDisable()
        {
            _targetContext?.UnsubscribePlayerTarget(HandleTargetChanged);
            _targetContext = null;
            _targetHealth = null;
            _comboActive = false;
        }

        private void Update()
        {
            if (_targetHealth == null || _targetHealth.CurrentHp <= 0f)
            {
                _comboActive = false;
                return;
            }

            if (_faceTarget) FaceTarget();

            float distance = Vector2.Distance(transform.position, _targetHealth.transform.position);
            if (distance > _range)
            {
                _comboActive = false;
                return;
            }

            if (!_comboActive && Time.time >= _nextComboAt)
            {
                _comboActive = true;
                _hitsDone = 0;
                _nextHitAt = Time.time;
            }

            if (!_comboActive || Time.time < _nextHitAt) return;

            ApplyHit();
            _hitsDone++;
            if (_hitsDone >= Mathf.Max(1, _comboHits))
            {
                _comboActive = false;
                _nextComboAt = _repeat ? Time.time + _startDelay : float.PositiveInfinity;
                return;
            }

            float interval = _comboDuration / Mathf.Max(1, _comboHits - 1);
            _nextHitAt = Time.time + interval;
        }

private void FaceTarget()
        {
            float direction = Mathf.Sign(_targetHealth.transform.position.x - transform.position.x);
            if (Mathf.Approximately(direction, 0f)) return;
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * direction;
            transform.localScale = scale;
        }

        private void ApplyHit()
        {
            if (_targetHealth == null) return;
            Vector2 sourcePosition = transform.position;
            DamageInfo info = DamageInfo.Create(
                _damagePerHit,
                DamageType.AD,
                category: WeaponCategory.Melee,
                sourcePosition: sourcePosition,
                source: gameObject,
                feedback: new DamageFeedbackSettings(
                    _hitstopIntensity,
                    _hitstopDuration,
                    _shakeIntensity,
                    _impactVfxId,
                    _feedbackPriority,
                    _feedbackCooldownGroup));

            _targetDamageReceiver?.ReceiveDamage(info, transform.position);
        }

        private void HandleTargetChanged(PlayerTargetChangedEvent change)
        {
            _targetHealth = change.Target != null ? change.Target.GetComponentInParent<HealthComponent>() : null;
            _targetDamageReceiver = null;
            if (_targetHealth == null) return;

            MonoBehaviour[] behaviours = _targetHealth.GetComponentsInParent<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IDamageReceiver damageReceiver)
                {
                    _targetDamageReceiver = damageReceiver;
                    break;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.2f, 0.1f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, _range);
        }
    }
}
