/*
 * DashActiveActionSO.cs
 * ---------------------
 * Module:  Player / StateMachine / Actions
 * Purpose: Manages dash lifecycle on MovementCore. OnStateEnter marks dash
 *          active (locks direction, ignores input, zeros gravity for a
 *          straight horizontal trajectory). OnStateExit marks dash inactive
 *          and restores gravity. Dash completion is driven by a fixed timer
 *          (0.5s default) rather than an animation event.
 *          Speed is set separately by DashSpeedActionSO.
 * Scene:    GameManager (attached to player prefab).
 * Ch.Ref:   Ch.6.4 State Actions, Ch.6.8 Physics / Dash Mechanics.
 */
using UnityEngine;
using Zephyr.Core.StateMachine;
using CoreSM = Zephyr.Core.StateMachine.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using Zephyr.Gameplay.Player.Core;

namespace Zephyr.Gameplay.Player.StateMachine
{
    [CreateAssetMenu(fileName = "DashActiveAction", menuName = "State Machines/Actions/Player/Dash Active")]
    public class DashActiveActionSO : StateActionSO
    {
        [Tooltip("Duration of the dash in seconds. Fixed by design for consistency.")]
        [Min(0.01f)] public float dashDuration = 0.5f;

        [Tooltip("Cooldown duration after dash ends before you can dash again.")]
        [Min(0f)] public float cooldownDuration = 0.5f;

        protected override StateAction CreateAction() => new DashActiveAction(dashDuration, cooldownDuration);
    }

    public class DashActiveAction : StateAction
    {
        private MovementCore _movementCore;
        private readonly float _dashDuration;
        private readonly float _cooldownDuration;
        private float _previousGravityScale;

        public DashActiveAction(float dashDuration, float cooldownDuration)
        {
            _dashDuration = dashDuration;
            _cooldownDuration = cooldownDuration;
        }

        public override void Awake(CoreSM stateMachine)
        {
            _movementCore = stateMachine.GetCachedComponent<MovementCore>();
        }

        public override void OnStateEnter()
        {
            _previousGravityScale = _movementCore.SetGravityScale(0f);
            _movementCore.SetDashActive(true, _dashDuration);
        }

        public override void OnStateExit()
        {
            _movementCore.SetDashActive(false);
            _movementCore.SetGravityScale(_previousGravityScale);
            _movementCore.StartDashCooldown(_cooldownDuration);
        }

        public override void OnUpdate() { }
    }
}
