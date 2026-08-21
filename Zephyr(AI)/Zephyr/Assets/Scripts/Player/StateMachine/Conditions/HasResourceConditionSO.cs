using UnityEngine;
using Zephyr.Core.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using Zephyr.Core.Weapons;
using CoreSM = Zephyr.Core.StateMachine.StateMachine;

namespace Zephyr.Gameplay.Player.StateMachine
{
    [CreateAssetMenu(
        fileName = "HasResourceCondition",
        menuName = "State Machines/Conditions/Player/Has Attack Resource")]
    public sealed class HasResourceConditionSO : StateConditionSO
    {
        [SerializeField] private WeaponSlot m_weaponSlot;

        protected override Condition CreateCondition()
        {
            return new HasResourceCondition(m_weaponSlot);
        }
    }

    public sealed class HasResourceCondition : Condition
    {
        private readonly WeaponSlot m_weaponSlot;
        private WeaponController m_weaponController;

        public HasResourceCondition(WeaponSlot weaponSlot)
        {
            m_weaponSlot = weaponSlot;
        }

        public override void Awake(CoreSM stateMachine)
        {
            stateMachine.TryGetCachedComponent(out m_weaponController);
        }

        protected override bool Statement()
        {
            return m_weaponController != null && m_weaponController.HasAttackResource(m_weaponSlot);
        }
    }
}
