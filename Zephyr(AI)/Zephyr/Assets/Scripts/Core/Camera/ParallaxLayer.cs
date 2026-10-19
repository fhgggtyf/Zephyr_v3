using UnityEngine;

namespace Zephyr.Core.Camera
{
    [DisallowMultipleComponent]
    public sealed class ParallaxLayer : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f), Tooltip("1 keeps the layer still on screen; 0 keeps it fixed in world space.")]
        private float _cameraFollowRatio = 0.5f;
        [SerializeField] private bool _loopHorizontally = true;
        [SerializeField, Min(0f), Tooltip("World-space gap after the last sprite before the pattern repeats.")]
        private float _repeatGap;

        private SpriteRenderer[] _sprites;
        private Vector3[] _authoredSpritePositions;
        private Vector3 _authoredLocalPosition;
        private float _repeatWidth;
        private bool _layoutCaptured;

        private void Awake() => CaptureAuthoredLayout();

        private void OnValidate()
        {
            _cameraFollowRatio = Mathf.Clamp01(_cameraFollowRatio);
            _repeatGap = Mathf.Max(0f, _repeatGap);
        }

        internal void ResetToAuthoredLayout()
        {
            CaptureAuthoredLayout();
            transform.localPosition = _authoredLocalPosition;

            for (int i = 0; i < _sprites.Length; i++)
            {
                if (_sprites[i] != null)
                    _sprites[i].transform.localPosition = _authoredSpritePositions[i];
            }

            _repeatWidth = CalculateRepeatWidth();
        }

internal void ApplyCameraDelta(Vector3 cameraDelta, UnityEngine.Camera outputCamera)
        {
            if (!isActiveAndEnabled || cameraDelta.sqrMagnitude <= 0.00000001f) return;
            CaptureAuthoredLayout();

            float layerDeltaX = cameraDelta.x * (_cameraFollowRatio - 1f);
            Vector3 position = transform.position;
            position.x += layerDeltaX;
            position.y += cameraDelta.y * (_cameraFollowRatio - 1f);
            transform.position = position;

            if (_loopHorizontally && Mathf.Abs(layerDeltaX) > 0.0001f)
                WrapSprites(outputCamera, layerDeltaX);
        }

        private void CaptureAuthoredLayout()
        {
            if (_layoutCaptured) return;

            _authoredLocalPosition = transform.localPosition;
            _sprites = GetComponentsInChildren<SpriteRenderer>(true);
            _authoredSpritePositions = new Vector3[_sprites.Length];
            for (int i = 0; i < _sprites.Length; i++)
                _authoredSpritePositions[i] = _sprites[i] != null ? _sprites[i].transform.localPosition : Vector3.zero;

            _repeatWidth = CalculateRepeatWidth();
            _layoutCaptured = true;
        }

        private float CalculateRepeatWidth()
        {
            if (_sprites == null || _sprites.Length == 0) return 0f;

            bool foundSprite = false;
            float left = float.PositiveInfinity;
            float right = float.NegativeInfinity;
            foreach (SpriteRenderer sprite in _sprites)
            {
                if (sprite == null || sprite.sprite == null) continue;
                Bounds bounds = sprite.bounds;
                left = Mathf.Min(left, bounds.min.x);
                right = Mathf.Max(right, bounds.max.x);
                foundSprite = true;
            }

            return foundSprite ? Mathf.Max(0f, right - left + _repeatGap) : 0f;
        }

private void WrapSprites(UnityEngine.Camera outputCamera, float layerDeltaX)
        {
            if (_repeatWidth <= 0f || outputCamera == null || !outputCamera.orthographic) return;

            float halfViewWidth = outputCamera.orthographicSize * outputCamera.aspect;
            float leftEdge = outputCamera.transform.position.x - halfViewWidth;
            float rightEdge = outputCamera.transform.position.x + halfViewWidth;

            foreach (SpriteRenderer sprite in _sprites)
            {
                if (sprite == null || !sprite.enabled || !sprite.gameObject.activeInHierarchy) continue;

                Bounds bounds = sprite.bounds;
                if (layerDeltaX < 0f && bounds.max.x < leftEdge)
                {
                    sprite.transform.position += Vector3.right * _repeatWidth;
                }
                else if (layerDeltaX > 0f && bounds.min.x > rightEdge)
                {
                    sprite.transform.position += Vector3.left * _repeatWidth;
                }
            }
        }
    }
}
