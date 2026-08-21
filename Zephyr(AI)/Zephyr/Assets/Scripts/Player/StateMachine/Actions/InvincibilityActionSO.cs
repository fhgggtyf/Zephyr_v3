/*
 * InvincibilityActionSO.cs
 * ------------------------
 * Module:  Player / StateMachine / Actions
 * Purpose: Grants or revokes invincibility frames (i-frames) on the player.
 *          WhenToEnter = sets IsInvincible = true (damage ignored during roll).
 *          OnStateExit = sets IsInvincible = false. Read by HurtReceiver or
 *          damage-dealing systems before applying damage.
 * Scene:    GameManager (attached to player prefab).
 * Ch.Ref:   Ch.6.4 State Actions, Ch.6.10 Presentation / Combat Hooks.
 */
using UnityEngine;
using UnityEngine.Serialization;
using Zephyr.Core.StateMachine;
using CoreSM = Zephyr.Core.StateMachine.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using Zephyr.Gameplay.Player.Core;

namespace Zephyr.Gameplay.Player.StateMachine
{
    [CreateAssetMenu(fileName = "InvincibilityAction", menuName = "State Machines/Actions/Player/Invincibility")]
    public class InvincibilityActionSO : StateActionSO
    {
        [FormerlySerializedAs("grantOnEnter")]
        [Tooltip("If true, grants invincibility on state enter and revokes on exit.")]
        [SerializeField] private bool m_grantOnEnter = true;

        protected override StateAction CreateAction()
        {
            return new InvincibilityAction(m_grantOnEnter);
        }
    }

    public class InvincibilityAction : StateAction
    {
        private HurtReceiver _hurtReceiver;
        private readonly bool _grantOnEnter;

        public InvincibilityAction(bool grantOnEnter)
        {
            _grantOnEnter = grantOnEnter;
        }

        public override void Awake(CoreSM stateMachine)
        {
            _hurtReceiver = stateMachine.GetCachedComponent<HurtReceiver>();
        }

        public override void OnStateEnter()
        {
            if (_grantOnEnter)
                _hurtReceiver.SetInvincible(true);
        }

        public override void OnStateExit()
        {
            if (_grantOnEnter)
                _hurtReceiver.SetInvincible(false);
        }

        public override void OnUpdate() { }
    }
}
