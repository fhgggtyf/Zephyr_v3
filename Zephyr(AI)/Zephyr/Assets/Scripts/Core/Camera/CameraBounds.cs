using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zephyr.Core.Camera
{
    public readonly struct CameraBehavior
    {
        public CameraBehavior(int regionIndex, CameraMode mode, Vector2 followOffset, float lookAheadMultiplier, Vector2 anchor)
        {
            RegionIndex = regionIndex;
            Mode = mode;
            FollowOffset = followOffset;
            LookAheadMultiplier = lookAheadMultiplier;
            Anchor = anchor;
        }

        public int RegionIndex { get; }
        public CameraMode Mode { get; }
        public Vector2 FollowOffset { get; }
        public float LookAheadMultiplier { get; }
        public Vector2 Anchor { get; }
    }

    /// <summary>
    /// Scene-owned camera bounds. Rect authoring remains backward compatible;
    /// a child PolygonCollider2D is supplied to CinemachineConfiner2D at runtime.
    /// </summary>
    public sealed class CameraBounds : MonoBehaviour
    {
        internal static readonly List<CameraBounds> Active = new List<CameraBounds>();
        [Serializable]
        private sealed class CameraRegion
        {
            [SerializeField] private string _name = "Camera Region";
            [SerializeField] private Rect _localBounds = new Rect(-5f, -3f, 10f, 6f);
            [SerializeField] private CameraMode _mode = CameraMode.Follow;
            [SerializeField] private Vector2 _followOffset;
            [SerializeField, Min(0f)] private float _lookAheadMultiplier = 1f;
            [SerializeField] private Vector2 _anchor;
            [SerializeField] private int _priority;

            public string Name => _name;
            public int Priority => _priority;
            public Rect GetWorldBounds(Vector2 origin) => OffsetRect(_localBounds, origin);

            public CameraBehavior GetBehavior(int id, Vector2 origin)
            {
                return new CameraBehavior(id, _mode, _followOffset, _lookAheadMultiplier, origin + _anchor);
            }
        }

        [SerializeField] private Rect _bounds = new Rect(-12f, -6f, 24f, 12f);
        [SerializeField] private PolygonCollider2D _boundsShape;
        [Tooltip("Keep the polygon synchronized with Bounds. Disable to author a custom polygon instead.")]
        [SerializeField] private bool _createRuntimeShape = true;
        private Rect _lastShapeBounds;
        private Matrix4x4 _lastShapeMatrix;
        private Vector2 _lastShapeOffset;
        private bool _shapeSynchronized;
        public int ShapeVersion { get; private set; }
        [Header("Default Behavior")]
        [SerializeField] private CameraMode _defaultMode = CameraMode.Follow;
        [SerializeField] private Vector2 _defaultFollowOffset;
        [SerializeField, Min(0f)] private float _defaultLookAheadMultiplier = 1f;
        [SerializeField] private Vector2 _defaultAnchor;
        [Header("Local Override Regions")]
        [SerializeField] private CameraRegion[] _regions = Array.Empty<CameraRegion>();

        public Rect Bounds => OffsetRect(_bounds, transform.position);

        private void OnEnable()
        {
            if (!Active.Contains(this)) Active.Add(this);
            ApplyBounds();
        }

        private void OnDisable()
        {
            Active.Remove(this);
            CameraController camera = CameraController.FindForGameplay(gameObject.scene);
            camera?.ClearRoomBounds(this);
        }

        public PolygonCollider2D GetOrCreateBoundsShape()
        {
            if (_boundsShape != null)
            {
                _boundsShape.isTrigger = true;
                _boundsShape.enabled = true;
                SynchronizeRectShape();
                return _boundsShape;
            }

            if (!_createRuntimeShape) return null;
            Transform shapeTransform = transform.Find("CameraBoundsShape");
            if (shapeTransform == null)
            {
                GameObject shapeObject = new GameObject("CameraBoundsShape");
                shapeTransform = shapeObject.transform;
                shapeTransform.SetParent(transform, false);
            }

            _boundsShape = shapeTransform.GetComponent<PolygonCollider2D>();
            if (_boundsShape == null) _boundsShape = shapeTransform.gameObject.AddComponent<PolygonCollider2D>();
            _boundsShape.isTrigger = true;
            SynchronizeRectShape();
            return _boundsShape;
        }

        private void SynchronizeRectShape()
        {
            if (!_createRuntimeShape || _boundsShape == null) return;
            Rect worldBounds = Bounds;
            Matrix4x4 matrix = _boundsShape.transform.worldToLocalMatrix;
            Vector2 offset = _boundsShape.offset;
            if (_shapeSynchronized && _lastShapeBounds == worldBounds &&
                _lastShapeMatrix == matrix && _lastShapeOffset == offset) return;

            _boundsShape.pathCount = 1;
            _boundsShape.SetPath(0, new[]
            {
                (Vector2)matrix.MultiplyPoint3x4(new Vector3(worldBounds.xMin, worldBounds.yMin)) - offset,
                (Vector2)matrix.MultiplyPoint3x4(new Vector3(worldBounds.xMin, worldBounds.yMax)) - offset,
                (Vector2)matrix.MultiplyPoint3x4(new Vector3(worldBounds.xMax, worldBounds.yMax)) - offset,
                (Vector2)matrix.MultiplyPoint3x4(new Vector3(worldBounds.xMax, worldBounds.yMin)) - offset
            });
            _lastShapeBounds = worldBounds;
            _lastShapeMatrix = matrix;
            _lastShapeOffset = offset;
            _shapeSynchronized = true;
            ShapeVersion++;
        }

        private void ApplyBounds()
        {
            CameraController camera = CameraController.FindForGameplay(gameObject.scene);
            camera?.SetRoomBounds(this);
        }

        public CameraBehavior GetBehavior(Vector2 worldPosition)
        {
            Vector2 origin = transform.position;
            CameraRegion selected = null;
            int selectedIndex = -1;
            for (int i = 0; _regions != null && i < _regions.Length; i++)
            {
                CameraRegion region = _regions[i];
                if (region == null || !region.GetWorldBounds(origin).Contains(worldPosition)) continue;
                if (selected != null && region.Priority <= selected.Priority) continue;
                selected = region;
                selectedIndex = i;
            }

            if (selected != null)
                return selected.GetBehavior(selectedIndex, origin);

            return new CameraBehavior(-1, _defaultMode, _defaultFollowOffset,
                _defaultLookAheadMultiplier, origin + _defaultAnchor);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
            Rect room = Bounds;
            Gizmos.DrawWireCube(room.center, room.size);

            Vector2 origin = transform.position;
            if (_regions == null) return;
            for (int i = 0; i < _regions.Length; i++)
            {
                CameraRegion region = _regions[i];
                if (region == null) continue;
                Rect bounds = region.GetWorldBounds(origin);
                Gizmos.color = new Color(1f, 0.75f, 0.15f, 0.9f);
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }
        }

        private static Rect OffsetRect(Rect source, Vector2 offset)
        {
            return new Rect(source.position + offset, source.size);
        }
    }
}
