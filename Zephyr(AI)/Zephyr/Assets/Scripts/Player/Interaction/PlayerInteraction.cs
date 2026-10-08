using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Zephyr.Core.Interfaces;
using Zephyr.Core.UI.Dialogue;

namespace Zephyr.Gameplay.Player.Interaction
{
    /// <summary>
    /// Player interaction detector. The detector is an independent trigger child
    /// and never reuses the player's physical collider.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-150)]
    public class PlayerInteraction : MonoBehaviour
    {
        [SerializeField] private CircleCollider2D _interactionTrigger;
        [SerializeField, Min(0.05f)] private float _interactionRadius = 1.25f;
        [SerializeField] private LayerMask _interactionLayers = ~0;

        private readonly Dictionary<IInteractable, HashSet<Collider2D>> _candidateColliders =
            new Dictionary<IInteractable, HashSet<Collider2D>>();
        private ZephyrInputActions _actions;
        private IInteractable _currentTarget;
        private IInteractionFocusReceiver _focusedReceiver;

        public IInteractable CurrentTarget => _currentTarget;
        public float InteractionRadius => _interactionRadius;
        public event Action<IInteractable> TargetChanged;

        private void Awake()
        {
            EnsureTrigger();
            _actions = new ZephyrInputActions();
        }

        private void OnEnable()
        {
            if (_actions == null) _actions = new ZephyrInputActions();
            _actions.GamePlay.Interact.started += HandleInteract;
            DialogueManager.GameplayInputSuspended += DisableInput;
            DialogueManager.GameplayInputRestored += EnableInput;

            if (DialogueManager.Instance != null && DialogueManager.Instance.IsOpen)
                DisableInput();
            else
                EnableInput();
        }

        private void OnDisable()
        {
            if (_actions != null) _actions.GamePlay.Interact.started -= HandleInteract;
            DialogueManager.GameplayInputSuspended -= DisableInput;
            DialogueManager.GameplayInputRestored -= EnableInput;
            _actions?.GamePlay.Disable();
            ClearFocus();
            _candidateColliders.Clear();
            _currentTarget = null;
        }

        private void OnDestroy() => _actions?.Dispose();

        private void Update()
        {
            SetCurrentTarget(FindNearestCandidate());
        }

        private void HandleInteract(InputAction.CallbackContext context)
        {
            if (!context.started || _currentTarget == null || !_currentTarget.CanInteract()
                || !IsWithinInteractionRadius(_currentTarget)) return;
            _currentTarget.OnInteract();
        }

        private void EnsureTrigger()
        {
            if (_interactionTrigger == null)
            {
                Transform detector = transform.Find("InteractionDetector");
                if (detector == null)
                {
                    var detectorObject = new GameObject("InteractionDetector");
                    detector = detectorObject.transform;
                    detector.SetParent(transform, false);
                }

                _interactionTrigger = detector.GetComponent<CircleCollider2D>();
                if (_interactionTrigger == null)
                    _interactionTrigger = detector.gameObject.AddComponent<CircleCollider2D>();
            }

            _interactionTrigger.isTrigger = true;
            _interactionTrigger.radius = _interactionRadius;
            _interactionTrigger.includeLayers = _interactionLayers;
        }

        private void OnTriggerEnter2D(Collider2D other) => AddCandidates(other);
        private void OnTriggerStay2D(Collider2D other) => AddCandidates(other);

        private void OnTriggerExit2D(Collider2D other)
        {
            foreach (MonoBehaviour behaviour in other.GetComponentsInParent<MonoBehaviour>(true))
            {
                if (!(behaviour is IInteractable interactable)) continue;
                if (!_candidateColliders.TryGetValue(interactable, out HashSet<Collider2D> colliders)) continue;

                colliders.Remove(other);
                if (colliders.Count == 0)
                    _candidateColliders.Remove(interactable);
            }
        }

        private void AddCandidates(Collider2D other)
        {
            if (other == null || other == _interactionTrigger) return;

            foreach (MonoBehaviour behaviour in other.GetComponentsInParent<MonoBehaviour>(true))
            {
                if (!(behaviour is IInteractable interactable)) continue;
                if (!_candidateColliders.TryGetValue(interactable, out HashSet<Collider2D> colliders))
                {
                    colliders = new HashSet<Collider2D>();
                    _candidateColliders.Add(interactable, colliders);
                }

                // OnTriggerStay2D is intentionally idempotent: one collider is
                // registered once, regardless of how many frames it remains inside.
                colliders.Add(other);
            }
        }

        private IInteractable FindNearestCandidate()
        {
            IInteractable nearest = null;
            float nearestDistance = float.PositiveInfinity;

            foreach (IInteractable candidate in _candidateColliders.Keys)
            {
                var behaviour = candidate as MonoBehaviour;
                if (candidate == null || behaviour == null || !behaviour.isActiveAndEnabled
                    || !candidate.CanInteract() || !IsWithinInteractionRadius(candidate)) continue;
                float distance = (behaviour.transform.position - transform.position).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = candidate;
                }
            }

            return nearest;
        }

        private bool IsWithinInteractionRadius(IInteractable candidate)
        {
            if (candidate == null) return false;
            float maxDistanceSqr = _interactionRadius * _interactionRadius;
            if (_candidateColliders.TryGetValue(candidate, out HashSet<Collider2D> colliders))
            {
                foreach (Collider2D collider in colliders)
                {
                    if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy) continue;
                    Vector2 closestPoint = collider.ClosestPoint(transform.position);
                    if (((Vector2)transform.position - closestPoint).sqrMagnitude <= maxDistanceSqr)
                        return true;
                }
            }

            var behaviour = candidate as MonoBehaviour;
            return behaviour != null && behaviour.isActiveAndEnabled
                && ((Vector2)behaviour.transform.position - (Vector2)transform.position).sqrMagnitude <= maxDistanceSqr;
        }

        private void SetCurrentTarget(IInteractable target)
        {
            if (ReferenceEquals(_currentTarget, target)) return;
            ClearFocus();
            _currentTarget = target;
            MonoBehaviour targetBehaviour = target as MonoBehaviour;
            _focusedReceiver = targetBehaviour != null
                ? targetBehaviour.GetComponent<IInteractionFocusReceiver>()
                : null;
            _focusedReceiver?.SetInteractionFocused(true);
            TargetChanged?.Invoke(_currentTarget);
        }

        private void ClearFocus()
        {
            _focusedReceiver?.SetInteractionFocused(false);
            _focusedReceiver = null;
        }

        private void DisableInput() => _actions?.GamePlay.Disable();

        private void EnableInput()
        {
            if (isActiveAndEnabled) _actions?.GamePlay.Enable();
        }
    }
}
