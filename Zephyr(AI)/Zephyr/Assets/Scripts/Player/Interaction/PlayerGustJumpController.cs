using UnityEngine;
using Zephyr.Gameplay.Player.Core;

namespace Zephyr.Gameplay.Player.Interaction
{
    /// <summary>
    /// Runtime owner for gust traversal. The state-machine actions call this
    /// component; the interactable only submits a destination request.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerGustJumpController : MonoBehaviour
    {
        [SerializeField, Min(0.05f)] private float _defaultDuration = 0.8f;
        [SerializeField, Min(0.1f)] private float _easeOutPower = 2.4f;

        private MovementCore _movement;
        private Rigidbody2D _body;
        private Transform _requestedDestination;
        private Vector2 _startPosition;
        private Vector2 _destinationPosition;
        private float _startedAt;
        private float _duration;
        private float _previousGravity;
        private bool _ownsGravity;
        private bool _active;
        private bool _complete;

        public bool HasRequest => _requestedDestination != null && !_active;
        public bool IsActive => _active;
        public bool IsComplete => _complete;

        private void Awake()
        {
            _movement = GetComponentInChildren<MovementCore>(true);
            _body = GetComponent<Rigidbody2D>();
        }

        public void RequestGustJump(Transform destination)
        {
            if (destination == null || _active) return;
            _requestedDestination = destination;
            _complete = false;
        }

        public bool BeginGustJump(float duration, float easeOutPower)
        {
            if (!HasRequest) return false;
            _startPosition = transform.position;
            _destinationPosition = _requestedDestination.position;
            _requestedDestination = null;
            _startedAt = Time.time;
            _duration = Mathf.Max(0.05f, duration > 0f ? duration : _defaultDuration);
            _easeOutPower = Mathf.Max(0.1f, easeOutPower > 0f ? easeOutPower : _easeOutPower);
            _active = true;
            _complete = false;

            if (_movement != null)
            {
                _movement.enabled = false;
                _movement.SetMoveInput(Vector2.zero);
                _movement.SetMoveSpeed(0f);
            }

            if (_body != null)
            {
                _previousGravity = _body.gravityScale;
                _body.gravityScale = 0f;
                _body.linearVelocity = Vector2.zero;
                _ownsGravity = true;
            }
            return true;
        }

        public void TickGustJump()
        {
            if (!_active) return;
            float t = Mathf.Clamp01((Time.time - _startedAt) / _duration);
            float eased = 1f - Mathf.Pow(1f - t, _easeOutPower);
            Vector2 position = Vector2.LerpUnclamped(_startPosition, _destinationPosition, eased);
            if (_body != null) _body.position = position;
            else transform.position = position;

            if (t >= 1f)
            {
                if (_body != null) _body.position = _destinationPosition;
                else transform.position = _destinationPosition;
                _active = false;
                _complete = true;
            }
        }

        public void EndGustJump()
        {
            _active = false;
            if (_body != null && _ownsGravity)
            {
                _body.gravityScale = _previousGravity;
                _body.linearVelocity = Vector2.zero;
                _ownsGravity = false;
            }
            if (_movement != null) _movement.enabled = true;
        }

        public void ClearCompletion() => _complete = false;
    }
}
