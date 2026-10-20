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
using Zephyr.Core.Flow;
using Zephyr.Contracts.Interfaces;

namespace Zephyr.Gameplay.Player.Input
{
    /// <summary>
    /// Reads player input from the ZephyrInputActions asset and exposes it
    /// as clean properties for state machine consumers. StateActions read
    /// <see cref="MoveInput"/> to determine movement direction.
    /// Lifecycle: Enable/Disable the GamePlay action map OnEnable/OnDisable.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour, ZephyrInputActions.IGamePlayActions, IGameManagerDependencyConsumer
    {
        private ZephyrInputActions _actions;
        private IDialogueInputGateway _dialogueInput;
        private ISceneLoadingGateway _sceneLoading;
        private bool _loadingBlocked;
        private bool _gameplayInputBlocked;

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
        /// True until the current Shift hold has started Sprint.
        /// </summary>
        public bool IsSprintPressFresh { get; private set; }

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

        /// <summary>
        /// Marks the current Shift hold as already used to enter Sprint.
        /// Releasing Shift resets this through OnSprint.
        /// </summary>
        public void ConsumeSprintStartInput()
        {
            IsSprintPressFresh = false;
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

        public void ClearJumpAndAttackInputs()
        {
            HasJumpInput = false;
            HasPrimaryAttackInput = false;
            HasSecondaryAttackInput = false;
        }

        private void Awake()
        {
            _actions = new ZephyrInputActions();
        }

private void OnEnable()
        {
            _actions ??= new ZephyrInputActions();
            _actions.GamePlay.AddCallbacks(this);
            SubscribeDialogueInput();

            _gameplayInputBlocked = (_dialogueInput != null && _dialogueInput.IsOpen) || _loadingBlocked;
            ClearInputState();
            EnableGameplayInput();
        }

private void OnDisable()
        {
            UnsubscribeDialogueInput();
            UnsubscribeSceneLoading();
            if (_actions == null) return;
            _actions.GamePlay.Disable();
            _actions.GamePlay.RemoveCallbacks(this);
            ClearInputState();
        }

        public void Configure(IGameManagerServices services)
        {
            UnsubscribeDialogueInput();
            UnsubscribeSceneLoading();
            _dialogueInput = services?.DialogueInput;
            _sceneLoading = services?.SceneLoading;
            SubscribeDialogueInput();
            SubscribeSceneLoading();
            _loadingBlocked = _sceneLoading != null && _sceneLoading.IsLoading;
            _gameplayInputBlocked = (_dialogueInput != null && _dialogueInput.IsOpen) || _loadingBlocked;
            ClearInputState();
            EnableGameplayInput();
        }

        private void SubscribeSceneLoading()
        {
            if (_sceneLoading == null) return;
            _sceneLoading.LoadingStarted += HandleLoadingStarted;
            _sceneLoading.LoadingCompleted += HandleLoadingCompleted;
            _sceneLoading.LoadingFailed += HandleLoadingFailed;
        }

        private void UnsubscribeSceneLoading()
        {
            if (_sceneLoading == null) return;
            _sceneLoading.LoadingStarted -= HandleLoadingStarted;
            _sceneLoading.LoadingCompleted -= HandleLoadingCompleted;
            _sceneLoading.LoadingFailed -= HandleLoadingFailed;
        }

        private void HandleLoadingStarted()
        {
            _loadingBlocked = true;
            DisableGameplayInput();
        }

        private void HandleLoadingCompleted()
        {
            _loadingBlocked = false;
            if (_dialogueInput == null || !_dialogueInput.IsOpen)
                RestoreGameplayInput();
        }

        private void HandleLoadingFailed()
        {
            _loadingBlocked = true;
            DisableGameplayInput();
        }

        private void SubscribeDialogueInput()
        {
            if (_dialogueInput == null) return;
            _dialogueInput.InputSuspended += DisableGameplayInput;
            _dialogueInput.InputRestored += RestoreGameplayInput;
        }

        private void UnsubscribeDialogueInput()
        {
            if (_dialogueInput == null) return;
            _dialogueInput.InputSuspended -= DisableGameplayInput;
            _dialogueInput.InputRestored -= RestoreGameplayInput;
        }

        private void DisableGameplayInput()
        {
            _gameplayInputBlocked = true;
            ClearInputState();
        }

        private void EnableGameplayInput()
        {
            if (isActiveAndEnabled) _actions?.GamePlay.Enable();
        }

        private void RestoreGameplayInput()
        {
            _gameplayInputBlocked = _loadingBlocked;
            ClearInputState();
            EnableGameplayInput();
        }

        private void ClearInputState()
        {
            MoveInput = Vector2.zero;
            HasSprintInput = false;
            IsSprintPressFresh = false;
            HasRollInput = false;
            HasCrouchInput = false;
            HasJumpInput = false;
            HasPrimaryAttackInput = false;
            HasSecondaryAttackInput = false;
        }

        private void OnDestroy()
        {
            UnsubscribeSceneLoading();
            _actions?.Dispose();
        }

        /// <summary>
        /// Called by the Input System when the Move action is started, performed, or canceled.
        /// Updates the cached move input value.
        /// </summary>
        public void OnMove(InputAction.CallbackContext context)
        {
            if (_gameplayInputBlocked) return;
            MoveInput = context.ReadValue<Vector2>();
        }

        public void OnInteract(InputAction.CallbackContext context){}

        /// <summary>
        /// Called by the Input System when the Sprint action is started, performed, or canceled.
        /// Updates the sprint held state.
        /// </summary>
        public void OnSprint(InputAction.CallbackContext context)
        {
            if (_gameplayInputBlocked) return;

            if (context.started)
            {
                HasSprintInput = true;
                IsSprintPressFresh = true;
                return;
            }

            if (context.canceled)
            {
                HasSprintInput = false;
                IsSprintPressFresh = false;
                return;
            }

            HasSprintInput = context.ReadValueAsButton();
        }

        /// <summary>
        /// Called by the Input System when the Roll action is started, performed, or canceled.
        /// Updates the roll held state.
        /// </summary>
        public void OnRoll(InputAction.CallbackContext context)
        {
            if (_gameplayInputBlocked) return;
            HasRollInput = context.ReadValueAsButton();
        }

        /// <summary>
        /// Called by the Input System when the Crouch action is started, performed, or canceled.
        /// Updates the crouch held state.
        /// </summary>
        public void OnCrouch(InputAction.CallbackContext context)
        {
            if (_gameplayInputBlocked) return;
            HasCrouchInput = context.ReadValueAsButton();
        }

        public void OnPrimaryAttack(InputAction.CallbackContext context)
        {
            if (_gameplayInputBlocked) return;
            if (context.started) HasPrimaryAttackInput = true;
        }

        public void OnSecondaryAttack(InputAction.CallbackContext context)
        {
            if (_gameplayInputBlocked) return;
            if (context.started) HasSecondaryAttackInput = true;
        }

        /// <summary>
        /// Called by the Input System when the Jump action is started, performed, or canceled.
        /// Updates the jump pressed state. Jump is edge-triggered — only true on the press frame.
        /// </summary>
        public void OnJump(InputAction.CallbackContext context)
        {
            if (_gameplayInputBlocked) return;
            if (context.started)
                HasJumpInput = true;
        }
    }
}
