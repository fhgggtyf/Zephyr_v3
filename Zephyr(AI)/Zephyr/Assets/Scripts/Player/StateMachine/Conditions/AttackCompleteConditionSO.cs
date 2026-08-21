using UnityEngine;
using Zephyr.Core.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using Zephyr.Core.Weapons;
using CoreSM = Zephyr.Core.StateMachine.StateMachine;

namespace Zephyr.Gameplay.Player.StateMachine
{
    [CreateAssetMenu(
        fileName = "AttackCompleteCondition",
        menuName = "State Machines/Conditions/Player/Attack Complete")]
    public class AttackCompleteConditionSO : StateConditionSO
    {
        protected override Condition CreateCondition()
        {
            return new AttackCompleteCondition();
        }
    }

    public class AttackCompleteCondition : Condition
    {
        private WeaponController m_weaponController;

        public override void Awake(CoreSM stateMachine)
        {
            stateMachine.TryGetCachedComponent(out m_weaponController);
        }

        protected override bool Statement()
        {
            return m_weaponController == null || m_weaponController.IsAttackComplete;
        }
    }
}
