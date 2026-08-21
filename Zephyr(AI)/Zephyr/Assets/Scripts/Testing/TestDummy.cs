using UnityEngine;
using Zephyr.Core.DamageSystem;
using Zephyr.Core.Health;

namespace Zephyr.Testing
{
    /// <summary>
    /// Visual feedback for the test target. HealthComponent owns health and the
    /// complete damage pipeline; this component only flashes and disables on death.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(HealthComponent))]
    public class TestDummy : MonoBehaviour
    {
        [Header("Test Dummy")]
        [SerializeField] private Color _hitFlashColor = new Color(1f, 0.5f, 0.5f, 0.5f);
        [SerializeField] private float _hitFlashDuration = 0.1f;

        private HealthComponent _health;
        private SpriteRenderer _spriteRenderer;
        private Color _originalColor;
        private float _hitFlashTimer;

        private void Awake()
        {
            _health = GetComponent<HealthComponent>();
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            if (_spriteRenderer != null) _originalColor = _spriteRenderer.color;
        }

        private void OnEnable()
        {
            if (_health == null) _health = GetComponent<HealthComponent>();
            if (_health == null) return;

            _health.DamageTaken += HandleDamageTaken;
            _health.Died += HandleDied;
        }

        private void OnDisable()
        {
            if (_health == null) return;

            _health.DamageTaken -= HandleDamageTaken;
            _health.Died -= HandleDied;
        }

        private void Update()
        {
            if (_hitFlashTimer > 0f)
            {
                _hitFlashTimer -= Time.deltaTime;
                if (_hitFlashTimer <= 0f && _spriteRenderer != null)
                {
                    _spriteRenderer.color = _originalColor;
                }
            }
        }

        private void HandleDamageTaken(DamageInfo damageInfo, DamageResult result)
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _hitFlashColor;
                _hitFlashTimer = _hitFlashDuration;
            }
        }

        private void HandleDied()
        {
            Debug.Log("[TestDummy] Destroyed!", this);
            gameObject.SetActive(false);
        }
    }
}
