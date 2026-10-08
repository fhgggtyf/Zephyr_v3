using UnityEngine;
using Zephyr.Core.Interfaces;

namespace Zephyr.Gameplay.Interaction
{
    /// <summary>
    /// Adds a white overlay when this interactable is the player's current target.
    /// The overlay mirrors the target SpriteRenderer every frame so replacing the
    /// sprite asset does not require changing this component or the prefab.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractableFocusVisual : MonoBehaviour, IInteractionFocusReceiver
    {
        [SerializeField] private SpriteRenderer _targetRenderer;
        [SerializeField] private Color _highlightColor = Color.white;
        [SerializeField, Min(0f)] private float _highlightScale = 1.04f;
        [SerializeField] private int _sortingOrderOffset = 1;

        private SpriteRenderer _highlightRenderer;
        private Color _normalColor;
        private bool _focused;

        private void Awake()
        {
            EnsureReferences();
            CaptureNormalState();
            ApplyFocusVisual();
        }

        private void OnEnable()
        {
            EnsureReferences();
            if (!_focused) CaptureNormalState();
            ApplyFocusVisual();
        }

        private void LateUpdate()
        {
            if (_targetRenderer == null || _highlightRenderer == null) return;
            SyncHighlightRenderer();
        }

        public void SetInteractionFocused(bool focused)
        {
            _focused = focused;
            EnsureReferences();
            ApplyFocusVisual();
        }

        private void EnsureReferences()
        {
            if (_targetRenderer == null) _targetRenderer = GetComponent<SpriteRenderer>();
            if (_targetRenderer == null) return;

            if (_highlightRenderer == null)
            {
                Transform highlight = transform.Find("InteractionHighlight");
                if (highlight == null)
                {
                    GameObject highlightObject = new GameObject("InteractionHighlight");
                    highlight = highlightObject.transform;
                    highlight.SetParent(transform, false);
                }

                _highlightRenderer = highlight.GetComponent<SpriteRenderer>();
                if (_highlightRenderer == null)
                    _highlightRenderer = highlight.gameObject.AddComponent<SpriteRenderer>();
            }

            SyncHighlightRenderer();
        }

        private void CaptureNormalState()
        {
            if (_targetRenderer != null) _normalColor = _targetRenderer.color;
        }

        private void ApplyFocusVisual()
        {
            if (_targetRenderer == null || _highlightRenderer == null) return;

            _targetRenderer.color = _focused ? Color.white : _normalColor;
            _highlightRenderer.enabled = _focused;
            if (_focused)
            {
                _highlightRenderer.color = _highlightColor;
                _highlightRenderer.transform.localScale = Vector3.one * _highlightScale;
            }
            else
            {
                _highlightRenderer.transform.localScale = Vector3.one;
            }
        }

        private void SyncHighlightRenderer()
        {
            if (_targetRenderer == null || _highlightRenderer == null) return;

            _highlightRenderer.sprite = _targetRenderer.sprite;
            _highlightRenderer.sharedMaterial = _targetRenderer.sharedMaterial;
            _highlightRenderer.sortingLayerID = _targetRenderer.sortingLayerID;
            _highlightRenderer.sortingOrder = _targetRenderer.sortingOrder + _sortingOrderOffset;
            _highlightRenderer.flipX = _targetRenderer.flipX;
            _highlightRenderer.flipY = _targetRenderer.flipY;
            _highlightRenderer.drawMode = _targetRenderer.drawMode;
            _highlightRenderer.size = _targetRenderer.size;
            _highlightRenderer.enabled = _focused && _targetRenderer.sprite != null;
        }
    }
}