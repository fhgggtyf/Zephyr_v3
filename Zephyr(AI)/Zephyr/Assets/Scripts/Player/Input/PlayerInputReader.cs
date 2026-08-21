/*
 * PlayerInputReader.cs
 * --------------------
 * Module:  Player / Input
 * Purpose: Bridges the ZephyrInputActions (auto-generated from InputAction asset)
 *          to the state machine-consumable public API. Implements IGamePlayActions
 *          to receive Move/Sprint callbacks and exposes them as public properties.
 *          StateActions and StateConditions read MoveInput / HasHorizontalInput.
 *          Owns the ZephyrInputActions lifecycle (Enable/Disable/Dispose).
 * Dependencies: ZephyrInputActions (auto-generated InputAction wrapper, global
 *               namespace), UnityEngine.InputSystem, UnityEngine.Vector2.
 * Scene:    GameManager (attached to player prefab's "Core" child).
 * Ch.Ref:   Ch.6.7 Owner Binding (input → state machine flow), Ch.1 Engineering Structure.
 */
using UnityEngine;
using UnityEngine.InputSystem;
using Zephyr.Core.UI.Dialogue;

namespace Zephyr.Gameplay.Player.Input
{
    /// <summary>
    /// Reads player input from the ZephyrInputActions asset and exposes it
    /// as clean properties for state machine consumers. StateActions read
    /// <see cref="MoveInput"/> to determine movement direction.
    /// Lifecycle: Enable/Disable the GamePlay action map OnEnable/OnDisable.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour, ZephyrInputActions.IGamePlayActions
    {
        private ZephyrInputActions _actions;

        /// <summary>
        /// Current move direction from WASD. X = horizontal (A/D), Y = vertical (W/S).
        /// Values are -1, 0, or 1 from the 2DVector composite binding.
        /// Read by MoveInputAction (state machine) and state conditions.
        /// </summary>
        public Vector2 MoveInput { get; private set; }

        /// <summary>
        /// True when there is any horizontal movement input (|MoveInput.x| > 0.01).
        /// Used by state conditions to gate Idle → Walk/Run transitions.
        /// </summary>
        public bool HasHorizontalInput => Mathf.Abs(MoveInput.x) > 0.01f;

        /// <summary>
        /// True when the sprint button is held. Used by state conditions to
        /// gate Walk → Sprint transitions.
        /// </summary>
        public bool HasSprintInput { get; private set; }

        /// <summary>
        /// True when the roll button is held. Used by state conditions to
        /// gate transitions into the Roll state.
        /// </summary>
        public bool HasRollInput { get; private set; }

        /// <summary>
        /// True when the crouch button is held. Used by state conditions to
        /// gate transitions into CrouchIdle / CrouchMoving states.
        /// </summary>
        public bool HasCrouchInput { get; private set; }

        /// <summary>
        /// True while a jump request is waiting to be consumed by a state condition.
        /// </summary>
        public bool HasJumpInput { get; private set; }

        /// <summary>
        /// True when a primary attack press is waiting for the state machine.
        /// </summary>
        public bool HasPrimaryAttackInput { get; private set; }

        /// <summary>
        /// True when a secondary attack press is waiting for the state machine.
        /// </summary>
        public bool HasSecondaryAttackInput { get; private set; }

        /// <summary>
        /// Consumes one pending jump request. Held input cannot create another request.
        /// </summary>
        public bool ConsumeJumpInput()
        {
            if (!HasJumpInput) return false;

            HasJumpInput = false;
            return true;
        }

        /// <summary>
        /// Consumes one pending roll/dash request. The same input is used for both:
        /// grounded → roll, airborne → dash. Mutually exclusive by grounded check
        /// in the state conditions, so only one will consume it per press.
        /// </summary>
        public bool ConsumeRollInput()
        {
            if (!HasRollInput) return false;

            HasRollInput = false;
            return true;
        }

        public bool ConsumePrimaryAttackInput()
        {
            if (!HasPrimaryAttackInput) return false;

            HasPrimaryAttackInput = false;
            return true;
        }

        public bool ConsumeSecondaryAttackInput()
        {
            if (!HasSecondaryAttackInput) return false;

            HasSecondaryAttackInput = false;
            return true;
        }

        private void Awake()
        {
            _actions = new ZephyrInputActions();
        }

        private void OnEnable()
        {
            _actions.GamePlay.AddCallbacks(this);
            DialogueManager.GameplayInputSuspended += DisableGameplayInput;
            DialogueManager.GameplayInputRestored += EnableGameplayInput;

            if (DialogueManager.Instance != null && DialogueManager.Instance.IsOpen)
                DisableGameplayInput();
            else
                EnableGameplayInput();
        }

        private void OnDisable()
        {
            DialogueManager.GameplayInputSuspended -= DisableGameplayInput;
            DialogueManager.GameplayInputRestored -= EnableGameplayInput;
            _actions.GamePlay.Disable();
            _actions.GamePlay.RemoveCallbacks(this);
            ClearInputState();
        }

        private void DisableGameplayInput()
        {
            _actions?.GamePlay.Disable();
            ClearInputState();
        }

        private void EnableGameplayInput()
        {
            if (isActiveAndEnabled) _actions?.GamePlay.Enable();
        }

        private void ClearInputState()
        {
            MoveInput = Vector2.zero;
            HasSprintInput = false;
            HasRollInput = false;
            HasCrouchInput = false;
            HasJumpInput = false;
            HasPrimaryAttackInput = false;
            HasSecondaryAttackInput = false;
        }

        private void OnDestroy()
        {
            _actions?.Dispose();
        }

        /// <summary>
        /// Called by the Input System when the Move action is started, performed, or canceled.
        /// Updates the cached move input value.
        /// </summary>
        public void OnMove(InputAction.CallbackContext context)
        {
            MoveInput = context.ReadValue<Vector2>();
        }

        /// <summary>
        /// Called by the Input System when the Sprint action is started, performed, or canceled.
        /// Updates the sprint held state.
        /// </summary>
        public void OnSprint(InputAction.CallbackContext context)
        {
            HasSprintInput = context.ReadValueAsButton();
        }

        /// <summary>
        /// Called by the Input System when the Roll action is started, performed, or canceled.
        /// Updates the roll held state.
        /// </summary>
        public void OnRoll(InputAction.CallbackContext context)
        {
            HasRollInput = context.ReadValueAsButton();
        }

        /// <summary>
        /// Called by the Input System when the Crouch action is started, performed, or canceled.
        /// Updates the crouch held state.
        /// </summary>
        public void OnCrouch(InputAction.CallbackContext context)
        {
            HasCrouchInput = context.ReadValueAsButton();
        }

        public void OnPrimaryAttack(InputAction.CallbackContext context)
        {
            if (context.started) HasPrimaryAttackInput = true;
        }

        public void OnSecondaryAttack(InputAction.CallbackContext context)
        {
            if (context.started) HasSecondaryAttackInput = true;
        }

        /// <summary>
        /// Called by the Input System when the Jump action is started, performed, or canceled.
        /// Updates the jump pressed state. Jump is edge-triggered — only true on the press frame.
        /// </summary>
        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.started)
                HasJumpInput = true;
        }
    }
}
