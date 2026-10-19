using UnityEngine;
using Zephyr.Contracts.Interfaces;

namespace Zephyr.Gameplay.Enemies
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class EnemyMotor : MonoBehaviour, IFacingProvider
    {
        [SerializeField] private float _maxFallSpeed = 20f;
        [SerializeField] private LayerMask _groundLayers;
        [SerializeField, Min(0.01f)] private float _patrolProbeDistance = 0.08f;
        [SerializeField, Min(0.01f)] private float _patrolProbeHeight = 0.2f;
        [SerializeField, Min(0.1f)] private float _patrolProbeDepth = 2f;
        [SerializeField, Min(0f)] private float _patrolHeightTolerance = 0.05f;
        private Rigidbody2D _body;
        private Collider2D _bodyCollider;
        private EnemyStats _stats;
        private Vector2 _moveInput;
        private float _requestedSpeed = -1f;
        public Vector2 Facing { get; private set; } = Vector2.left;
        public Vector2 Velocity => _body != null ? _body.linearVelocity : Vector2.zero;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _bodyCollider = GetComponent<Collider2D>();
            _stats = GetComponentInChildren<EnemyStats>(true);
            if (_groundLayers.value == 0)
                _groundLayers = LayerMask.GetMask("Ground");
        }
        public void SetMoveDirection(Vector2 direction, float speed = -1f)
        {
            _moveInput = Vector2.ClampMagnitude(direction, 1f);
            _requestedSpeed = speed;
            if (Mathf.Abs(_moveInput.x) > 0.01f) Facing = _moveInput.x > 0f ? Vector2.right : Vector2.left;
        }
        public void Stop() { _moveInput = Vector2.zero; _requestedSpeed = -1f; }

        public bool CanPatrolInDirection(float direction)
        {
            if (_bodyCollider == null || Mathf.Abs(direction) < 0.01f)
                return false;

            Bounds bounds = _bodyCollider.bounds;
            float currentGroundY;
            float frontGroundY;
            float currentProbeX = bounds.center.x;
            float frontProbeX = direction > 0f
                ? bounds.max.x + _patrolProbeDistance
                : bounds.min.x - _patrolProbeDistance;

            bool hasCurrentGround = TryGetGroundHeight(currentProbeX, bounds.max.y + _patrolProbeHeight, out currentGroundY);
            bool hasFrontGround = TryGetGroundHeight(frontProbeX, bounds.max.y + _patrolProbeHeight, out frontGroundY);

            if (!hasCurrentGround || !hasFrontGround)
                return hasCurrentGround && hasFrontGround;

            return Mathf.Abs(frontGroundY - currentGroundY) <= _patrolHeightTolerance;
        }

        private void FixedUpdate()
        {
            if (_body == null) return;
            float speed = _requestedSpeed >= 0f ? _requestedSpeed : (_stats != null ? _stats.MoveSpeed : 0f);
            float desiredX = _moveInput.x * speed;
            float x = Mathf.MoveTowards(_body.linearVelocity.x, desiredX,
                (_stats != null ? _stats.Acceleration : 30f) * Time.fixedDeltaTime);
            _body.linearVelocity = new Vector2(x, Mathf.Max(_body.linearVelocity.y, -_maxFallSpeed));
        }

        private bool TryGetGroundHeight(float x, float originY, out float groundY)
        {
            RaycastHit2D hit = Physics2D.Raycast(
                new Vector2(x, originY),
                Vector2.down,
                Mathf.Max(0.1f, _patrolProbeDepth),
                _groundLayers);

            groundY = hit.collider != null ? hit.point.y : 0f;
            return hit.collider != null;
        }
    }
}
