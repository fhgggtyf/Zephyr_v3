using UnityEngine;
using Zephyr.Core.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using Zephyr.Core.Weapons;
using Zephyr.Gameplay.Player.Input;
using CoreSM = Zephyr.Core.StateMachine.StateMachine;

namespace Zephyr.Gameplay.Player.StateMachine
{
    [CreateAssetMenu(
        fileName = "AirborneWeaponAttackCondition",
        menuName = "State Machines/Conditions/Player/Airborne Weapon Attack")]
    public sealed class AirborneWeaponAttackConditionSO : StateConditionSO
    {
        [SerializeField] private WeaponSlot m_weaponSlot;

        protected override Condition CreateCondition()
        {
            return new AirborneWeaponAttackCondition(m_weaponSlot);
        }
    }

    public sealed class AirborneWeaponAttackCondition : Condition
    {
        private readonly WeaponSlot m_weaponSlot;
        private PlayerInputReader m_inputReader;
        private WeaponController m_weaponController;

        public AirborneWeaponAttackCondition(WeaponSlot weaponSlot)
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

            WeaponSO weapon = m_weaponController.GetWeapon(m_weaponSlot);
            if (weapon?.GetData<AirAttackData>() == null)
            {
                ConsumeAttackInput();
                return false;
            }

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
