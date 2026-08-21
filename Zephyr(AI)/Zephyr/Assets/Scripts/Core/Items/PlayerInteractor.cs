using UnityEngine;
using UnityEngine.InputSystem;
using Zephyr.Core.Interfaces;
using Zephyr.Core.UI.Dialogue;

namespace Zephyr.Core.Items
{
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private float _interactionRange = 1.25f;
        [SerializeField] private LayerMask _interactionLayers = ~0;
        [SerializeField] private string _keyboardBinding = "<Keyboard>/e";
        [SerializeField] private string _gamepadBinding = "<Gamepad>/buttonWest";
        private InputAction _interactAction;
        public IInteractable Nearest { get; private set; }
        public float InteractionRange => _interactionRange;

        private void Awake()
        {
            _interactAction = new InputAction("Interact", InputActionType.Button, _keyboardBinding);
            if (!string.IsNullOrWhiteSpace(_gamepadBinding)) _interactAction.AddBinding(_gamepadBinding);
        }
        private void OnEnable()
        {
            _interactAction.Enable();
            DialogueManager.GameplayInputSuspended += DisableInput;
            DialogueManager.GameplayInputRestored += EnableInput;
        }
        private void OnDisable()
        {
            DialogueManager.GameplayInputSuspended -= DisableInput;
            DialogueManager.GameplayInputRestored -= EnableInput;
            _interactAction.Disable();
        }
        private void OnDestroy() => _interactAction?.Dispose();
        private void Update()
        {
            Nearest = FindNearest();
            if (Nearest != null && _interactAction.WasPressedThisFrame()) Nearest.OnInteract();
        }
        private void DisableInput() => _interactAction?.Disable();
        private void EnableInput() { if (isActiveAndEnabled) _interactAction?.Enable(); }

        private IInteractable FindNearest()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _interactionRange, _interactionLayers);
            IInteractable nearest = null;
            float distance = float.PositiveInfinity;
            foreach (Collider2D hit in hits)
            {
                MonoBehaviour[] behaviours = hit.GetComponentsInParent<MonoBehaviour>(true);
                foreach (MonoBehaviour behaviour in behaviours)
                {
                    if (!(behaviour is IInteractable candidate) || !candidate.CanInteract()) continue;
                    float d = (hit.transform.position - transform.position).sqrMagnitude;
                    if (d < distance) { distance = d; nearest = candidate; }
                }
            }
            return nearest;
        }
    }
}
