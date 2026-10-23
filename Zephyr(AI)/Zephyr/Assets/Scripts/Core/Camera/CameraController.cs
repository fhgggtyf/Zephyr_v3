using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zephyr.Contracts.Interfaces;

namespace Zephyr.Core.Camera
{
    /// <summary>
    /// Public gameplay-camera API and Cinemachine adapter.
    /// Cinemachine owns the actual camera calculation. This component only
    /// selects the gameplay virtual camera and applies scene-owned settings.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public sealed class CameraController : MonoBehaviour, ICameraShakeGateway
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
        [SerializeField] private Component _brain;
        [SerializeField] private Component _virtualCamera;
        [SerializeField] private Component _positionComposer;
        [SerializeField] private Component _confiner;
        [SerializeField] private Component _impulseSource;
        [SerializeField] private Vector2 _deadzoneSize = new Vector2(3f, 2f);
        [SerializeField] private float _smoothTime = 0.12f;
        [SerializeField] private bool _runtimeDiagnostics;
        [SerializeField, Min(1)] private int _runtimeDiagnosticsInterval = 5;
        [SerializeField, Min(0.0001f)] private float _stabilityPositionThreshold = 0.01f;
        [SerializeField, Min(1)] private int _requiredStableFrames = 5;
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
        private bool _hasOutputSample;
        private Vector3 _lastOutputPosition;
        private int _stableFrameCount;
        private int _runtimeDiagnosticsFrame;
        private bool _hasDiagnosticsSample;
        private Vector3 _lastDiagnosticsOutput;
        private Vector3 _lastDiagnosticsLockTarget;
        private bool _diagnosticsSnapshotRequested;
        private bool _lastDiagnosticsGuidanceActive;
        private bool _lastDiagnosticsReturning;
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

            float amplitude = Mathf.Clamp(damage * _shakeAmplitudePerDamage, 0f, _shakeMaxAmplitude);
            InvokeMember(_impulseSource, "GenerateImpulseWithForce", amplitude);
        }

        public void SetFollow(Transform target)
        {
            bool targetChanged = _follow != target;
            if (targetChanged)
                ResetStabilityTracking();
            _follow = target;
            ApplyFollowTarget();

            if (!targetChanged || target == null || _guidanceActive) return;

            _activeZone = null;
            _activeMode = (CameraMode)(-1);
            _activeRoomRegionIndex = int.MinValue;
            ApplyCurrentBehavior(force: true);

            Vector2 initialPosition = target.position;
            if (_activeZone != null && _activeZone.Mode == CameraMode.FullLock)
                initialPosition = _activeZone.Anchor;
            else if (_roomConfiguration != null)
            {
                CameraBehavior behavior = _roomConfiguration.GetBehavior(target.position);
                if (behavior.Mode == CameraMode.FullLock)
                    initialPosition = behavior.Anchor;
            }

            SnapTo(initialPosition);
        }

        public bool IsFollowing(Transform target)
        {
            return target != null && _setupComplete && _isPrimary && _follow == target;
        }

        public bool IsFollowingStable(Transform target)
        {
            return IsFollowing(target) && _stableFrameCount >= _requiredStableFrames;
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

            // A scene transition can leave the Brain holding the previous root frame.
            // Clear that frame before the next LateUpdate so initialization cannot blend
            // from the old map position to the new player position.
            ResetBrainState();
            InvokeMember(_virtualCamera, "UpdateCameraState", Vector3.up, -1f);
            SynchronizeOutputCamera();
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
            _guidanceReturnPosition = _follow != null ? _follow.position : _guidanceStart;
            _guidanceEnd = worldPosition;
            _guidanceElapsed = 0f;
            _guidanceDuration = Mathf.Max(0f, duration);
            _guidanceReturning = false;
            _guidanceAtTarget = false;
            _guidanceCompleted = null;
            _guidanceActive = true;
            ApplyComposerSettings(CameraMode.FullLock, Vector2.zero, 0f, _guidanceStart);
            SetGuidanceTarget(_guidanceStart);
            InvalidateVirtualCameraState();
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

        private void Update()
        {
            if (!_setupComplete) EnsureSetup();
            if (!_isPrimary)
            {
                ResetStabilityTracking();
                return;
            }
            if (_guidanceActive)
            {
                UpdateGuidance();
                return;
            }
            // PlayerSpawner binds the follow target after the player is spawned.
            if (_follow == null)
            {
                ResetStabilityTracking();
                return;
            }
            ApplyCurrentBehavior(force: false);
        }

        private void LateUpdate()
        {
            if (!_setupComplete) EnsureSetup();
            if (!_isPrimary)
            {
                ResetStabilityTracking();
                return;
            }

            if (_guidanceActive || _follow == null)
            {
                ResetStabilityTracking();
                EmitRuntimeDiagnostics();
                return;
            }

            UpdateStabilityTracking();
            EmitRuntimeDiagnostics();
        }

        private void EmitRuntimeDiagnostics()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!_runtimeDiagnostics) return;
            _runtimeDiagnosticsFrame++;

            Vector3 outputPosition = _camera != null ? _camera.transform.position : Vector3.zero;
            Vector3 virtualPosition = _virtualCamera != null ? _virtualCamera.transform.position : Vector3.zero;
            Vector3 lockPosition = _lockTarget != null ? _lockTarget.position : Vector3.zero;
            Vector3 followPosition = _follow != null ? _follow.position : Vector3.zero;
            Vector3 outputDelta = _hasDiagnosticsSample ? outputPosition - _lastDiagnosticsOutput : Vector3.zero;
            Vector3 lockDelta = _hasDiagnosticsSample ? lockPosition - _lastDiagnosticsLockTarget : Vector3.zero;

            bool guidanceStateChanged = _guidanceActive != _lastDiagnosticsGuidanceActive ||
                _guidanceReturning != _lastDiagnosticsReturning;
            bool periodicSnapshot = _runtimeDiagnosticsInterval > 0 &&
                _runtimeDiagnosticsFrame % _runtimeDiagnosticsInterval == 0;
            if (!_diagnosticsSnapshotRequested && !guidanceStateChanged && !periodicSnapshot)
            {
                _lastDiagnosticsOutput = outputPosition;
                _lastDiagnosticsLockTarget = lockPosition;
                _hasDiagnosticsSample = true;
                return;
            }

            Debug.LogFormat(
                "CAMERA_TRACE frame={0} output={1} outputDelta={2} virtual={3} lock={4} lockDelta={5} follow={6} guidance={7} returning={8} mode={9}",
                _runtimeDiagnosticsFrame, outputPosition, outputDelta, virtualPosition, lockPosition,
                lockDelta, followPosition, _guidanceActive, _guidanceReturning, _activeMode);

            _lastDiagnosticsOutput = outputPosition;
            _lastDiagnosticsLockTarget = lockPosition;
            _hasDiagnosticsSample = true;
            _lastDiagnosticsGuidanceActive = _guidanceActive;
            _lastDiagnosticsReturning = _guidanceReturning;
            _diagnosticsSnapshotRequested = false;
#endif
        }

        public void SetRuntimeDiagnostics(bool enabled, int interval = 0)
        {
            _runtimeDiagnostics = enabled;
            _runtimeDiagnosticsInterval = Mathf.Max(0, interval);
            _runtimeDiagnosticsFrame = 0;
            _hasDiagnosticsSample = false;
            _diagnosticsSnapshotRequested = enabled;
            _lastDiagnosticsGuidanceActive = _guidanceActive;
            _lastDiagnosticsReturning = _guidanceReturning;
        }

        public void LogRuntimeDiagnosticsSnapshot()
        {
            _diagnosticsSnapshotRequested = true;
        }

        private void UpdateStabilityTracking()
        {
            if (_camera == null)
            {
                ResetStabilityTracking();
                return;
            }

            Vector3 outputPosition = _camera.transform.position;
            if (!_hasOutputSample)
            {
                _hasOutputSample = true;
                _lastOutputPosition = outputPosition;
                _stableFrameCount = 0;
                return;
            }

            float threshold = Mathf.Max(0.0001f, _stabilityPositionThreshold);
            if ((outputPosition - _lastOutputPosition).sqrMagnitude <= threshold * threshold)
                _stableFrameCount++;
            else
                _stableFrameCount = 0;

            _lastOutputPosition = outputPosition;
        }

        private void ResetStabilityTracking()
        {
            _hasOutputSample = false;
            _stableFrameCount = 0;
        }

        private void UpdateGuidance()
        {
            _guidanceElapsed += Time.unscaledDeltaTime;
            float progress = _guidanceDuration <= 0f
                ? 1f
                : Mathf.Clamp01(_guidanceElapsed / _guidanceDuration);
            float easedProgress = 0.5f - 0.5f * Mathf.Cos(progress * Mathf.PI);
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

        }

        private void ApplyCurrentBehavior(bool force)
        {
            if (_roomConfiguration != null)
                ApplyBoundsShape(_roomConfiguration.GetOrCreateBoundsShape());

            Vector2 followPosition = _follow != null ? _follow.position : transform.position;
            Scene followScene = _follow != null ? _follow.gameObject.scene : SceneManager.GetActiveScene();
            CameraZone zone = CameraZone.FindActive(followScene, followPosition);
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

            bool isFullLock = mode == CameraMode.FullLock;
            SetMember(_positionComposer, "TargetOffset", new Vector3(offset.x, offset.y, 0f));
            SetMember(_positionComposer, "Damping", isFullLock ? Vector3.zero : new Vector3(_smoothTime, _smoothTime, 0f));
            float effectiveLookAheadDistance = _lookAheadDistance >= _minimumLookDirection ? _lookAheadDistance : 0f;
            bool lookAheadEnabled = !isFullLock && effectiveLookAheadDistance > 0f && lookAheadMultiplier > 0f;
            SetNestedMember(_positionComposer, "Lookahead", "Enabled", lookAheadEnabled);
            SetNestedMember(_positionComposer, "Lookahead", "Time", lookAheadEnabled
                ? Mathf.Clamp(effectiveLookAheadDistance * 0.1f * Mathf.Max(0f, lookAheadMultiplier), 0f, 1f)
                : 0f);
            SetNestedMember(_positionComposer, "Lookahead", "Smoothing", Mathf.Clamp(_lookAheadSmoothTime, 0f, 30f));
            SetNestedMember(_positionComposer, "Lookahead", "IgnoreY", true);

            object composition = GetMember(_positionComposer, "Composition");
            if (composition != null)
            {
                object deadZone = GetMember(composition, "DeadZone");
                if (deadZone != null)
                {
                    bool deadZoneEnabled = mode != CameraMode.FullLock;
                    SetMember(deadZone, "Enabled", deadZoneEnabled);
                    SetMember(deadZone, "Size", deadZoneEnabled ? ResolveDeadZone(mode) : Vector2.zero);
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

            if (brainType != null)
            {
                _brain = EnsureComponent(_camera.gameObject, brainType);
                SetMember(_brain, "IgnoreTimeScale", true);
                ConfigureBrainUpdateMethod(_brain);
            }

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

            object impulseDefinition = GetMember(_impulseSource, "ImpulseDefinition");
            if (impulseDefinition != null && _shakeMaxDuration > 0f)
                SetMember(impulseDefinition, "ImpulseDuration", _shakeMaxDuration);

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

        private void InvalidateVirtualCameraState()
        {
            SetMember(_virtualCamera, "PreviousStateIsValid", false);
        }

        private void ResetBrainState()
        {
            if (_brain == null) return;
            InvokeMember(_brain, "ResetState");
        }

        private void SynchronizeOutputCamera()
        {
            if (_camera == null || _virtualCamera == null) return;

            object state = GetMember(_virtualCamera, "State");
            object rawPosition = GetMember(state, "RawPosition");
            object positionCorrection = GetMember(state, "PositionCorrection");
            object rawOrientation = GetMember(state, "RawOrientation");
            object orientationCorrection = GetMember(state, "OrientationCorrection");
            if (!(rawPosition is Vector3 position) || !(rawOrientation is Quaternion orientation)) return;

            if (positionCorrection is Vector3 correction)
                position += correction;
            if (orientationCorrection is Quaternion rotationCorrection)
                orientation *= rotationCorrection;

            _camera.transform.SetPositionAndRotation(position, orientation);
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
            if (_isPrimary != (primary == this))
                ResetStabilityTracking();
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

            CameraBounds preferredBounds = null;
            Scene activeScene = SceneManager.GetActiveScene();
            Scene targetScene = preferredScene.IsValid() && preferredScene.isLoaded
                ? preferredScene
                : activeScene;

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

                if (candidateScene == targetScene) preferredBounds ??= candidate;
            }

            CameraBounds selectedBounds = preferredBounds;
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

        private static void ConfigureBrainUpdateMethod(Component brain)
        {
            if (brain == null) return;

            Type updateMethodsType = brain.GetType().GetNestedType(
                "UpdateMethods", BindingFlags.Public | BindingFlags.NonPublic);
            if (updateMethodsType == null || !updateMethodsType.IsEnum) return;

            try
            {
                object lateUpdate = Enum.Parse(updateMethodsType, "LateUpdate");
                SetMember(brain, "UpdateMethod", lateUpdate);
            }
            catch (ArgumentException)
            {
                // Keep the scene-configured value if a Cinemachine version renames the enum.
            }
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
