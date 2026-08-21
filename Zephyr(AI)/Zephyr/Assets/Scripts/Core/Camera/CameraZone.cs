using System.Collections.Generic;
using UnityEngine;

namespace Zephyr.Core.Camera
{
    /// <summary>Camera behavior used while the player is inside a room region.</summary>
    public enum CameraMode
    {
        Follow,
        VerticalLock,
        HorizontalLock,
        VerticalCenter,
        FullLock
    }

    /// <summary>
    /// Scene-owned camera zone. Its transform is the center of the zone and Size
    /// is expressed in world units, so a level designer can position and resize
    /// it directly in the scene without touching the persistent camera.
    /// </summary>
    [ExecuteAlways]
    public sealed class CameraZone : MonoBehaviour
    {
        private static readonly List<CameraZone> Zones = new List<CameraZone>();

        [SerializeField] private Vector2 _size = new Vector2(20f, 12f);
        [SerializeField] private CameraMode _mode = CameraMode.Follow;
        [SerializeField] private Vector2 _followOffset;
        [SerializeField, Min(0f)] private float _lookAheadMultiplier = 1f;
        [SerializeField] private Vector2 _anchor;
        [SerializeField] private int _priority;

        public Vector2 Size => _size;
        public CameraMode Mode => _mode;
        public Vector2 FollowOffset => _followOffset;
        public float LookAheadMultiplier => _lookAheadMultiplier;
        public Vector2 Anchor => (Vector2)transform.position + _anchor;
        public int Priority => _priority;
        public Rect WorldBounds => new Rect((Vector2)transform.position - _size * 0.5f, _size);

        private void OnEnable()
        {
            if (!Zones.Contains(this)) Zones.Add(this);
        }

        private void OnDisable() => Zones.Remove(this);

        public static CameraZone FindActive(Vector2 worldPosition)
        {
            CameraZone result = null;
            int bestPriority = int.MinValue;
            for (int i = Zones.Count - 1; i >= 0; i--)
            {
                CameraZone zone = Zones[i];
                if (zone == null)
                {
                    Zones.RemoveAt(i);
                    continue;
                }

                if (!zone.isActiveAndEnabled || !zone.WorldBounds.Contains(worldPosition)) continue;
                int priority = zone.Priority;
                if (result == null || priority > bestPriority)
                {
                    result = zone;
                    bestPriority = priority;
                }
            }

            return result;
        }

        private void OnDrawGizmosSelected()
        {
            Rect bounds = WorldBounds;
            Gizmos.color = _mode == CameraMode.FullLock ? new Color(1f, 0.35f, 0.2f, 0.8f) : new Color(0.2f, 0.8f, 1f, 0.8f);
            Gizmos.DrawWireCube(bounds.center, bounds.size);
            if (_mode == CameraMode.FullLock)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(Anchor, 0.15f);
            }
        }
    }
}
