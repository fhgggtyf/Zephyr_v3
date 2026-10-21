using UnityEngine;

namespace Zephyr.Core.Physics
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class GravityBody2D : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _gravityScale = 2f;
        [SerializeField] private LayerMask _groundLayers = 1 << 3;
        [SerializeField, Min(0.01f)] private float _groundProbeDistance = 0.2f;
        [SerializeField, Min(0f)] private float _groundSurfaceOffset = 0.02f;
        [SerializeField, Min(0.1f)] private float _maxFallSpeed = 20f;

        private Rigidbody2D _body;
        private Collider2D _collider;
        private readonly RaycastHit2D[] _groundHits = new RaycastHit2D[8];
        private bool _grounded;

        private void Awake()
        {
            ConfigureBody();
        }

        private void OnEnable()
        {
            ConfigureBody();
            _body.gravityScale = _gravityScale;
            _body.linearVelocity = Vector2.zero;
            _body.angularVelocity = 0f;
            _body.simulated = true;
            _grounded = false;
        }

        private void FixedUpdate()
        {
            if (_body == null || _collider == null) return;

            if (_grounded)
            {
                if (HasGroundBelow())
                {
                    _body.linearVelocity = Vector2.zero;
                    return;
                }

                _grounded = false;
                _body.gravityScale = _gravityScale;
            }

            Vector2 velocity = _body.linearVelocity;
            velocity.y = Mathf.Max(velocity.y, -_maxFallSpeed);
            _body.linearVelocity = velocity;

            if (!TryFindGround(out RaycastHit2D hit)) return;

            float targetY = hit.point.y + _collider.bounds.extents.y + _groundSurfaceOffset;
            if (_body.position.y <= targetY + _groundProbeDistance)
            {
                _body.position = new Vector2(_body.position.x, targetY);
                _body.linearVelocity = Vector2.zero;
                _grounded = true;
            }
        }

        private void ConfigureBody()
        {
            if (_body == null) _body = GetComponent<Rigidbody2D>();
            if (_collider == null) _collider = GetComponent<Collider2D>();

            _body.bodyType = RigidbodyType2D.Dynamic;
            _body.gravityScale = _gravityScale;
            _body.freezeRotation = true;
            _body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _body.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        private bool HasGroundBelow()
        {
            Vector2 origin = new Vector2(_collider.bounds.center.x,
                _collider.bounds.min.y + 0.025f);
            return TryFindGroundFrom(origin, _groundProbeDistance + 0.05f, out _);
        }

        private bool TryFindGround(out RaycastHit2D hit)
        {
            Vector2 origin = new Vector2(_collider.bounds.center.x,
                _collider.bounds.max.y + 0.025f);
            return TryFindGroundFrom(origin, _collider.bounds.size.y + _groundProbeDistance,
                out hit);
        }

        private bool TryFindGroundFrom(Vector2 origin, float distance, out RaycastHit2D hit)
        {
            int hitCount = Physics2D.RaycastNonAlloc(origin, Vector2.down, _groundHits,
                Mathf.Max(distance, 0.01f), _groundLayers);
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit2D candidate = _groundHits[i];
                if (!IsStandableSurface(candidate)) continue;
                hit = candidate;
                return true;
            }

            hit = default;
            return false;
        }

        private bool IsStandableSurface(RaycastHit2D hit)
        {
            Collider2D surface = hit.collider;
            return surface != null
                && !surface.isTrigger
                && hit.normal.y >= 0.5f
                && surface.attachedRigidbody != _body;
        }
    }
}
