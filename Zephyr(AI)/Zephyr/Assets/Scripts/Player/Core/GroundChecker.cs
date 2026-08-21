/*
 * GroundChecker.cs
 * ----------------
 * Module:  Player / Core
 * Purpose: Continuous ground and wall contact detection.
 *          Ground uses an overlap circle, while walls use collision normals
 *          so ground and walls can safely share the same layer.
 * Scene:    GameManager (attached to player prefab's "Core" child).
 * Ch.Ref:   Ch.6.8 Physics / Ground & Wall Detection.
 */
using UnityEngine;

namespace Zephyr.Gameplay.Player.Core
{
    /// <summary>
    /// Detects ground and wall contact for the player. Ground uses an overlap
    /// circle, while wall contact is identified from horizontal collision normals
    /// on the player's collider. This allows ground and walls to share a layer.
    /// Exposes IsGrounded, IsAgainstWallLeft, IsAgainstWallRight for any system
    /// that needs contact information (movement, jump conditions, wall-siphon
    /// prevention, etc.).
    /// Runs before MovementCore (execution order -100) so contact state is
    /// always up-to-date when movement or jump logic reads it.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GroundChecker : MonoBehaviour
    {
        [Header("Ground Detection")]
        [Tooltip("Layer mask for ground and wall detection. Select Tilemap, Platforms, Walls, etc.")]
        [SerializeField] private LayerMask _contactLayer;

        [Tooltip("Distance below the root position to place the ground detection circle center.")]
        [SerializeField] private float _groundCheckDistance = 0.1f;

        [Tooltip("Radius of the ground overlap circle. Larger values are more forgiving.")]
        [SerializeField] private float _groundCheckRadius = 0.15f;

        [Header("Wall Detection")]
        [Tooltip("Distance beyond the player collider used to detect nearby walls.")]
        [SerializeField] private float _wallCheckDistance = 0.1f;

        private const float k_minWallNormalX = 0.5f;
        private const int k_maxWallHitCount = 8;

        private readonly RaycastHit2D[] _wallHits = new RaycastHit2D[k_maxWallHitCount];
        private Collider2D _playerCollider;
        private ContactFilter2D _contactFilter;

        /// <summary>
        /// True when the player is touching the ground. Updated every FixedUpdate.
        /// </summary>
        public bool IsGrounded { get; private set; }

        /// <summary>
        /// True when the player's left side is touching a wall.
        /// </summary>
        public bool IsAgainstWallLeft { get; private set; }

        /// <summary>
        /// True when the player's right side is touching a wall.
        /// </summary>
        public bool IsAgainstWallRight { get; private set; }

        private void Awake()
        {
            _playerCollider = GetComponentInParent<Collider2D>();
            _contactFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = _contactLayer,
                useTriggers = false
            };

            Debug.Log($"[GroundChecker] Awake: layerMask={_contactLayer.value}, groundDist={_groundCheckDistance}, groundR={_groundCheckRadius}, collider={_playerCollider != null}");
        }

        private void FixedUpdate()
        {
            // Ground detection
            // Use the collider's actual feet instead of the actor root. The
            // player collider is offset from the root, so a root-centered
            // overlap can miss the floor while the player is visibly standing.
            var bounds = _playerCollider != null
                ? _playerCollider.bounds
                : new Bounds(transform.root.position, Vector3.zero);
            var groundOrigin = new Vector2(bounds.center.x, bounds.min.y - _groundCheckDistance);
            var wasGrounded = IsGrounded;
            IsGrounded = Physics2D.OverlapCircle(groundOrigin, _groundCheckRadius, _contactLayer) != null;

            if (wasGrounded != IsGrounded)
            {
                Debug.Log($"[GroundChecker] Grounded: {wasGrounded}→{IsGrounded}");
            }

            UpdateWallContacts();
        }

        private void UpdateWallContacts()
        {
            IsAgainstWallLeft = HasWallInDirection(Vector2.left, true);
            IsAgainstWallRight = HasWallInDirection(Vector2.right, false);
        }

        private bool HasWallInDirection(Vector2 direction, bool isLeft)
        {
            if (_playerCollider == null) return false;

            var hitCount = _playerCollider.Cast(
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
            // Ground gizmo
            var bounds = _playerCollider != null
                ? _playerCollider.bounds
                : new Bounds(transform.root.position, Vector3.zero);
            var groundOrigin = new Vector2(bounds.center.x, bounds.min.y - _groundCheckDistance);
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundOrigin, _groundCheckRadius);

            if (_playerCollider == null) return;

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
    }
}
