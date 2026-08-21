/*
 * MovementCore.cs
 * ---------------
 * Module:  Player / Core
 * Purpose: Physics-based movement controller for the player. Owns the Rigidbody2D
 *          and runs all motion authority in FixedUpdate (not Update — per Ch.6.8).
 *          Implements IFacingProvider so external systems (AI detection, backstab
 *          calc, camera look-ahead) can read the player's facing direction.
 *          Input is fed via SetMoveInput() by a StateAction (MoveInputAction);
 *          speed is set by StateActions (Idle=0, Walk=3, Run=6).
 *          No direct dependency on input systems or visuals — pure physics + state.
 *          Rigidbody2D is on the Root GameObject; this component lives on the "Core"
 *          child and finds the Rigidbody2D via GetComponentInParent.
 *          Ground detection is delegated to GroundChecker; this component only
 *          reads the ground state, it does not detect it.
 * Dependencies: Rigidbody2D (on parent Root GameObject), GroundChecker (on Core child),
 *               IFacingProvider (Core interface), GameSettingsSO (tuning).
 * Scene:    GameManager (attached to player prefab's "Core" child).
 * Ch.Ref:   Ch.6.7 Owner Binding, Ch.6.8 Physics / FixedUpdate, Ch.11 Enemy AI.
 */
using UnityEngine;
using Zephyr.Core.Interfaces;

namespace Zephyr.Gameplay.Player.Core
{
    /// <summary>
    /// Physics-driven movement controller. Runs movement in FixedUpdate to keep
    /// physics deterministic. StateActions set the move speed and feed input
    /// via SetMoveInput(); this component applies Rigidbody2D velocity.
    /// Rigidbody2D is expected to be on the parent Root GameObject (not on this child).
    /// Visual presentation (sprite flip, animation) is handled separately by SpriteAnimator.
    /// Ground detection is owned by GroundChecker; this component reads its state.
    /// </summary>
    public class MovementCore : MonoBehaviour, IFacingProvider
    {
        [Header("Movement Tuning")]
        [Tooltip("Default move speed; overridden by state actions (Idle=0, Walk=3, Run=6)")]
        [SerializeField] private float _defaultMoveSpeed = 5f;

        [Tooltip("Small horizontal velocity used to separate from a wall while airborne.")]
        [SerializeField] private float _airborneWallSeparationSpeed = 0.1f;

        private Rigidbody2D _rigidbody;
        private GroundChecker _groundChecker;

        /// <summary>
        /// Current move speed in units/second. Set by StateActions (Idle = 0, Walk = 3, Run = 6).
        /// MovementCore applies: velocity = MoveInput * CurrentSpeed in FixedUpdate.
        /// </summary>
        public float CurrentSpeed { get; set; }

        /// <summary>
        /// True when the player is touching the ground. Reads from GroundChecker.
        /// Used by jump/double-jump state conditions.
        /// </summary>
        public bool IsGrounded => _groundChecker != null && _groundChecker.IsGrounded;

        /// <summary>
        /// True when the player's left side is touching a wall.
        /// Used by ApplyMovement to prevent getting stuck when pushing into a wall.
        /// Reads from GroundChecker.
        /// </summary>
        public bool IsAgainstWallLeft => _groundChecker != null && _groundChecker.IsAgainstWallLeft;

        /// <summary>
        /// True when the player's right side is touching a wall.
        /// Used by ApplyMovement to prevent getting stuck when pushing into a wall.
        /// Reads from GroundChecker.
        /// </summary>
        public bool IsAgainstWallRight => _groundChecker != null && _groundChecker.IsAgainstWallRight;

        /// <summary>
        /// Logical facing direction. Updated from the last non-zero horizontal
        /// move input. +1 = right, -1 = left. Used by AI detection, backstab, camera.
        /// Visual flipping is handled by SpriteAnimator which reads this property.
        /// Implements IFacingProvider.Facing (direction vector).
        /// </summary>
        public Vector2 Facing { get; private set; } = Vector2.right;

        /// <summary>
        /// Current move vector fed by the state machine. Set by MoveInputAction
        /// each frame from PlayerInputReader.MoveInput.
        /// </summary>
        private Vector2 _moveInput;

        /// <summary>
        /// True when a roll is active. Set by RollActiveActionSO on state enter/exit.
        /// When true, movement input is ignored (direction locked, cannot change
        /// mid-roll). Read by ApplyMovement() and SetFacing() to enforce lock.
        /// Roll completion is determined by RollCompleteConditionSO via SpriteAnimator.
        /// </summary>
        public bool IsRollActive { get; private set; }

        /// <summary>
        /// True when a dash is active. Set by DashActiveActionSO on state enter/exit.
        /// When true, movement input is ignored (direction locked, cannot change
        /// mid-dash). Vertical velocity is zeroed to maintain a straight horizontal
        /// trajectory. Read by ApplyMovement() and SetFacing() to enforce lock.
        /// </summary>
        public bool IsDashActive { get; private set; }

        /// <summary>
        /// True when the player is crouching. Set by CrouchActionSO on state enter/exit.
        /// Used by other systems (e.g. animator, collision height) to adjust behavior.
        /// </summary>
        public bool IsCrouching { get; private set; }

        /// <summary>
        /// True when the player has not yet used their air jump (double jump)
        /// since last grounding. Set by ResetAirJumpsActionSO on grounded state
        /// entry. Consumed by JumpForceActionSO when starting an airborne jump.
        /// </summary>
        public bool HasDoubleJumpRemaining { get; private set; } = true;

        /// <summary>
        /// True when the roll cooldown is still active. Set by StartRollCooldown()
        /// when exiting the roll state. Prevents chain-rolling by requiring a delay
        /// before the next roll can start.
        /// </summary>
        public bool IsRollOnCooldown => Time.time < _rollCooldownEndTime;

        /// <summary>
        /// True when the dash cooldown is still active. Set by StartDashCooldown()
        /// when exiting the dash state. Prevents chain-dashing by requiring a delay.
        /// </summary>
        public bool IsDashOnCooldown => Time.time < _dashCooldownEndTime;

        /// <summary>
        /// True once the active dash reaches its configured duration.
        /// Provides a deterministic gameplay timer for dash completion.
        /// </summary>
        public bool IsDashDurationComplete => IsDashActive && Time.time >= _dashEndTime;

        /// <summary>
        /// Current rigidbody velocity. Read by state conditions to detect
        /// peak (velocity.y <= 0) and fall phases.
        /// </summary>
        public Vector2 Velocity => _rigidbody != null ? _rigidbody.linearVelocity : Vector2.zero;

        /// <summary>
        /// Current vertical velocity component. Positive = rising, negative = falling.
        /// Used by jump state conditions to detect transition from jump-up to peak.
        /// </summary>
        public float VerticalVelocity => _rigidbody != null ? _rigidbody.linearVelocity.y : 0f;

        private float _rollCooldownEndTime;
        private float _dashEndTime;
        private float _dashCooldownEndTime;

        /// <summary>
        /// Maximum fall speed cap set by external systems (e.g. JumpFallActionSO).
        /// 0 = no cap. Applied in FixedUpdate after movement to prevent
        /// gravity from accelerating the player beyond the desired terminal velocity.
        /// </summary>
        private float _maxFallSpeed;

        private void Awake()
        {
            _rigidbody = GetComponentInParent<Rigidbody2D>();
            _groundChecker = GetComponent<GroundChecker>();
            CurrentSpeed = _defaultMoveSpeed;

            if (_rigidbody == null)
            {
                Debug.LogError("[MovementCore] Rigidbody2D is missing from the player root hierarchy.", this);
                enabled = false;
                return;
            }

            Debug.Log($"[MovementCore] Awake: rb=true, groundChecker={_groundChecker != null}, defaultSpeed={_defaultMoveSpeed}");
        }

        private void FixedUpdate()
        {
            ApplyMovement();
            ClampFallSpeed();
        }

        /// <summary>
        /// Applies velocity from the current move input and speed.
        /// During roll, movement input is ignored — the roll carries the player
        /// forward in the locked facing direction at the roll speed.
        /// When grounded and pushing into a wall, horizontal velocity is zeroed.
        /// When airborne, a small velocity away from the wall breaks friction
        /// contact so the player continues falling normally.
        /// </summary>
        private void ApplyMovement()
        {
            if (CurrentSpeed <= 0f)
            {
                _rigidbody.linearVelocity = new Vector2(0f, _rigidbody.linearVelocity.y);
                return;
            }

            Vector2 effectiveInput;

            if (IsRollActive)
            {
                // During roll: lock direction to facing, ignore player input
                effectiveInput = Facing;
            }
            else if (IsDashActive)
            {
                // During dash: lock direction to facing, ignore player input
                effectiveInput = Facing;
            }
            else
            {
                effectiveInput = _moveInput;
            }

            var horizontalVelocity = effectiveInput.x * CurrentSpeed;

            // Grounded movement stops at a wall. While airborne, a tiny velocity
            // away from the wall breaks friction contact so gravity can keep falling.
            if (!IsRollActive && !IsDashActive)
            {
                var wantsLeft = effectiveInput.x < -0.01f;
                var wantsRight = effectiveInput.x > 0.01f;

                if (wantsLeft && IsAgainstWallLeft)
                {
                    horizontalVelocity = IsGrounded ? 0f : _airborneWallSeparationSpeed;
                }
                else if (wantsRight && IsAgainstWallRight)
                {
                    horizontalVelocity = IsGrounded ? 0f : -_airborneWallSeparationSpeed;
                }
            }

            var targetVelocity = new Vector2(horizontalVelocity, _rigidbody.linearVelocity.y);

            if (IsDashActive)
            {
                // During dash: zero vertical velocity for a straight horizontal trajectory
                targetVelocity.y = 0f;
            }

            _rigidbody.linearVelocity = targetVelocity;
        }

        /// <summary>
        /// Called by MoveInputAction (state machine) every frame to feed the
        /// current move input vector. MovementCore does not read input directly.
        /// </summary>
        public void SetMoveInput(Vector2 input)
        {
            _moveInput = input;
        }

        /// <summary>
        /// Sets the logical facing direction from horizontal input. Called by
        /// FaceDirectionAction via the state system. During roll, facing is
        /// locked and cannot be changed — this method becomes a no-op.
        /// Facing only changes on non-zero horizontal input; stopping preserves
        /// the last facing direction for natural feel.
        /// </summary>
        public void SetFacing(float horizontalInput)
        {
            if (IsRollActive || IsDashActive) return;

            if (Mathf.Abs(horizontalInput) > 0.01f)
            {
                Facing = horizontalInput > 0 ? Vector2.right : Vector2.left;
            }
        }

        /// <summary>
        /// Called by StateActions (Idle, Walk, Run, etc.) to set the move speed.
        /// E.g., Idle sets to 0f, Walk sets to 3f, Run sets to 6f.
        /// MovementCore applies this speed to the current input in the next FixedUpdate.
        /// </summary>
        public void SetMoveSpeed(float speed)
        {
            CurrentSpeed = speed;
        }

        /// <summary>
        /// Called by RollActiveActionSO to mark roll active/inactive.
        /// When active, movement input is ignored and facing is locked.
        /// </summary>
        public void SetRollActive(bool active)
        {
            IsRollActive = active;
        }

        /// <summary>
        /// Starts the roll cooldown timer. Called externally (e.g. by RollActiveActionSO
        /// on state exit) to enforce a delay before the next roll can begin.
        /// </summary>
        public void StartRollCooldown(float duration)
        {
            _rollCooldownEndTime = Time.time + duration;
        }

        /// <summary>
        /// Called by DashActiveActionSO to mark dash active/inactive.
        /// When active, movement input is ignored and facing is locked.
        /// A timer is started to track dash duration completion.
        /// </summary>
        public void SetDashActive(bool active, float duration = 0.5f)
        {
            IsDashActive = active;
            _dashEndTime = active ? Time.time + Mathf.Max(0.01f, duration) : 0f;
        }

        /// <summary>
        /// Starts the dash cooldown timer. Called externally (e.g. by DashActiveActionSO
        /// on state exit) to enforce a delay before the next dash can begin.
        /// </summary>
        public void StartDashCooldown(float duration)
        {
            _dashCooldownEndTime = Time.time + duration;
        }

        /// <summary>
        /// Called by CrouchActionSO to mark crouch active/inactive.
        /// When active, other systems can read IsCrouching to adjust behavior.
        /// </summary>
        public void SetCrouching(bool active)
        {
            IsCrouching = active;
        }

        /// <summary>
        /// Consumes the available air jump. Called by JumpForceActionSO when
        /// starting a jump from an airborne state. Returns true if a jump was
        /// available, false if already consumed.
        /// </summary>
        public bool ConsumeAirJump()
        {
            if (!HasDoubleJumpRemaining) return false;

            HasDoubleJumpRemaining = false;
            return true;
        }

        /// <summary>
        /// Resets the air jump availability. Called by ResetAirJumpsActionSO
        /// on grounded state entry so the player gets their air jump back on landing.
        /// </summary>
        public void ResetAirJumps()
        {
            HasDoubleJumpRemaining = true;
        }

        /// <summary>
        /// Applies an upward jump impulse to the rigidbody. Called by JumpForceActionSO
        /// when entering the JumpUp state. Uses ForceMode2D.Impulse so the force goes
        /// through the physics engine's solver.
        /// </summary>
        public void ApplyJumpForce(float jumpForce)
        {
            if (_rigidbody == null)
            {
                Debug.LogWarning("[MovementCore] ApplyJumpForce: rigidbody is null!");
                return;
            }

            var horizontalVelocity = IsAgainstWallLeft || IsAgainstWallRight
                ? 0f
                : _rigidbody.linearVelocity.x;

            // 贴墙起跳时先停止持续推墙，避免接触摩擦削弱垂直跳跃。
            _rigidbody.linearVelocity = new Vector2(horizontalVelocity, 0f);
            _rigidbody.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

            Debug.Log($"[MovementCore] ApplyJumpForce: impulse={jumpForce}, resulting vel=({_rigidbody.linearVelocity.x:F2},{_rigidbody.linearVelocity.y:F2}), grounded={IsGrounded}, wallL={IsAgainstWallLeft}, wallR={IsAgainstWallRight}");
        }

        /// <summary>
        /// Sets the gravity scale on the rigidbody. Used by jump actions to
        /// support buffs (low gravity, quick fall, etc.). Returns the previous
        /// gravity scale so callers can restore it on state exit.
        /// </summary>
        public float SetGravityScale(float scale)
        {
            if (_rigidbody == null) return 1f;

            var previous = _rigidbody.gravityScale;
            _rigidbody.gravityScale = scale;
            return previous;
        }

        /// <summary>
        /// Restores a gravity value only while this owner still controls the value.
        /// Unrelated gravity changes made after acquisition are preserved.
        /// </summary>
        public void RestoreGravityScale(float ownedScale, float previousScale)
        {
            if (_rigidbody == null) return;
            if (Mathf.Approximately(_rigidbody.gravityScale, ownedScale))
                _rigidbody.gravityScale = previousScale;
        }

        /// <summary>
        /// Sets the maximum fall speed cap. Called by JumpFallActionSO on state
        /// enter/exit. Pass 0 to disable the cap. Clamping is applied in FixedUpdate.
        /// </summary>
        public void SetMaxFallSpeed(float maxFallSpeed)
        {
            _maxFallSpeed = maxFallSpeed;
        }

        /// <summary>
        /// Clamps the rigidbody's vertical velocity to the configured max fall
        /// speed. Runs after ApplyMovement in FixedUpdate to counteract gravity
        /// acceleration that occurs between FixedUpdate frames.
        /// </summary>
        private void ClampFallSpeed()
        {
            if (_maxFallSpeed <= 0f || _rigidbody == null) return;

            var velocity = _rigidbody.linearVelocity;
            if (velocity.y < -_maxFallSpeed)
            {
                velocity.y = -_maxFallSpeed;
                _rigidbody.linearVelocity = velocity;
            }
        }
    }
}
