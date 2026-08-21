using UnityEngine;
using Zephyr.Core.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using Zephyr.Core.Weapons;
using CoreSM = Zephyr.Core.StateMachine.StateMachine;

namespace Zephyr.Gameplay.Player.StateMachine
{
    [CreateAssetMenu(
        fileName = "ResourceCostAction",
        menuName = "State Machines/Actions/Player/Resource Cost")]
    public sealed class ResourceCostActionSO : StateActionSO
    {
        protected override StateAction CreateAction()
        {
            return new ResourceCostAction();
        }
    }

    public sealed class ResourceCostAction : StateAction
    {
        private WeaponController m_weaponController;

        public override void Awake(CoreSM stateMachine)
        {
            stateMachine.TryGetCachedComponent(out m_weaponController);
        }

        public override void OnStateEnter()
        {
            m_weaponController?.ConsumeRequestedAttackResource();
        }

        public override void OnUpdate()
        {
        }
    }
}
