using System.Collections;
using UnityEngine;
using Zephyr.Core;
using Zephyr.Core.Camera;
using Zephyr.Core.DamageSystem;
using Zephyr.Core.Health;
using Zephyr.Core.Stats;
using Zephyr.Gameplay.Player.Visual;

namespace Zephyr.Gameplay.Player.Core
{
    /// <summary>
    /// Owns player hit reactions that are shared by every damage source.
    /// Impact motion is coordinated by authored Knockback/Hitback/Getup states so
    /// animations and other state actions can be attached in the editor.
    /// </summary>
    [DefaultExecutionOrder(-250)]
    [RequireComponent(typeof(HealthComponent))]
    public sealed class PlayerImpactController : MonoBehaviour
    {
        private const float MaxKnockbackTime = 0.8f;
        private const float GetupTime = 1f;

        [Header("Knockback")]
        [SerializeField, Min(0f)] private float _damagePerSecond = 100f;
        [SerializeField, Range(0f, 0.8f)] private float _maximumTenacityReduction = 0.8f;
        [SerializeField, Min(0f)] private float _knockbackSpeed = 6f;
        [SerializeField, Min(0f)] private float _hitbackHorizontalSpeed = 8f;
        [SerializeField, Min(0f)] private float _hitbackVerticalSpeed = 11f;
        [SerializeField, Min(0.01f)] private float _minimumHitbackFlight = 0.1f;
        [SerializeField, Min(0f)] private float _bounceDamping = 0.85f;

        [Header("Presentation")]
        [SerializeField] private Color _flashColor = new Color(1f, 1f, 1f, 0.25f);
        [SerializeField, Min(0.01f)] private float _flashInterval = 0.06f;

        private HealthComponent _health;
        private StatsCore _stats;
        private HurtReceiver _hurtReceiver;
        private MovementCore _movement;
        private Rigidbody2D _body;
        private SpriteRenderer[] _sprites;
        private CameraController _camera;
        private Coroutine _flashRoutine;
        private float _knockbackRemaining;
        private ImpactMode _mode;
        private float _modeEndsAt;
        private float _hitbackStartedAt;
        private Vector2 _pendingSourcePosition;
        private Color[] _originalColors;

        public bool IsInImpact => _mode != ImpactMode.None;
        public bool IsInvincible => _hurtReceiver != null && _hurtReceiver.IsInvincible;
        public float KnockbackRemaining => _knockbackRemaining;
        public bool WantsKnockbackState => _mode == ImpactMode.Knockback;
        public bool WantsHitbackState => _mode == ImpactMode.Hitback;
        public bool WantsGetupState => _mode == ImpactMode.Getup;
        public bool IsImpactComplete => _mode == ImpactMode.None;

        public enum ImpactState { Knockback, Hitback, Getup }
        private enum ImpactMode { None, Knockback, Hitback, Getup }

        private void Awake()
        {
            _health = GetComponent<HealthComponent>();
            _stats = GetComponent<StatsCore>();
            _hurtReceiver = GetComponentInChildren<HurtReceiver>(true);
            _movement = GetComponentInChildren<MovementCore>(true);
            _body = GetComponent<Rigidbody2D>();
            SpriteAnimator playerVisual = GetComponentInChildren<SpriteAnimator>(true);
            SpriteRenderer playerRenderer = playerVisual != null ? playerVisual.GetComponent<SpriteRenderer>() : null;
            _sprites = playerRenderer != null
                ? new[] { playerRenderer }
                : GetComponentsInChildren<SpriteRenderer>(true);
            _originalColors = new Color[_sprites.Length];
            for (int i = 0; i < _sprites.Length; i++) _originalColors[i] = _sprites[i].color;
            _camera = FindAnyObjectByType<CameraController>();
        }

        private void OnEnable()
        {
            if (_health != null)
            {
                _health.DamageTaken += HandleDamage;
                _health.Died += HandleDeath;
            }
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.DamageTaken -= HandleDamage;
                _health.Died -= HandleDeath;
            }
            StopImpact();
        }

        private void HandleDamage(DamageInfo info, DamageResult result)
        {
            float damage = Mathf.Max(0f, result.DealtToHp + result.DealtToShield);
            if (damage <= 0f) return;

            _camera ??= FindAnyObjectByType<CameraController>();
            _camera?.ShakeForDamage(damage);

            // DamageTaken is not raised for filtered hits. A lethal accepted hit
            // still shakes the camera, but death presentation owns all motion.
            if (_health == null || _health.CurrentHp <= 0f || _hurtReceiver == null)
                return;

            float tenacity = _stats != null ? _stats.GetStatValue(StatType.Tenacity) : 0f;
            float reduction = Mathf.Clamp(tenacity / 100f, 0f, _maximumTenacityReduction);
            float duration = damage / Mathf.Max(1f, _damagePerSecond) * (1f - reduction);
            duration = Mathf.Max(0.01f, duration);

            if (_mode == ImpactMode.Hitback || _mode == ImpactMode.Getup) return;
            float projected = (_mode == ImpactMode.Knockback ? _knockbackRemaining : 0f) + duration;
            if (projected > MaxKnockbackTime)
            {
                BeginHitback(info);
                return;
            }

            if (_mode != ImpactMode.Knockback)
            {
                _mode = ImpactMode.Knockback;
                _pendingSourcePosition = info.SourcePosition;
            }
            if (duration > _knockbackRemaining)
            {
                _knockbackRemaining = Mathf.Min(MaxKnockbackTime, duration);
                _pendingSourcePosition = info.SourcePosition;
                if (_movement != null && !_movement.enabled)
                    ApplyKnockbackVelocity(info.SourcePosition);
            }
        }

        private void ApplyKnockbackVelocity(Vector2 sourcePosition)
        {
            if (_body == null) return;
            float direction = Mathf.Sign(transform.position.x - sourcePosition.x);
            if (Mathf.Abs(direction) < 0.01f) direction = -(_movement != null && _movement.Facing.x > 0f ? 1f : -1f);
            _body.linearVelocity = new Vector2(direction * _knockbackSpeed, _body.linearVelocity.y);
        }

        private void BeginHitback(DamageInfo info)
        {
            _mode = ImpactMode.Hitback;
            _pendingSourcePosition = info.SourcePosition;
            _hitbackStartedAt = Time.time;
        }

        private void BeginGetup()
        {
            _mode = ImpactMode.Getup;
            _modeEndsAt = Time.time + GetupTime;
        }

        private void StopImpact()
        {
            _mode = ImpactMode.None;
            _knockbackRemaining = 0f;
            if (_hurtReceiver != null) _hurtReceiver.SetInvincible(false);
            if (_movement != null) _movement.enabled = true;
            ResetFlash();
        }

        public void EnterImpactState(ImpactState state)
        {
            if (_movement != null) _movement.enabled = false;
            _movement?.SetMoveInput(Vector2.zero);
            _movement?.SetMoveSpeed(0f);

            switch (state)
            {
                case ImpactState.Knockback:
                    ApplyKnockbackVelocity(_pendingSourcePosition);
                    break;
                case ImpactState.Hitback:
                    _hurtReceiver?.SetInvincible(true);
                    if (_flashRoutine != null) StopCoroutine(_flashRoutine);
                    _flashRoutine = StartCoroutine(FlashRoutine());
                    float direction = Mathf.Sign(transform.position.x - _pendingSourcePosition.x);
                    if (Mathf.Abs(direction) < 0.01f) direction = -(_movement != null && _movement.Facing.x > 0f ? 1f : -1f);
                    if (_body != null) _body.linearVelocity = new Vector2(direction * _hitbackHorizontalSpeed, _hitbackVerticalSpeed);
                    break;
                case ImpactState.Getup:
                    _hurtReceiver?.SetInvincible(true);
                    if (_body != null) _body.linearVelocity = Vector2.zero;
                    break;
            }
        }

        public void UpdateImpactState(ImpactState state)
        {
            if (_mode == ImpactMode.None) return;
            if (state == ImpactState.Knockback && _mode == ImpactMode.Knockback)
            {
                _knockbackRemaining = Mathf.Max(0f, _knockbackRemaining - Time.deltaTime);
                if (_knockbackRemaining <= 0f) StopImpact();
            }
            else if (state == ImpactState.Hitback && _mode == ImpactMode.Hitback)
            {
                if (Time.time - _hitbackStartedAt >= _minimumHitbackFlight && _movement != null && _movement.IsGrounded)
                    BeginGetup();
            }
            else if (state == ImpactState.Getup && _mode == ImpactMode.Getup && Time.time >= _modeEndsAt)
            {
                StopImpact();
            }
        }

        public void ExitImpactState(ImpactState state)
        {
            if (_mode == ImpactMode.None)
                if (_movement != null) _movement.enabled = true;
        }

        private IEnumerator FlashRoutine()
        {
            bool visible = false;
            while (_mode == ImpactMode.Hitback || _mode == ImpactMode.Getup)
            {
                visible = !visible;
                for (int i = 0; i < _sprites.Length; i++)
                    _sprites[i].color = visible ? _flashColor : _originalColors[i];
                yield return new WaitForSeconds(_flashInterval);
            }

            for (int i = 0; i < _sprites.Length; i++)
                if (_sprites[i] != null) _sprites[i].color = _originalColors[i];
            _flashRoutine = null;
        }

        private void HandleDeath()
        {
            _mode = ImpactMode.None;
            _knockbackRemaining = 0f;
            if (_movement != null) _movement.enabled = true;
            ResetFlash();
            _hurtReceiver?.SetInvincible(false);
        }

        private void ResetFlash()
        {
            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
                _flashRoutine = null;
            }
            for (int i = 0; i < _sprites.Length; i++)
                if (_sprites[i] != null) _sprites[i].color = _originalColors[i];
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_mode != ImpactMode.Hitback) return;
            foreach (ContactPoint2D contact in collision.contacts)
            {
                Vector2 normal = contact.normal;
                if (normal.y < -0.5f && _body != null)
                    _body.linearVelocity = new Vector2(_body.linearVelocity.x, -Mathf.Abs(_body.linearVelocity.y) * _bounceDamping);
                else if (Mathf.Abs(normal.x) > 0.5f && _body != null)
                    _body.linearVelocity = new Vector2(-_body.linearVelocity.x * _bounceDamping, _body.linearVelocity.y);
            }
        }
    }
}
