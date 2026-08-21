using UnityEngine;
using Zephyr.Core.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using Zephyr.Core.Weapons;
using Zephyr.Gameplay.Player.Core;
using Zephyr.Gameplay.Player.Data;
using Zephyr.Gameplay.Player.Input;
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

        protected override StateAction CreateAction()
        {
            return new WeaponAttackAction(m_speedData);
        }
    }

    public class WeaponAttackAction : StateAction
    {
        private float m_entryMoveSpeed;
        private readonly PlayerMovementSpeedDataSO m_speedData;
        private MovementCore m_movementCore;
        private PlayerInputReader m_inputReader;
        private WeaponController m_weaponController;

        public WeaponAttackAction(PlayerMovementSpeedDataSO speedData)
        {
            m_speedData = speedData;
        }

        public override void Awake(CoreSM stateMachine)
        {
            stateMachine.TryGetCachedComponent(out m_movementCore);
            stateMachine.TryGetCachedComponent(out m_inputReader);
            stateMachine.TryGetCachedComponent(out m_weaponController);
        }

        public override void OnStateEnter()
        {
            if (m_movementCore != null) m_entryMoveSpeed = m_movementCore.CurrentSpeed;

            bool attackStarted = m_weaponController != null && m_weaponController.BeginRequestedAttack();
            if (!attackStarted) return;

            ApplyInitialAttackFacing();
            UpdateAttackMovement();
        }

        public override void OnUpdate()
        {
            UpdateAttackFacing();
            UpdateAttackMovement();
            m_weaponController?.UpdateAttack();
        }

        public override void OnStateExit()
        {
            if (m_movementCore != null)
            {
                m_movementCore.SetMoveInput(Vector2.zero);
                m_movementCore.SetMoveSpeed(m_entryMoveSpeed);
            }

            m_weaponController?.CancelAttack();
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
