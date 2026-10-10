using System;
using UnityEngine;

namespace Zephyr.Gameplay.Enemies
{
    public enum BossSpearState
    {
        Held,
        Flying,
        Landed
    }

    /// <summary>
    /// Owns the spear's presentation and transform lifecycle. BossController
    /// only commands the throw and reacts to the landing event; it does not
    /// manipulate parentage, local pose, or flight interpolation directly.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BossSpearController : MonoBehaviour
    {
        
        [SerializeField] private EnemyMotor _motor;
[SerializeField] private Transform _socket;
        [SerializeField, Min(0f)] private float _defaultArcHeight = 1.25f;

        private BossSpearState _state = BossSpearState.Held;
        private Vector3 _heldLocalPosition;
        private Quaternion _heldLocalRotation;
        private Vector3 _heldLocalScale;
        private Vector2 _flightStart;
        private Vector2 _flightTarget;
        private float _flightStartedAt;
        private float _flightDuration;
        
        private Vector3 _socketBaseLocalPosition;
private float _flightArcHeight;

        public event Action Landed;

        public BossSpearState State => _state;
        public bool IsHeld => _state == BossSpearState.Held;
        public bool IsFlying => _state == BossSpearState.Flying;
        public bool IsLanded => _state == BossSpearState.Landed;
        public Vector2 WorldPosition => transform.position;

        private void Awake()
        {
            _socket ??= transform.parent;
            _motor ??= GetComponentInParent<EnemyMotor>();
            if (_socket != null) _socketBaseLocalPosition = _socket.localPosition;
            CacheHeldPose();
            ReturnToSocket();
        }

        private void Update()
        {
            if (_state != BossSpearState.Flying) return;

            float t = _flightDuration <= 0f
                ? 1f
                : Mathf.Clamp01((Time.time - _flightStartedAt) / _flightDuration);
            Vector2 position = Vector2.Lerp(_flightStart, _flightTarget, t);
            position.y += Mathf.Sin(t * Mathf.PI) * _flightArcHeight;
            transform.position = position;

            if (t >= 1f) Land();
        }

        public void ThrowTo(Vector2 target, float duration, float arcHeight = -1f)
        {
            if (_socket == null) _socket = transform.parent;
            if (_socket == null)
            {
                Debug.LogError("BossSpearController requires a socket Transform.", this);
                return;
            }

            CacheHeldPose();
            _flightStart = transform.position;
            _flightTarget = target;
            _flightStartedAt = Time.time;
            _flightDuration = Mathf.Max(0.01f, duration);
            _flightArcHeight = Mathf.Max(0f, arcHeight < 0f ? _defaultArcHeight : arcHeight);
            transform.SetParent(null, true);
            gameObject.SetActive(true);
            _state = BossSpearState.Flying;
        }

        public void ReturnToSocket()
        {
            if (_socket == null) _socket = transform.parent;
            if (_socket == null) return;

            transform.SetParent(_socket, false);
            transform.localPosition = _heldLocalPosition;
            transform.localRotation = _heldLocalRotation;
            transform.localScale = _heldLocalScale;
            gameObject.SetActive(true);
            _state = BossSpearState.Held;
        }

        public void Land()
        {
            if (_state != BossSpearState.Flying) return;
            transform.position = _flightTarget;
            _state = BossSpearState.Landed;
            Landed?.Invoke();
        }

        private void CacheHeldPose()
        {
            if (_socket == null) return;
            if (transform.parent != _socket) return;
            _heldLocalPosition = transform.localPosition;
            _heldLocalRotation = transform.localRotation;
            _heldLocalScale = transform.localScale;
        }
    

private void LateUpdate()
        {
            if (_socket == null || _motor == null || Mathf.Abs(_socketBaseLocalPosition.x) <= 0.001f) return;
            Vector3 position = _socketBaseLocalPosition;
            if (_motor.Facing.x > 0f) position.x = -_socketBaseLocalPosition.x;
            _socket.localPosition = position;
        }
}
}
