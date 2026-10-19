using UnityEngine;

namespace Zephyr.Gameplay.Levels
{
    /// <summary>
    /// Aligns the object's bottom-left reference point to the containing Grid cell.
    /// Uses a Collider2D first, then a SpriteRenderer, and finally the root Transform
    /// position when the object has no geometry to provide a bounds reference.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class GridAligner : MonoBehaviour
    {
        [SerializeField] private Grid _grid;
        [SerializeField] private Collider2D _alignmentCollider;
        [SerializeField] private SpriteRenderer _alignmentRenderer;

        private void OnEnable()
        {
            AlignNow();
        }

        [ContextMenu("Align Now")]
        public void AlignNow()
        {
            Grid grid = ResolveGrid();
            if (grid == null)
            {
                if (Application.isPlaying)
                    Debug.LogWarning("GridAligner could not find a Grid component.", this);
                return;
            }

            Vector3 referencePoint = GetReferencePoint();
            Vector3 targetPoint = grid.CellToWorld(grid.WorldToCell(referencePoint));
            targetPoint.z = transform.position.z;

            Vector3 delta = targetPoint - referencePoint;
            delta.z = 0f;
            if (delta.sqrMagnitude <= 0.0000001f) return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.Undo.RecordObject(transform, "Align Object To Grid");
#endif

            transform.position += delta;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(transform);
                UnityEditor.EditorUtility.SetDirty(transform);
                UnityEditor.EditorUtility.SetDirty(gameObject);
                UnityEditor.EditorUtility.SetDirty(this);
            }
#endif
        }

        private Grid ResolveGrid()
        {
            if (_grid != null) return _grid;

            _grid = GetComponentInParent<Grid>();
            if (_grid != null) return _grid;

            return _grid;
        }

        private Vector3 GetReferencePoint()
        {
            Collider2D collider = ResolveCollider();
            if (collider != null) return collider.bounds.min;

            SpriteRenderer renderer = ResolveRenderer();
            if (renderer != null) return renderer.bounds.min;

            return transform.position;
        }

        private Collider2D ResolveCollider()
        {
            if (_alignmentCollider != null && _alignmentCollider.isActiveAndEnabled)
                return _alignmentCollider;

            Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
            Collider2D fallback = null;
            foreach (Collider2D collider in colliders)
            {
                if (collider == null || !collider.isActiveAndEnabled) continue;
                if (!collider.isTrigger) return collider;
                fallback ??= collider;
            }

            return fallback;
        }

        private SpriteRenderer ResolveRenderer()
        {
            if (_alignmentRenderer != null && _alignmentRenderer.enabled
                && _alignmentRenderer.gameObject.activeInHierarchy
                && _alignmentRenderer.sprite != null)
                return _alignmentRenderer;

            SpriteRenderer rootRenderer = GetComponent<SpriteRenderer>();
            if (rootRenderer != null && rootRenderer.enabled
                && rootRenderer.gameObject.activeInHierarchy && rootRenderer.sprite != null)
                return rootRenderer;

            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer != null && renderer.enabled
                    && renderer.gameObject.activeInHierarchy && renderer.sprite != null)
                    return renderer;
            }

            return null;
        }
    }
}
