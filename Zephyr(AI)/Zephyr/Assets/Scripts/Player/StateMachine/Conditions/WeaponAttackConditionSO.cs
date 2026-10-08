/*
 * WeaponAttackConditionSO.cs
 * --------------------------
 * Module:  Player / State Machine / Conditions
 * Purpose: Handles either attack input through one slot-configurable condition and
 *          records the requested weapon slot for the shared attack state action.
 */
using UnityEngine;
using Zephyr.Core.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using Zephyr.Core.Weapons;
using Zephyr.Gameplay.Player.Input;
using CoreSM = Zephyr.Core.StateMachine.StateMachine;

namespace Zephyr.Gameplay.Player.StateMachine
{
    [CreateAssetMenu(
        fileName = "WeaponAttackCondition",
        menuName = "State Machines/Conditions/Player/Weapon Attack")]
    public class WeaponAttackConditionSO : StateConditionSO
    {
        [SerializeField] protected WeaponSlot m_weaponSlot;

        protected override Condition CreateCondition()
        {
            return new WeaponAttackCondition(m_weaponSlot);
        }
    }

    public class WeaponAttackCondition : Condition
    {
        private readonly WeaponSlot m_weaponSlot;
        private PlayerInputReader m_inputReader;
        private WeaponController m_weaponController;

        public WeaponAttackCondition(WeaponSlot weaponSlot)
        {
            m_weaponSlot = weaponSlot;
        }

        public override void Awake(CoreSM stateMachine)
        {
            stateMachine.TryGetCachedComponent(out m_inputReader);
            stateMachine.TryGetCachedComponent(out m_weaponController);
        }

        protected override bool Statement()
        {
            if (m_inputReader == null || m_weaponController == null) return false;

            bool hasInput = m_weaponSlot == WeaponSlot.Primary
                ? m_inputReader.HasPrimaryAttackInput
                : m_inputReader.HasSecondaryAttackInput;
            if (!hasInput) return false;

            // Reject resource-gated presses immediately; do not replay them after regeneration.
            if (!m_weaponController.HasAttackResource(m_weaponSlot))
            {
                ConsumeAttackInput();
                return false;
            }

            // Preserve ordinary attack buffering while an affordable attack is temporarily blocked.
            if (!m_weaponController.CanAttack(m_weaponSlot)) return false;

            ConsumeAttackInput();
            m_weaponController.RequestAttack(m_weaponSlot);
            return true;
        }

        private void ConsumeAttackInput()
        {
            if (m_weaponSlot == WeaponSlot.Primary)
                m_inputReader.ConsumePrimaryAttackInput();
            else
                m_inputReader.ConsumeSecondaryAttackInput();
        }
    }
}
