/*
 * PlayerColliderController.cs
 * Module: Player / Core
 * Owns enabled-state switching only; collider geometry stays authored in the prefab.
 */
using UnityEngine;

namespace Zephyr.Gameplay.Player.Core
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-200)]
    public sealed class PlayerColliderController : MonoBehaviour
    {
        [Header("Collider Profiles")]
        [SerializeField] private BoxCollider2D _standingCollider;
        [SerializeField] private BoxCollider2D _crouchingCollider;

        [Header("Standing Clearance")]
        [Tooltip("Match GroundChecker's ground/wall layers.")]
        [SerializeField] private LayerMask _obstructionLayers;

        // Query tolerance only, NOT a change to either collider. The authored
        // standing sole is about 0.00472 units below the crouching sole. Exclude
        // floor contact/solver slop so a grounded player can still stand up.
        private const float k_footTolerance = 0.02f;
        private const float k_sideTolerance = 0.001f;
        private readonly Collider2D[] _overlapResults = new Collider2D[16];
        private ContactFilter2D _contactFilter;
        private Rigidbody2D _rigidbody;
        private bool _desiredCrouched;
        private bool _configured;
        private bool _configurationFailureLogged;

        public Collider2D ActiveCollider => IsCrouchingProfileActive
            ? _crouchingCollider : _standingCollider;
        public bool IsCrouchingProfileActive => _crouchingCollider != null && _crouchingCollider.enabled;
        public bool MustRemainCrouched => IsCrouchingProfileActive && !CanStandUp();

        private void Awake()
        {
            if (!EnsureConfigured())
            {
                enabled = false;
                return;
            }

            // Preserve an authored crouch start; do not expand into a ceiling.
            _desiredCrouched = IsCrouchingProfileActive;
            ApplyProfile(_desiredCrouched);
        }

        private void FixedUpdate()
        {
            // Forced impact exits may request standing in a tunnel. Retry
            // before GroundChecker and MovementCore run on the next physics step.
            if (!_desiredCrouched && IsCrouchingProfileActive && CanStandUp())
                ApplyProfile(false);
        }

        public void RequestCrouch(bool crouched)
        {
            if (!EnsureConfigured()) return;
            _desiredCrouched = crouched;
            if (crouched || CanStandUp()) ApplyProfile(crouched);
        }

        // Conditions must check this BEFORE consuming jump/roll input.
        public bool CanUseStandingProfile() => EnsureConfigured()
            && (_standingCollider.enabled || CanStandUp());

        public bool CanStandUp()
        {
            if (!EnsureConfigured()) return false;
            if (_standingCollider.enabled) return true;

            // Disabled Collider2D.bounds is empty. Derive the query from authored
            // size/offset and transform instead. Preserve the full head height.
            var boxTransform = _standingCollider.transform;
            var scale = boxTransform.lossyScale;
            var size = new Vector2(Mathf.Abs(_standingCollider.size.x * scale.x),
                Mathf.Abs(_standingCollider.size.y * scale.y));
            var footInset = Mathf.Min(k_footTolerance, size.y * 0.1f);
            Vector2 center = boxTransform.TransformPoint(_standingCollider.offset);
            Vector2 up = boxTransform.up * Mathf.Sign(scale.y);
            center += up * (footInset * 0.5f);
            size.y -= footInset;
            size.x -= Mathf.Min(2f * k_sideTolerance, size.x * 0.1f);
            if (size.x <= 0f || size.y <= 0f) return false;

            var count = Physics2D.OverlapBox(center, size, boxTransform.eulerAngles.z,
                _contactFilter, _overlapResults);
            for (var i = 0; i < count; i++)
            {
                var hit = _overlapResults[i];
                if (hit != null && !hit.isTrigger && hit.attachedRigidbody != _rigidbody)
                    return false;
            }
            // Fail closed if results were truncated, even if the retained hits
            // were all self-colliders. Never silently miss a ceiling.
            return count < _overlapResults.Length;
        }

        private bool EnsureConfigured()
        {
            if (_configured) return true;

            _rigidbody ??= GetComponentInParent<Rigidbody2D>();
            _configured = _standingCollider != null && _crouchingCollider != null
                && _standingCollider != _crouchingCollider && _rigidbody != null
                && _standingCollider.attachedRigidbody == _rigidbody
                && _crouchingCollider.attachedRigidbody == _rigidbody
                && !_standingCollider.isTrigger && !_crouchingCollider.isTrigger
                && _obstructionLayers.value != 0;

            if (!_configured)
            {
                if (!_configurationFailureLogged)
                {
                    Debug.LogError("[PlayerColliderController] Assign two distinct body boxes, their Rigidbody2D and obstruction layers.", this);
                    _configurationFailureLogged = true;
                }
                return false;
            }

            _contactFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = _obstructionLayers,
                useTriggers = false
            };
            return true;
        }

        private void ApplyProfile(bool crouched)
        {
            // Keep these exact authored shapes. No resizing, offset adjustment,
            // Transform scaling or Rigidbody teleport is used when switching.
            if (crouched)
            {
                _standingCollider.enabled = false;
                _crouchingCollider.enabled = true;
            }
            else
            {
                _crouchingCollider.enabled = false;
                _standingCollider.enabled = true;
            }
        }
    }
}
