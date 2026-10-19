using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Zephyr.Core.Camera
{
    /// <summary>
    /// Public gameplay-camera API and Cinemachine adapter.
    /// Cinemachine owns the actual camera calculation. This component only
    /// selects the gameplay virtual camera and applies scene-owned settings.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public sealed class CameraController : MonoBehaviour
    {
        private static readonly List<CameraController> Controllers = new List<CameraController>();
        private const string CinemachineNamespace = "Unity.Cinemachine.";
        private const string VirtualCameraTypeName = "CinemachineCamera";
        private const string BrainTypeName = "CinemachineBrain";
        private const string PositionComposerTypeName = "CinemachinePositionComposer";
        private const string ConfinerTypeName = "CinemachineConfiner2D";
        private const string ImpulseListenerTypeName = "CinemachineImpulseListener";
        private const string ImpulseSourceTypeName = "CinemachineImpulseSource";

        [SerializeField] private UnityEngine.Camera _camera;
        [SerializeField] private Component _virtualCamera;
        [SerializeField] private Component _positionComposer;
        [SerializeField] private Component _confiner;
        [SerializeField] private Component _impulseSource;
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
        private Transform _lockTarget;
        private PolygonCollider2D _rectBoundsShape;
        private PolygonCollider2D _appliedBoundsShape;
        private int _appliedShapeVersion = -1;
        private CameraBounds _roomConfiguration;
        private CameraZone _activeZone;
        private int _activeRoomRegionIndex = int.MinValue;
        private bool _setupComplete;
        private bool _isPrimary;
        private CameraMode _activeMode = (CameraMode)(-1);
        private Vector2 _activeOffset;
        private float _activeLookAheadMultiplier = 1f;
        private bool _guidanceActive;
        private bool _guidanceAtTarget;
        private bool _guidanceReturning;
        private Vector2 _guidanceStart;
        private Vector2 _guidanceEnd;
        private Vector2 _guidanceReturnPosition;
        private float _guidanceElapsed;
        private float _guidanceDuration;
        private Action _guidanceCompleted;

        internal UnityEngine.Camera OutputCamera
        {
            get
            {
                EnsureSetup();
                return _camera;
            }
        }

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

            for (int i = Controllers.Count - 1; i >= 0; i--)
            {
                CameraController candidate = Controllers[i];
                if (candidate == null)
                {
                    Controllers.RemoveAt(i);
                    continue;
                }
                if (candidate == null || !candidate.gameObject.activeInHierarchy) continue;
                fallback ??= candidate;
                if (candidate.gameObject.scene == preferredScene) preferred ??= candidate;
                if (candidate.gameObject.scene.path.EndsWith("/GameManager.unity", StringComparison.OrdinalIgnoreCase))
                    gameManager ??= candidate;
            }

            return gameManager ?? preferred ?? fallback;
        }

        /// <summary>Queues a damage-scaled Cinemachine impulse.</summary>
        public void ShakeForDamage(float damage)
        {
            if (!_setupComplete || damage <= 0f || _impulseSource == null) return;

            float duration = Mathf.Min(_shakeMaxDuration, damage * _shakeDurationPerDamage);
            float amplitude = Mathf.Clamp(damage * _shakeAmplitudePerDamage, 0f, _shakeMaxAmplitude);
            object definition = GetMember(_impulseSource, "ImpulseDefinition");
            if (definition != null && duration > 0f)
                SetMember(definition, "ImpulseDuration", duration);
            InvokeMember(_impulseSource, "GenerateImpulseWithForce", amplitude);
        }

        public void SetFollow(Transform target)
        {
            _follow = target;
            ApplyFollowTarget();
        }

        public void SetRoomBounds(Rect bounds)
        {
            _roomConfiguration = null;
            _activeRoomRegionIndex = int.MinValue;
            EnsureSetup();

            if (_rectBoundsShape == null)
            {
                GameObject shapeObject = new GameObject("RuntimeCameraBoundsShape");
                shapeObject.transform.SetParent(transform, false);
                shapeObject.transform.position = Vector3.zero;
                _rectBoundsShape = shapeObject.AddComponent<PolygonCollider2D>();
                _rectBoundsShape.isTrigger = true;
            }

            _rectBoundsShape.points = new[]
            {
                new Vector2(bounds.xMin, bounds.yMin),
                new Vector2(bounds.xMin, bounds.yMax),
                new Vector2(bounds.xMax, bounds.yMax),
                new Vector2(bounds.xMax, bounds.yMin)
            };
            _rectBoundsShape.enabled = true;
            ApplyBoundsShape(_rectBoundsShape);
        }

        public void SetRoomBounds(CameraBounds bounds)
        {
            DisableRectBoundsShape();
            _roomConfiguration = bounds;
            _activeRoomRegionIndex = int.MinValue;
            ApplyBoundsShape(bounds != null ? bounds.GetOrCreateBoundsShape() : null);
        }

        public void ClearRoomBounds()
        {
            DisableRectBoundsShape();
            _roomConfiguration = null;
            _activeRoomRegionIndex = int.MinValue;
            ClearConfiner();
        }

        public void ClearRoomBounds(CameraBounds owner)
        {
            if (_roomConfiguration != owner) return;
            ClearRoomBounds();
        }

        public void ResetToRoomBounds()
        {
            _activeZone = null;
            _activeMode = (CameraMode)(-1);
            ApplyCurrentBehavior(force: true);
        }

        public void ResetToSceneStart()
        {
            EnsureSetup();

            Vector2 resetPosition = _follow != null ? _follow.position : transform.position;
            if (_roomConfiguration != null)
            {
                CameraBehavior behavior = _roomConfiguration.GetBehavior(resetPosition);
                if (_follow == null || behavior.Mode == CameraMode.FullLock)
                    resetPosition = behavior.Anchor;
            }

            _activeZone = null;
            _activeMode = (CameraMode)(-1);
            _activeRoomRegionIndex = int.MinValue;
            SnapTo(resetPosition);
        }

        public void ApplyCameraZone(Vector2 followOffset)
        {
            _activeOffset = followOffset;
            ApplyComposerSettings(CameraMode.Follow, followOffset, 1f, Vector2.zero);
        }

        public void SnapTo(Vector2 worldPosition)
        {
            EnsureSetup();
            if (_virtualCamera == null) return;

            Vector3 position = _virtualCamera.transform.position;
            _virtualCamera.transform.position = new Vector3(worldPosition.x, worldPosition.y, position.z);
            SetMember(_virtualCamera, "PreviousStateIsValid", false);
            InvokeMember(_virtualCamera, "ForceCameraPosition", _virtualCamera.transform.position, Quaternion.identity);
            if (_lockTarget != null) _lockTarget.position = new Vector3(worldPosition.x, worldPosition.y, _lockTarget.position.z);
        }

        public bool IsGuidanceAtTarget => _guidanceAtTarget;

        public bool BeginGuidance(Vector2 worldPosition, float duration)
        {
            EnsureSetup();
            if (!_isPrimary || _virtualCamera == null) return false;

            EnsureLockTarget();
            _guidanceStart = _camera != null
                ? _camera.transform.position
                : _virtualCamera.transform.position;
            _guidanceReturnPosition = _guidanceStart;
            _guidanceEnd = worldPosition;
            _guidanceElapsed = 0f;
            _guidanceDuration = Mathf.Max(0f, duration);
            _guidanceReturning = false;
            _guidanceAtTarget = false;
            _guidanceCompleted = null;
            _guidanceActive = true;
            ApplyComposerSettings(CameraMode.FullLock, Vector2.zero, 0f, _guidanceStart);
            SetGuidanceTarget(_guidanceStart);
            return true;
        }

        public void EndGuidance(float duration, Action completed = null)
        {
            if (!_guidanceActive)
            {
                completed?.Invoke();
                return;
            }

            _guidanceStart = _camera != null
                ? _camera.transform.position
                : (_lockTarget != null ? _lockTarget.position : _virtualCamera.transform.position);
            _guidanceEnd = _guidanceReturnPosition;
            _guidanceElapsed = 0f;
            _guidanceDuration = Mathf.Max(0f, duration);
            _guidanceReturning = true;
            _guidanceAtTarget = false;
            _guidanceCompleted = completed;
        }

        private void Awake() => EnsureSetup();

        private void OnEnable()
        {
            if (!Controllers.Contains(this)) Controllers.Add(this);
            SceneManager.sceneLoaded += HandleSceneLoaded;
            SceneManager.sceneUnloaded += HandleSceneUnloaded;
            SceneManager.activeSceneChanged += HandleActiveSceneChanged;
        }

        private void OnDisable()
        {
            Controllers.Remove(this);
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneUnloaded -= HandleSceneUnloaded;
            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
        }

        private void Start() => RefreshCameraAuthorityAndTarget();

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RefreshCameraAuthorityAndTarget();
            RebindSceneCameraBounds(scene);
            ApplyCurrentBehavior(force: true);
        }

        private void HandleActiveSceneChanged(Scene previous, Scene current)
        {
            RebindSceneCameraBounds(current);
            ResetToSceneStart();
            ApplyCurrentBehavior(force: true);
        }

        private void HandleSceneUnloaded(Scene scene)
        {
            if (_roomConfiguration != null && _roomConfiguration.gameObject.scene == scene)
                ClearRoomBounds(_roomConfiguration);

            // If GameManager was unloaded, a directly opened gameplay scene can
            // reclaim the output camera until the next gameplay service is loaded.
            RefreshCameraAuthorityAndTarget();
        }

        private void LateUpdate()
        {
            if (!_setupComplete) EnsureSetup();
            if (!_isPrimary) return;
            if (_guidanceActive)
            {
                UpdateGuidance();
                return;
            }
            // PlayerSpawner binds the follow target after the player is spawned.
            if (_follow == null) return;
            ApplyCurrentBehavior(force: false);
        }

        private void UpdateGuidance()
        {
            _guidanceElapsed += Time.unscaledDeltaTime;
            float progress = _guidanceDuration <= 0f
                ? 1f
                : Mathf.Clamp01(_guidanceElapsed / _guidanceDuration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            SetGuidanceTarget(Vector2.Lerp(_guidanceStart, _guidanceEnd, easedProgress));

            if (progress < 1f) return;
            if (!_guidanceReturning)
            {
                _guidanceAtTarget = true;
                return;
            }

            _guidanceActive = false;
            _guidanceReturning = false;
            _guidanceAtTarget = false;
            _activeMode = (CameraMode)(-1);
            ApplyCurrentBehavior(force: true);
            Action completed = _guidanceCompleted;
            _guidanceCompleted = null;
            completed?.Invoke();
        }

        private void SetGuidanceTarget(Vector2 worldPosition)
        {
            EnsureLockTarget();
            _lockTarget.position = new Vector3(worldPosition.x, worldPosition.y, _lockTarget.position.z);

            if (_camera != null)
            {
                Vector3 cameraPosition = _camera.transform.position;
                _camera.transform.position = new Vector3(worldPosition.x, worldPosition.y, cameraPosition.z);
            }

            if (_virtualCamera != null)
            {
                Vector3 virtualCameraPosition = _virtualCamera.transform.position;
                _virtualCamera.transform.position = new Vector3(
                    worldPosition.x, worldPosition.y, virtualCameraPosition.z);
                InvokeMember(_virtualCamera, "ForceCameraPosition", _virtualCamera.transform.position, Quaternion.identity);
            }
        }

        private void ApplyCurrentBehavior(bool force)
        {
            if (_roomConfiguration != null)
                ApplyBoundsShape(_roomConfiguration.GetOrCreateBoundsShape());

            Vector2 followPosition = _follow != null ? _follow.position : transform.position;
            CameraZone zone = CameraZone.FindActive(followPosition);
            CameraBehavior behavior = _roomConfiguration != null
                ? _roomConfiguration.GetBehavior(followPosition)
                : new CameraBehavior(-1, CameraMode.Follow, Vector2.zero, 1f, followPosition);
            int roomRegionIndex = zone != null ? int.MinValue : behavior.RegionIndex;

            bool changed = force || zone != _activeZone || roomRegionIndex != _activeRoomRegionIndex;
            if (!changed && zone == null &&
                (_activeMode != behavior.Mode || _activeOffset != behavior.FollowOffset ||
                 !Mathf.Approximately(_activeLookAheadMultiplier, behavior.LookAheadMultiplier)))
                changed = true;
            if (!changed) return;

            _activeZone = zone;
            _activeRoomRegionIndex = roomRegionIndex;
            CameraMode mode = zone != null ? zone.Mode : behavior.Mode;
            Vector2 offset = zone != null ? zone.FollowOffset : behavior.FollowOffset;
            float lookAheadMultiplier = zone != null ? zone.LookAheadMultiplier : behavior.LookAheadMultiplier;
            ApplyComposerSettings(mode, offset, lookAheadMultiplier, zone != null ? zone.Anchor : behavior.Anchor);
        }

        private void ApplyComposerSettings(CameraMode mode, Vector2 offset, float lookAheadMultiplier, Vector2 anchor)
        {
            EnsureSetup();
            if (_positionComposer == null) return;

            _activeMode = mode;
            _activeOffset = offset;
            _activeLookAheadMultiplier = lookAheadMultiplier;

            if (mode == CameraMode.FullLock)
            {
                EnsureLockTarget();
                _lockTarget.position = new Vector3(anchor.x, anchor.y, _lockTarget.position.z);
                SetMember(_virtualCamera, "Follow", _lockTarget);
            }
            else ApplyFollowTarget();

            SetMember(_positionComposer, "TargetOffset", new Vector3(offset.x, offset.y, 0f));
            SetMember(_positionComposer, "Damping", new Vector3(_smoothTime, _smoothTime, 0f));
            float effectiveLookAheadDistance = _lookAheadDistance >= _minimumLookDirection ? _lookAheadDistance : 0f;
            SetNestedMember(_positionComposer, "Lookahead", "Enabled", effectiveLookAheadDistance > 0f);
            SetNestedMember(_positionComposer, "Lookahead", "Time", Mathf.Clamp(effectiveLookAheadDistance * 0.1f * Mathf.Max(0f, lookAheadMultiplier), 0f, 1f));
            SetNestedMember(_positionComposer, "Lookahead", "Smoothing", Mathf.Clamp(_lookAheadSmoothTime, 0f, 30f));
            SetNestedMember(_positionComposer, "Lookahead", "IgnoreY", true);

            object composition = GetMember(_positionComposer, "Composition");
            if (composition != null)
            {
                object deadZone = GetMember(composition, "DeadZone");
                if (deadZone != null)
                {
                    SetMember(deadZone, "Enabled", true);
                    SetMember(deadZone, "Size", ResolveDeadZone(mode));
                    SetMember(composition, "DeadZone", deadZone);
                }
                SetMember(_positionComposer, "Composition", composition);
            }
        }

        private Vector2 ResolveDeadZone(CameraMode mode)
        {
            float halfHeight = _camera != null ? _camera.orthographicSize : 5f;
            float halfWidth = halfHeight * (_camera != null ? _camera.aspect : 16f / 9f);
            Vector2 normalized = new Vector2(
                Mathf.Clamp01(_deadzoneSize.x / Mathf.Max(0.01f, halfWidth * 2f)),
                Mathf.Clamp01(_deadzoneSize.y / Mathf.Max(0.01f, halfHeight * 2f)));

            switch (mode)
            {
                case CameraMode.VerticalLock: normalized.x = 1f; break;
                case CameraMode.HorizontalLock: normalized.y = 1f; break;
                case CameraMode.VerticalCenter: normalized.y = 0f; break;
                case CameraMode.FullLock: normalized = Vector2.one; break;
            }

            return normalized;
        }

        private void EnsureSetup()
        {
            if (_setupComplete) return;

            Transform rig = transform.parent != null ? transform.parent : transform;
            Transform outputTransform = rig.Find("GameplayOutputCamera");
            Transform virtualCameraTransform = rig.Find("GameplayCamera");

            if (_camera == null)
            {
                _camera = outputTransform != null
                    ? outputTransform.GetComponentInChildren<UnityEngine.Camera>(true)
                    : GetComponentInChildren<UnityEngine.Camera>(true);
            }
            if (_camera == null)
            {
                GameObject output = new GameObject("GameplayOutputCamera");
                output.transform.SetParent(rig, false);
                output.transform.localPosition = new Vector3(0f, 0f, -10f);
                _camera = output.AddComponent<UnityEngine.Camera>();
                _camera.orthographic = true;
                _camera.orthographicSize = 5f;
                outputTransform = output.transform;
            }

            Type brainType = FindCinemachineType(BrainTypeName);
            Type vcamType = FindCinemachineType(VirtualCameraTypeName);
            Type composerType = FindCinemachineType(PositionComposerTypeName);
            Type confinerType = FindCinemachineType(ConfinerTypeName);
            Type listenerType = FindCinemachineType(ImpulseListenerTypeName);
            Type sourceType = FindCinemachineType(ImpulseSourceTypeName);

            if (brainType != null) EnsureComponent(_camera.gameObject, brainType);

            if (_virtualCamera == null && vcamType != null)
            {
                Transform child = virtualCameraTransform;
                if (child == null)
                {
                    GameObject cameraObject = new GameObject("GameplayCamera");
                    child = cameraObject.transform;
                    child.SetParent(rig, false);
                    child.localPosition = new Vector3(0f, 0f, -10f);
                }

                _virtualCamera = EnsureComponent(child.gameObject, vcamType);
                _positionComposer = composerType != null ? EnsureComponent(child.gameObject, composerType) : null;
                _confiner = confinerType != null ? EnsureComponent(child.gameObject, confinerType) : null;
                if (listenerType != null) EnsureComponent(child.gameObject, listenerType);
            }

            if (_impulseSource == null && sourceType != null)
                _impulseSource = EnsureComponent(gameObject, sourceType);

            if (_virtualCamera != null)
            {
                SetNestedMember(_virtualCamera, "Priority", "Value", 10);
                SetNestedMember(_virtualCamera, "Lens", "Orthographic", true);
                SetNestedMember(_virtualCamera, "Lens", "OrthographicSize", _camera.orthographicSize);
            }

            _lockTarget = rig.Find("CameraLockTarget");
            _setupComplete = true;
            ApplyFollowTarget();
        }

        private void EnsureLockTarget()
        {
            if (_lockTarget != null) return;
            GameObject target = new GameObject("CameraLockTarget");
            target.transform.SetParent(transform, false);
            _lockTarget = target.transform;
        }

        private void ApplyFollowTarget()
        {
            if (!_setupComplete || _virtualCamera == null || _activeMode == CameraMode.FullLock) return;
            SetMember(_virtualCamera, "Follow", _follow);
        }

        private void ApplyBoundsShape(PolygonCollider2D shape)
        {
            EnsureSetup();
            if (_confiner == null) return;
            int shapeVersion = _roomConfiguration != null ? _roomConfiguration.ShapeVersion : 0;
            if (ReferenceEquals(_appliedBoundsShape, shape) && _appliedShapeVersion == shapeVersion &&
                ReferenceEquals(GetMember(_confiner, "BoundingShape2D"), shape)) return;

            SetMember(_confiner, "BoundingShape2D", shape);
            InvokeMember(_confiner, "InvalidateBoundingShapeCache");
            _appliedBoundsShape = shape;
            _appliedShapeVersion = shapeVersion;
        }

        private void ClearConfiner() => ApplyBoundsShape(null);

        private void DisableRectBoundsShape()
        {
            if (_rectBoundsShape != null) _rectBoundsShape.enabled = false;
        }

        private void RefreshCameraAuthorityAndTarget()
        {
            EnsureSetup();
            CameraController primary = FindForGameplay(gameObject.scene);
            _isPrimary = primary == this;

            if (_camera != null)
            {
                _camera.enabled = _isPrimary;
                _camera.tag = _isPrimary ? "MainCamera" : "Untagged";
            }

            Component brain = _camera != null ? FindComponentByType(_camera.gameObject, BrainTypeName) : null;
            if (brain != null) brain.gameObject.SetActive(_isPrimary);
            if (_virtualCamera != null) _virtualCamera.gameObject.SetActive(_isPrimary);
            if (_isPrimary)
            {
                RebindSceneCameraBounds(SceneManager.GetActiveScene());
            }
        }

        private void RebindSceneCameraBounds(Scene preferredScene)
        {
            if (!_isPrimary) return;

            CameraBounds activeBounds = null;
            CameraBounds preferredBounds = null;
            CameraBounds fallbackBounds = null;
            Scene activeScene = SceneManager.GetActiveScene();

            for (int i = CameraBounds.Active.Count - 1; i >= 0; i--)
            {
                CameraBounds candidate = CameraBounds.Active[i];
                if (candidate == null)
                {
                    CameraBounds.Active.RemoveAt(i);
                    continue;
                }
                if (candidate == null || !candidate.isActiveAndEnabled) continue;
                Scene candidateScene = candidate.gameObject.scene;
                if (!candidateScene.IsValid() || !candidateScene.isLoaded) continue;

                fallbackBounds ??= candidate;
                if (candidateScene == preferredScene) preferredBounds ??= candidate;
                if (candidateScene == activeScene) activeBounds ??= candidate;
            }

            CameraBounds selectedBounds = activeBounds ?? preferredBounds ?? fallbackBounds;
            if (selectedBounds != null)
            {
                if (_roomConfiguration != selectedBounds)
                    SetRoomBounds(selectedBounds);
                else
                    ApplyBoundsShape(selectedBounds.GetOrCreateBoundsShape());
                return;
            }

            // A destroyed Unity object compares equal to null; still clear the
            // old Confiner reference after the final gameplay scene unloads.
            ClearRoomBounds();
        }


        private static Type FindCinemachineType(string shortName)
        {
            string fullName = string.Concat(CinemachineNamespace, shortName);
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName, false);
                if (type != null) return type;
            }
            return null;
        }

        private static Component EnsureComponent(GameObject target, Type type)
        {
            Component component = target.GetComponent(type);
            return component != null ? component : target.AddComponent(type);
        }

        private static Component FindComponentByType(GameObject target, string shortName)
        {
            Type type = FindCinemachineType(shortName);
            return type != null ? target.GetComponent(type) : null;
        }

        private static object GetMember(object target, string memberName)
        {
            if (target == null) return null;
            Type type = target.GetType();
            PropertyInfo property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && property.CanRead) return property.GetValue(target);
            FieldInfo field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return field != null ? field.GetValue(target) : null;
        }

        private static bool SetMember(object target, string memberName, object value)
        {
            if (target == null) return false;
            Type type = target.GetType();
            PropertyInfo property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && property.CanWrite && CanAssign(property.PropertyType, value))
            {
                property.SetValue(target, value);
                return true;
            }

            FieldInfo field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null && CanAssign(field.FieldType, value))
            {
                field.SetValue(target, value);
                return true;
            }
            return false;
        }

        private static void SetNestedMember(object target, string parentName, string memberName, object value)
        {
            object parent = GetMember(target, parentName);
            if (parent == null) return;
            if (SetMember(parent, memberName, value)) SetMember(target, parentName, parent);
        }

        private static bool CanAssign(Type targetType, object value)
        {
            if (value == null) return !targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null;
            return targetType.IsInstanceOfType(value) || targetType == value.GetType();
        }

        private static void InvokeMember(Component target, string methodName, params object[] arguments)
        {
            if (target == null) return;
            MethodInfo[] methods = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (MethodInfo method in methods)
            {
                if (method.Name != methodName || method.GetParameters().Length != arguments.Length) continue;
                try
                {
                    method.Invoke(target, arguments);
                    return;
                }
                catch (ArgumentException)
                {
                    // Cinemachine minor versions can expose different overloads.
                }
            }
        }
    }
}
