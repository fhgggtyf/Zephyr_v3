using System;
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

    /// <summary>Scene-owned camera bounds applied to the shared gameplay camera.</summary>
    public sealed class CameraBounds : MonoBehaviour
    {
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
        [Header("Default Behavior")]
        [SerializeField] private CameraMode _defaultMode = CameraMode.Follow;
        [SerializeField] private Vector2 _defaultFollowOffset;
        [SerializeField, Min(0f)] private float _defaultLookAheadMultiplier = 1f;
        [SerializeField] private Vector2 _defaultAnchor;
        [Header("Local Override Regions")]
        [SerializeField] private CameraRegion[] _regions = Array.Empty<CameraRegion>();

        public Rect Bounds => OffsetRect(_bounds, transform.position);

        private void Start() => ApplyBounds();

        private void OnDisable()
        {
            CameraController camera = CameraController.FindForGameplay(gameObject.scene);
            camera?.ClearRoomBounds(this);
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
