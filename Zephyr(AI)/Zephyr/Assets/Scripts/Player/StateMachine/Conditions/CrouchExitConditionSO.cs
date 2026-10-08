/*
 * CrouchExitConditionSO.cs
 * ------------------------
 * Module:  Player / StateMachine / Conditions
 * Purpose: Condition for exiting crouch states when the crouch button is released.
 * Scene:    GameManager (attached to player prefab).
 * Ch.Ref:   Ch.6.5 State Conditions.
 */
using UnityEngine;
using Zephyr.Core.StateMachine;
using CoreSM = Zephyr.Core.StateMachine.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using Zephyr.Gameplay.Player.Core;
using Zephyr.Gameplay.Player.Input;

namespace Zephyr.Gameplay.Player.StateMachine
{
    [CreateAssetMenu(fileName = "CrouchExitCondition", menuName = "State Machines/Conditions/Player/Crouch Exit")]
    public class CrouchExitConditionSO : StateConditionSO
    {
        protected override Condition CreateCondition() => new CrouchExitCondition();
    }

    public class CrouchExitCondition : Condition
    {
        private PlayerInputReader _inputReader;
        private PlayerColliderController _colliderController;

        public override void Awake(CoreSM stateMachine)
        {
            _inputReader = stateMachine.GetCachedComponent<PlayerInputReader>();
            stateMachine.TryGetCachedComponent(out _colliderController);
        }

        protected override bool Statement()
        {
            if (_inputReader == null) return false;

            return !_inputReader.HasCrouchInput
                && (_colliderController == null || _colliderController.CanUseStandingProfile());
        }
    }
}
