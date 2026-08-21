/*
 * DashCompleteConditionSO.cs
 * ---------------------------
 * Module:  Player / StateMachine / Conditions
 * Purpose: State condition for exiting the Dash state. Returns true when
 *          the dash duration timer completes. Unlike roll, dash is not
 *          tied to an animation — it uses a deterministic gameplay timer
 *          for consistent behavior every time.
 * Scene:    GameManager (attached to player prefab).
 * Ch.Ref:   Ch.6.5 State Conditions, Ch.6.8 Physics / Dash Mechanics.
 */
using UnityEngine;
using Zephyr.Core.StateMachine;
using CoreSM = Zephyr.Core.StateMachine.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using Zephyr.Gameplay.Player.Core;

namespace Zephyr.Gameplay.Player.StateMachine
{
    [CreateAssetMenu(fileName = "DashCompleteCondition", menuName = "State Machines/Conditions/Player/Dash Complete")]
    public class DashCompleteConditionSO : StateConditionSO
    {
        protected override Condition CreateCondition() => new DashCompleteCondition();
    }

    public class DashCompleteCondition : Condition
    {
        private MovementCore _movementCore;

        public override void Awake(CoreSM stateMachine)
        {
            _movementCore = stateMachine.GetCachedComponent<MovementCore>();
        }

        protected override bool Statement()
        {
            return _movementCore != null && _movementCore.IsDashDurationComplete;
        }
    }
}
