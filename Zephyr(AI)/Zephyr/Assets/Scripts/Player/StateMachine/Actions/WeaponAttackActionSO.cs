using UnityEngine;
using Zephyr.Core.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using Zephyr.Core.Weapons;
using Zephyr.Gameplay.Player.Core;
using Zephyr.Gameplay.Player.Data;
using Zephyr.Gameplay.Player.Input;
using Zephyr.Gameplay.Player.Visual;
using CoreSM = Zephyr.Core.StateMachine.StateMachine;

namespace Zephyr.Gameplay.Player.StateMachine
{
    [CreateAssetMenu(
        fileName = "WeaponAttackAction",
        menuName = "State Machines/Actions/Player/Weapon Attack")]
    public class WeaponAttackActionSO : StateActionSO
    {
        [Tooltip("Movement speeds used by the Variable Input attack movement mode.")]
        [SerializeField] private PlayerMovementSpeedDataSO m_speedData;

        [Tooltip("Jump physics used to preserve normal airborne motion during attacks.")]
        [SerializeField] private JumpDataSO m_jumpData;

        protected override StateAction CreateAction()
        {
            return new WeaponAttackAction(m_speedData, m_jumpData);
        }
    }

    public class WeaponAttackAction : StateAction
    {
        private float m_entryMoveSpeed;
        private readonly PlayerMovementSpeedDataSO m_speedData;
        private readonly JumpDataSO m_jumpData;
        private MovementCore m_movementCore;
        private PlayerInputReader m_inputReader;
        private WeaponController m_weaponController;
        private SpriteAnimator m_spriteAnimator;
        private bool m_ownsAirborneGravity;
        private float m_ownedGravityScale;
        private float m_previousGravityScale;

        public WeaponAttackAction(PlayerMovementSpeedDataSO speedData, JumpDataSO jumpData)
        {
            m_speedData = speedData;
            m_jumpData = jumpData;
        }

        public override void Awake(CoreSM stateMachine)
        {
            stateMachine.TryGetCachedComponent(out m_movementCore);
            stateMachine.TryGetCachedComponent(out m_inputReader);
            stateMachine.TryGetCachedComponent(out m_weaponController);
            stateMachine.TryGetCachedComponent(out m_spriteAnimator);
        }

        public override void OnStateEnter()
        {
            if (m_movementCore != null) m_entryMoveSpeed = m_movementCore.CurrentSpeed;
            m_inputReader?.ClearJumpAndAttackInputs();

            bool attackStarted = m_weaponController != null && m_weaponController.BeginRequestedAttack();
            if (!attackStarted) return;

            PlayPlayerAttackAnimation();
            ApplyInitialAttackFacing();
            UpdateAirborneGravity();
            UpdateAttackMovement();
        }

        public override void OnUpdate()
        {
            UpdateAirborneGravity();
            UpdateAttackFacing();
            UpdateAttackMovement();
            m_weaponController?.UpdateAttack();
        }

        public override void OnStateExit()
        {
            RestoreAirborneGravity();

            if (m_movementCore != null)
            {
                m_movementCore.SetMoveInput(Vector2.zero);
                m_movementCore.SetMoveSpeed(m_entryMoveSpeed);
            }

            m_weaponController?.CancelAttack();
            m_spriteAnimator?.RestoreBaseController();
        }

        private void UpdateAirborneGravity()
        {
            if (m_jumpData == null || m_movementCore == null) return;

            if (m_movementCore.IsGrounded)
            {
                RestoreAirborneGravity();
                return;
            }

            float gravityScale = m_movementCore.VerticalVelocity > 0f
                ? m_jumpData.gravityScale
                : m_jumpData.gravityScale * m_jumpData.fallGravityMultiplier;

            if (!m_ownsAirborneGravity)
            {
                m_previousGravityScale = m_movementCore.SetGravityScale(gravityScale);
                m_ownedGravityScale = gravityScale;
                m_ownsAirborneGravity = true;
            }
            else if (!Mathf.Approximately(m_ownedGravityScale, gravityScale))
            {
                m_movementCore.SetGravityScale(gravityScale);
                m_ownedGravityScale = gravityScale;
            }

            m_movementCore.SetMaxFallSpeed(
                m_movementCore.VerticalVelocity <= 0f ? m_jumpData.maxFallSpeed : 0f);
        }

        private void RestoreAirborneGravity()
        {
            if (!m_ownsAirborneGravity || m_movementCore == null) return;

            m_movementCore.RestoreGravityScale(m_ownedGravityScale, m_previousGravityScale);
            m_movementCore.SetMaxFallSpeed(0f);
            m_ownsAirborneGravity = false;
        }

        /// <summary>
        /// Called once on attack entry. If the active weapon's AttackDirectionData
        /// specifies a fixed horizontal direction (Forward / Backward) for this
        /// combo step, force MovementCore.Facing to match that direction. This
        /// ensures the player body (SpriteAnimator) and weapon sprite flip to
        /// align with the weapon authoring, even if the player was facing the
        /// opposite way. For modes Facing / Up / Down this is a no-op.
        /// </summary>
        private void ApplyInitialAttackFacing()
        {
            if (m_movementCore == null || m_weaponController?.CurrentAttackContext is not AttackContext context) return;

            AttackDirectionData directionData = context.Weapon.GetData<AttackDirectionData>();
            AttackDirectionMode mode = directionData?.GetAttackData(context.ComboIndex)?.Mode
                ?? AttackDirectionMode.Facing;
            float forcedFacing = mode switch
            {
                AttackDirectionMode.Forward => 1f,
                AttackDirectionMode.Backward => -1f,
                _ => 0f
            };

            if (Mathf.Abs(forcedFacing) > 0.01f)
            {
                m_movementCore.SetFacing(forcedFacing);
            }
        }

        private void PlayPlayerAttackAnimation()
        {
            if (m_spriteAnimator == null || m_weaponController == null) return;

            ComboStep step = m_weaponController.CurrentAttackStep;
            if (step == null || string.IsNullOrWhiteSpace(step.PlayerAnimationName)
                || step.PlayerAnimationPlaceholder == null
                || step.PlayerAnimationClip == null) return;

            float duration = m_weaponController.CurrentAttackContext is AttackContext context
                ? context.Duration
                : Mathf.Max(0.01f, step.DurationInSeconds);

            m_spriteAnimator.PlayAnimationOverride(
                step.PlayerAnimationName,
                step.PlayerAnimationPlaceholder,
                step.PlayerAnimationClip,
                step.PlayerAnimationClip.length > 0f
                    ? step.PlayerAnimationClip.length / duration
                    : 1f,
                true);
        }

        private void UpdateAttackFacing()
        {
            if (m_movementCore == null || m_inputReader == null) return;
            if (m_weaponController?.CurrentAttackContext is not AttackContext context) return;

            AttackFacingData facingData = context.Weapon.GetData<AttackFacingData>();
            if (!(facingData?.GetAttackData(context.ComboIndex)?.CanChangeFacing ?? false)) return;

            m_movementCore.SetFacing(m_inputReader.MoveInput.x);
        }

        private void UpdateAttackMovement()
        {
            if (m_movementCore == null) return;
            if (m_weaponController?.CurrentAttackContext is not AttackContext context) return;

            AttackMovementData attackMovementData = context.Weapon.GetData<AttackMovementData>();
            AttackMovementAttackData movementData = attackMovementData?.GetAttackData(context.ComboIndex);
            AttackMovementMode mode = movementData?.Mode ?? AttackMovementMode.Locked;

            switch (mode)
            {
                case AttackMovementMode.Input:
                    m_movementCore.SetMoveSpeed(movementData.InputSpeed);
                    m_movementCore.SetMoveInput(m_inputReader != null ? m_inputReader.MoveInput : Vector2.zero);
                    break;
                case AttackMovementMode.VariableInput:
                    ApplyVariableInputMovement();
                    break;
                case AttackMovementMode.SurgeForward:
                case AttackMovementMode.SurgeBackward:
                    ApplySurge(movementData, mode == AttackMovementMode.SurgeForward ? 1f : -1f);
                    break;
                default:
                    m_movementCore.SetMoveSpeed(0f);
                    m_movementCore.SetMoveInput(Vector2.zero);
                    break;
            }
        }

        private void ApplyVariableInputMovement()
        {
            Vector2 moveInput = m_inputReader != null ? m_inputReader.MoveInput : Vector2.zero;
            m_movementCore.SetMoveInput(moveInput);

            if (m_speedData == null)
            {
                m_movementCore.SetMoveSpeed(0f);
                return;
            }

            float speed;
            if (m_inputReader == null || !m_inputReader.HasHorizontalInput)
            {
                speed = m_speedData.idleSpeed;
            }
            else if (m_inputReader.HasSprintInput)
            {
                speed = m_speedData.sprintSpeed;
            }
            else
            {
                speed = m_speedData.walkSpeed;
            }

            m_movementCore.SetMoveSpeed(speed);
        }

        private void ApplySurge(AttackMovementAttackData movementData, float direction)
        {
            float normalizedTime = 1f;
            if (m_weaponController?.CurrentAttackContext is AttackContext context)
            {
                float duration = context.EndsAt - context.StartedAt;
                normalizedTime = duration > 0f
                    ? Mathf.Clamp01((Time.time - context.StartedAt) / duration)
                    : 1f;
            }

            float speed = Mathf.Lerp(movementData.InitialSpeed, movementData.FinalSpeed, normalizedTime);
            m_movementCore.SetMoveSpeed(speed);
            m_movementCore.SetMoveInput(m_movementCore.Facing * direction);
        }
    }
}
