using UnityEngine;
using Zephyr.Core.Interfaces;

namespace Zephyr.Gameplay.Interaction
{
    /// <summary>
    /// Toggleable two-tile door. Visuals are intentionally prefab-driven; this
    /// component only owns state and the blocking collider.
    /// </summary>
    public sealed class DoorInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private Collider2D _blockingCollider;
        [SerializeField] private SpriteRenderer _closedRenderer;
        [SerializeField] private SpriteRenderer _openRenderer;
        [SerializeField] private Transform _openVisual;
        [SerializeField] private Vector3 _openOffset = new Vector3(0f, 2f, 0f);
        [SerializeField] private bool _startsOpen;
        [SerializeField] private float _heightInTiles = 2f;
        private Vector3 _closedVisualPosition;
        private bool _isOpen;

        public bool IsOpen => _isOpen;
        public float HeightInTiles => _heightInTiles;

        private void Awake()
        {
            if (_blockingCollider == null) _blockingCollider = GetComponent<Collider2D>();
            if (_openVisual != null) _closedVisualPosition = _openVisual.localPosition;
            SetOpen(_startsOpen);
        }

        public bool CanInteract() => isActiveAndEnabled;
        public void OnInteract() => SetOpen(!_isOpen);

        public void SetOpen(bool open)
        {
            _isOpen = open;
            if (_blockingCollider != null) _blockingCollider.enabled = !_isOpen;
            if (_closedRenderer != null) _closedRenderer.enabled = !_isOpen;
            if (_openRenderer != null) _openRenderer.enabled = _isOpen;
            if (_openVisual != null)
                _openVisual.localPosition = _isOpen ? _closedVisualPosition + _openOffset : _closedVisualPosition;
        }
    }
}
