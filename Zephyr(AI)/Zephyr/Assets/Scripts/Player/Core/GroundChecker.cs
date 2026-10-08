/*
 * GroundChecker.cs
 * ----------------
 * Module:  Player / Core
 * Purpose: Continuous ground and wall contact detection.
 *          Ground uses an explicit foot Transform, while walls use collision
 *          normals so ground and walls can safely share the same layer.
 * Scene:    GameManager (attached to player prefab's "Core" child).
 * Ch.Ref:   Ch.6.8 Physics / Ground & Wall Detection.
 */
using UnityEngine;

namespace Zephyr.Gameplay.Player.Core
{
    /// <summary>
    /// Detects ground and wall contact for the player. Ground uses an explicit
    /// foot Transform and overlap circle, while wall contact is identified from
    /// horizontal collision normals on the player's collider. This allows ground
    /// and walls to share a layer without deriving the probe from visual bounds.
    /// Exposes the existing contact API consumed by MovementCore and conditions.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GroundChecker : MonoBehaviour
    {
        [Header("Ground Detection")]
        [Tooltip("Layer mask for ground and wall detection. Select Tilemap, Platforms, Walls, etc.")]
        [SerializeField] private LayerMask _contactLayer;

        [Tooltip("Explicit transform placed at the player's feet, matching the tutorial-style CollisionSenses groundCheck.")]
        [SerializeField] private Transform _groundCheck;

        [Tooltip("Fallback body collider used when PlayerColliderController is unavailable. Runtime casts use the active profile collider.")]
        [SerializeField] private Collider2D _bodyCollider;

        [Tooltip("Small downward offset applied to the explicit ground check to tolerate tiny gaps at tile/platform seams.")]
        [SerializeField] private float _groundCheckDistance = 0.1f;

        [Tooltip("Radius of the ground overlap circle. Larger values are more forgiving.")]
        [SerializeField] private float _groundCheckRadius = 0.15f;

        [Tooltip("Legacy compatibility field. Phase 1 no longer gates landing on multiple consecutive frames.")]
        [SerializeField] private int _requiredStableGroundFrames = 2;

        [Tooltip("Short grace period that suppresses a one-frame ground probe loss after landing.")]
        [SerializeField] private float _groundedGracePeriod = 0.08f;

        [Header("Wall Detection")]
        [Tooltip("Distance beyond the player collider used to detect nearby walls.")]
        [SerializeField] private float _wallCheckDistance = 0.1f;

        private const float k_minWallNormalX = 0.5f;
        private const int k_maxWallHitCount = 8;

        private readonly RaycastHit2D[] _wallHits = new RaycastHit2D[k_maxWallHitCount];
        private PlayerColliderController _colliderController;
        private Collider2D _playerCollider;
        private ContactFilter2D _contactFilter;
        private int _groundedFrameCount;
        private float _lastGroundContactTime = float.NegativeInfinity;

        /// <summary>
        /// True when the player is touching the ground. Updated in Update so the
        /// StateMachine reads the current visual-frame contact result.
        /// </summary>
        public bool IsGrounded { get; private set; }

        /// <summary>
        /// Compatibility alias used by the existing StateCondition SOs. Ground
        /// contact is intentionally not gated by a multi-frame stable-contact
        /// requirement, matching the tutorial's direct OverlapCircle check.
        /// </summary>
        public bool HasStableGroundContact => IsGrounded;

        /// <summary>
        /// True briefly after the last valid ground probe. Locomotion states use
        /// this to ignore a one-frame ground probe loss at a platform edge.
        /// </summary>
        public bool HasGroundedGrace =>
            Time.time - _lastGroundContactTime <= Mathf.Max(0f, _groundedGracePeriod);

        public bool IsAgainstWallLeft { get; private set; }
        public bool IsAgainstWallRight { get; private set; }

        private void Awake()
        {
            // The serialized references are authoritative. The name fallback
            // keeps older prefab instances functional during the transition.
            if (_groundCheck == null)
            {
                _groundCheck = transform.root.Find("CollisionChecks/GroundCheck");
            }

            _colliderController = GetComponentInParent<PlayerColliderController>();
            _playerCollider = _bodyCollider != null
                ? _bodyCollider : GetComponentInParent<Collider2D>();

            _contactFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = _contactLayer,
                useTriggers = false
            };

            Debug.Log($"[GroundChecker] Awake: layerMask={_contactLayer.value}, groundCheck={_groundCheck != null}, groundDist={_groundCheckDistance}, groundR={_groundCheckRadius}, collider={_playerCollider != null}");
        }

        private void Update()
        {
            // Update ground in Update so the StateMachine (which evaluates in
            // Update) sees the same contact result as the visual frame. This is
            // the direct-read behavior used by the tutorial's CollisionSenses.
            var wasGrounded = IsGrounded;
            IsGrounded = HasGroundContact();

            if (IsGrounded)
            {
                _groundedFrameCount = Mathf.Min(
                    _groundedFrameCount + 1,
                    Mathf.Max(1, _requiredStableGroundFrames));
                _lastGroundContactTime = Time.time;
            }
            else
            {
                _groundedFrameCount = 0;
            }

            if (wasGrounded != IsGrounded)
            {
                Debug.Log($"[GroundChecker] Grounded: {wasGrounded}→{IsGrounded}");
            }
        }

        private void FixedUpdate()
        {
            UpdateWallContacts();
        }

        private bool HasGroundContact()
        {
            if (_groundCheck == null) return false;

            var radius = Mathf.Max(0.01f, _groundCheckRadius);
            var origin = (Vector2)_groundCheck.position
                         + Vector2.down * Mathf.Max(0f, _groundCheckDistance);
            return Physics2D.OverlapCircle(origin, radius, _contactLayer) != null;
        }

        private void UpdateWallContacts()
        {
            IsAgainstWallLeft = HasWallInDirection(Vector2.left, true);
            IsAgainstWallRight = HasWallInDirection(Vector2.right, false);
        }

        private bool HasWallInDirection(Vector2 direction, bool isLeft)
        {
            var playerCollider = GetActivePlayerCollider();
            if (playerCollider == null) return false;

            var hitCount = playerCollider.Cast(
                direction,
                _contactFilter,
                _wallHits,
                Mathf.Max(0f, _wallCheckDistance));

            for (int i = 0; i < hitCount; i++)
            {
                var normalX = _wallHits[i].normal.x;
                if (isLeft && normalX > k_minWallNormalX) return true;
                if (!isLeft && normalX < -k_minWallNormalX) return true;
            }

            return false;
        }

        private void OnDrawGizmosSelected()
        {
            var radius = Mathf.Max(0.01f, _groundCheckRadius);
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            if (_groundCheck != null)
            {
                var origin = _groundCheck.position + Vector3.down * Mathf.Max(0f, _groundCheckDistance);
                Gizmos.DrawWireSphere(origin, radius);
                Gizmos.DrawLine(origin, origin + Vector3.down * Mathf.Max(0.02f, _groundCheckDistance));
            }

            var playerCollider = GetActivePlayerCollider();
            if (playerCollider == null) return;

            var bounds = playerCollider.bounds;
            var wallDistance = Mathf.Max(0f, _wallCheckDistance);
            var leftCheckX = bounds.min.x - wallDistance;
            var rightCheckX = bounds.max.x + wallDistance;

            Gizmos.color = IsAgainstWallLeft ? Color.green : Color.red;
            Gizmos.DrawLine(
                new Vector3(leftCheckX, bounds.min.y),
                new Vector3(leftCheckX, bounds.max.y));

            Gizmos.color = IsAgainstWallRight ? Color.green : Color.red;
            Gizmos.DrawLine(
                new Vector3(rightCheckX, bounds.min.y),
                new Vector3(rightCheckX, bounds.max.y));
        }

        private Collider2D GetActivePlayerCollider()
        {
            return _colliderController != null
                ? _colliderController.ActiveCollider ?? _playerCollider
                : _playerCollider != null ? _playerCollider : _bodyCollider;
        }
    }
}
