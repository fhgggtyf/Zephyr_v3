using UnityEngine;
using UnityEngine.SceneManagement;

namespace Zephyr.Core.Camera
{
    /// <summary>
    /// Hollow Knight-style 2D camera: the player can move inside a dead zone,
    /// the camera biases toward the facing direction, and scene-owned zones
    /// can lock or center individual axes.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public sealed class CameraController : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera _camera;
        [SerializeField] private Vector2 _deadzoneSize = new Vector2(3f, 2f);
        [SerializeField] private float _smoothTime = 0.12f;
        [SerializeField, Min(0f)] private float _lookAheadDistance = 1.5f;
        [SerializeField, Min(0.01f)] private float _lookAheadSmoothTime = 0.2f;
        [SerializeField, Min(0.01f)] private float _minimumLookDirection = 0.05f;
        [Header("Screen shake")]
        [SerializeField, Min(0f)] private float _shakeDurationPerDamage = 0.012f;
        [SerializeField, Min(0f)] private float _shakeMaxDuration = 0.45f;
        [SerializeField, Min(0f)] private float _shakeAmplitudePerDamage = 0.012f;
        [SerializeField, Min(0f)] private float _shakeMaxAmplitude = 0.35f;
        private Transform _follow;
        private Rect _roomBounds = new Rect(-1000f, -1000f, 2000f, 2000f);
        private Vector3 _velocity;
        private Vector2 _zoneOffset;
        private Vector2 _lookAhead;
        private Vector2 _lookDirection;
        private Vector2 _lastFollowPosition;
        private bool _hasFollowPosition;
        private CameraZone _activeZone;
        private CameraBounds _roomConfiguration;
        private CameraBounds _activeBehaviorBounds;
        private int _activeRoomRegionIndex = int.MinValue;
        private float _shakeUntil;
        private float _shakeDuration;
        private float _shakeAmplitude;
        private Vector3 _logicalPosition;
        private bool _hasLogicalPosition;

        /// <summary>
        /// Resolves the gameplay camera deterministically. GameManager owns the
        /// shared camera when it is loaded; otherwise the requested gameplay
        /// scene camera is used as a standalone fallback.
        /// </summary>
        public static CameraController FindForGameplay(Scene preferredScene)
        {
            CameraController fallback = null;
            CameraController preferred = null;
            CameraController gameManager = null;

            foreach (CameraController candidate in Object.FindObjectsByType<CameraController>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate == null || !candidate.gameObject.activeInHierarchy) continue;
                fallback ??= candidate;
                if (candidate.gameObject.scene == preferredScene) preferred ??= candidate;
                if (candidate.gameObject.scene.path.EndsWith("/GameManager.unity", System.StringComparison.OrdinalIgnoreCase))
                    gameManager ??= candidate;
            }

            return gameManager ?? preferred ?? fallback;
        }

        /// <summary>Queues a short, damage-scaled camera shake.</summary>
        public void ShakeForDamage(float damage)
        {
            if (damage <= 0f) return;
            float duration = Mathf.Min(_shakeMaxDuration, damage * _shakeDurationPerDamage);
            _shakeDuration = Mathf.Max(_shakeDuration, duration);
            _shakeUntil = Mathf.Max(_shakeUntil, Time.unscaledTime + duration);
            _shakeAmplitude = Mathf.Clamp(Mathf.Max(_shakeAmplitude, damage * _shakeAmplitudePerDamage), 0f, _shakeMaxAmplitude);
        }

        public void SetFollow(Transform target)
        {
            _follow = target;
            _hasFollowPosition = target != null;
            if (target != null) _lastFollowPosition = target.position;
            _lookAhead = Vector2.zero;
            _lookDirection = Vector2.zero;
            _lookAheadVelocity = Vector2.zero;
            _velocity = Vector3.zero;
            RestoreLogicalPosition();
        }
        public void SetRoomBounds(Rect bounds)
        {
            _roomConfiguration = null;
            _roomBounds = bounds;
            _activeBehaviorBounds = null;
            _activeRoomRegionIndex = int.MinValue;
        }
        public void SetRoomBounds(CameraBounds bounds)
        {
            _roomConfiguration = bounds;
            _roomBounds = bounds != null ? bounds.Bounds : _roomBounds;
            _activeBehaviorBounds = null;
            _activeRoomRegionIndex = int.MinValue;
        }
        public void ClearRoomBounds()
        {
            _roomConfiguration = null;
            _roomBounds = new Rect(-1000f, -1000f, 2000f, 2000f);
            _activeBehaviorBounds = null;
            _activeRoomRegionIndex = int.MinValue;
        }
        public void ClearRoomBounds(CameraBounds owner)
        {
            if (_roomConfiguration != owner) return;
            _roomConfiguration = null;
            ClearRoomBounds();
        }
        public void ResetToRoomBounds() => _zoneOffset = Vector2.zero;
        public void ApplyCameraZone(Vector2 followOffset) => _zoneOffset = followOffset;
        public void SnapTo(Vector2 worldPosition)
        {
            _logicalPosition = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
            _hasLogicalPosition = true;
            transform.position = _logicalPosition;
            _velocity = Vector3.zero;
            _lookAheadVelocity = Vector2.zero;
            RestoreLogicalPosition();
        }

        private void Awake()
        {
            if (_camera == null) _camera = GetComponentInChildren<UnityEngine.Camera>();
            _logicalPosition = transform.position;
            _hasLogicalPosition = true;
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void Start()
        {
            // Gameplay scenes may still contain a standalone camera for editor
            // authoring. Once GameManager is loaded, only its camera may render.
            RefreshCameraAuthorityAndTarget();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // A direct-open scene can start its local camera before ColdStartup
            // loads GameManager. Re-evaluate authority after every additive load
            // so the shared GameManager camera takes over deterministically.
            RefreshCameraAuthorityAndTarget();
        }

        private void RefreshCameraAuthorityAndTarget()
        {
            CameraController primary = FindForGameplay(gameObject.scene);
            if (primary != this)
            {
                if (_camera != null) _camera.enabled = false;
                return;
            }

            if (_camera != null) _camera.enabled = true;
            TryBindSceneOwnedPlayer();
        }

        private void TryBindSceneOwnedPlayer()
        {
            if (_follow != null) return;

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                SetFollow(player.transform);
        }

        private void LateUpdate()
        {
            // Follow calculations use a dedicated logical position. The rendered
            // transform may be shaken, but that visual offset never feeds back into
            // dead-zone, bounds, or smoothing calculations.
            if (_follow == null)
            {
                RestoreLogicalPosition();
                return;
            }
            if (!_hasLogicalPosition)
            {
                _logicalPosition = transform.position;
                _hasLogicalPosition = true;
            }
            Vector3 current = _logicalPosition;
            Vector3 target = current;
            Vector2 followPosition = _follow.position;
            Vector2 followVelocity = Vector2.zero;
            if (_hasFollowPosition)
            {
                float dt = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
                followVelocity = (followPosition - _lastFollowPosition) / dt;
            }
            _lastFollowPosition = followPosition;
            _hasFollowPosition = true;

            if (_roomConfiguration != null)
                _roomBounds = _roomConfiguration.Bounds;

            CameraZone zone = CameraZone.FindActive(followPosition);
            CameraBehavior roomBehavior = _roomConfiguration != null
                ? _roomConfiguration.GetBehavior(followPosition)
                : new CameraBehavior(-1, CameraMode.Follow, _zoneOffset, 1f, Vector2.zero);
            int roomRegionIndex = zone != null ? int.MinValue : roomBehavior.RegionIndex;
            if (zone != _activeZone || _roomConfiguration != _activeBehaviorBounds || roomRegionIndex != _activeRoomRegionIndex)
            {
                _activeZone = zone;
                _activeBehaviorBounds = _roomConfiguration;
                _activeRoomRegionIndex = roomRegionIndex;
                _velocity = Vector3.zero;
                _lookAhead = Vector2.zero;
                _lookAheadVelocity = Vector2.zero;
            }

            CameraMode mode = zone != null ? zone.Mode : roomBehavior.Mode;
            Vector2 zoneOffset = zone != null ? zone.FollowOffset : roomBehavior.FollowOffset;
            Vector2 lookDirection = ResolveLookDirection(followVelocity);
            Vector2 desiredLookAhead = lookDirection * _lookAheadDistance;
            desiredLookAhead *= zone != null ? zone.LookAheadMultiplier : roomBehavior.LookAheadMultiplier;
            _lookAhead = Vector2.SmoothDamp(_lookAhead, desiredLookAhead, ref _lookAheadVelocity,
                _lookAheadSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);

            Vector2 delta = followPosition - (Vector2)current - zoneOffset - _lookAhead;
            bool canMoveX = mode != CameraMode.VerticalLock && mode != CameraMode.FullLock;
            bool canMoveY = mode != CameraMode.HorizontalLock && mode != CameraMode.FullLock;
            if (mode == CameraMode.VerticalCenter) canMoveY = true;
            if (canMoveX && Mathf.Abs(delta.x) > _deadzoneSize.x * 0.5f) target.x = followPosition.x - zoneOffset.x - _lookAhead.x;
            if (canMoveY && Mathf.Abs(delta.y) > _deadzoneSize.y * 0.5f) target.y = followPosition.y - zoneOffset.y - _lookAhead.y;
            if (mode == CameraMode.VerticalCenter) target.y = followPosition.y - zoneOffset.y;
            if (mode == CameraMode.FullLock)
            {
                Vector2 anchor = zone != null ? zone.Anchor : roomBehavior.Anchor;
                target = new Vector3(anchor.x, anchor.y, current.z);
            }

            Rect activeBounds = zone != null ? zone.WorldBounds : _roomBounds;
            float halfHeight = _camera != null ? _camera.orthographicSize : 5f;
            float halfWidth = _camera != null ? halfHeight * _camera.aspect : 8.9f;
            float minX = activeBounds.xMin + halfWidth;
            float maxX = activeBounds.xMax - halfWidth;
            float minY = activeBounds.yMin + halfHeight;
            float maxY = activeBounds.yMax - halfHeight;
            target.x = ClampForViewport(target.x, minX, maxX);
            target.y = ClampForViewport(target.y, minY, maxY);

            if (mode != CameraMode.FullLock)
            {
                if (!canMoveX) target.x = current.x;
                if (!canMoveY && mode != CameraMode.VerticalCenter) target.y = current.y;
            }

            Vector3 next = Vector3.SmoothDamp(current, target, ref _velocity, _smoothTime,
                Mathf.Infinity, Time.unscaledDeltaTime);
            // SmoothDamp can retain a tiny residual velocity while the target
            // is pinned to a room edge. Lock the exact edge to prevent visible
            // sub-pixel jitter while the player keeps pushing against it.
            if (IsApproachingBound(target.x, next.x, minX, maxX))
            {
                next.x = Mathf.Clamp(next.x, minX, maxX);
                _velocity.x = 0f;
            }

            if (IsApproachingBound(target.y, next.y, minY, maxY))
            {
                next.y = Mathf.Clamp(next.y, minY, maxY);
                _velocity.y = 0f;
            }

            if (Time.unscaledTime < _shakeUntil)
            {
                float strength = Mathf.Clamp01((_shakeUntil - Time.unscaledTime) / Mathf.Max(0.01f, _shakeDuration));
                Vector2 offset = Random.insideUnitCircle * (_shakeAmplitude * strength);
                _logicalPosition = next;
                transform.position = next + new Vector3(offset.x, offset.y, 0f);
            }
            else
            {
                _shakeAmplitude = 0f;
                _shakeDuration = 0f;
                _logicalPosition = next;
                transform.position = next;
            }
        }

        private void RestoreLogicalPosition()
        {
            if (_hasLogicalPosition)
                transform.position = _logicalPosition;
        }

        private Vector2 _lookAheadVelocity;

        private Vector2 ResolveLookDirection(Vector2 followVelocity)
        {
            if (Mathf.Abs(followVelocity.x) > _minimumLookDirection)
                _lookDirection = new Vector2(Mathf.Sign(followVelocity.x), 0f);
            return _lookDirection;
        }

        private static float ClampForViewport(float value, float min, float max)
        {
            // If a room is smaller than the camera viewport, keep the camera
            // centered on the room instead of passing an inverted Clamp range.
            return min > max ? (min + max) * 0.5f : Mathf.Clamp(value, min, max);
        }

        private static bool IsApproachingBound(float target, float value, float min, float max)
        {
            if (min > max) return false;
            bool targetIsBound = Mathf.Abs(target - min) <= 0.0001f || Mathf.Abs(target - max) <= 0.0001f;
            return targetIsBound && Mathf.Abs(value - target) <= 0.02f;
        }
    }
}
