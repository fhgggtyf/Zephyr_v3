using UnityEngine;
using Zephyr.Core.Interfaces;

namespace Zephyr.Gameplay.Enemies
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class EnemyMotor : MonoBehaviour, IFacingProvider
    {
        [SerializeField] private float _maxFallSpeed = 20f;
        private Rigidbody2D _body;
        private EnemyStats _stats;
        private Vector2 _moveInput;
        private float _requestedSpeed = -1f;
        public Vector2 Facing { get; private set; } = Vector2.left;
        public Vector2 Velocity => _body != null ? _body.linearVelocity : Vector2.zero;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _stats = GetComponentInChildren<EnemyStats>(true);
        }
        public void SetMoveDirection(Vector2 direction, float speed = -1f)
        {
            _moveInput = Vector2.ClampMagnitude(direction, 1f);
            _requestedSpeed = speed;
            if (Mathf.Abs(_moveInput.x) > 0.01f) Facing = _moveInput.x > 0f ? Vector2.right : Vector2.left;
        }
        public void Stop() { _moveInput = Vector2.zero; _requestedSpeed = -1f; }
        private void FixedUpdate()
        {
            if (_body == null) return;
            float speed = _requestedSpeed >= 0f ? _requestedSpeed : (_stats != null ? _stats.MoveSpeed : 0f);
            float desiredX = _moveInput.x * speed;
            float x = Mathf.MoveTowards(_body.linearVelocity.x, desiredX,
                (_stats != null ? _stats.Acceleration : 30f) * Time.fixedDeltaTime);
            _body.linearVelocity = new Vector2(x, Mathf.Max(_body.linearVelocity.y, -_maxFallSpeed));
        }
    }
}
