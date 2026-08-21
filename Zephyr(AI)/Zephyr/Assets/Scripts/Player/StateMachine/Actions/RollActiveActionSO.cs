/*
 * RollActiveActionSO.cs
 * ---------------------
 * Module:  Player / StateMachine / Actions
 * Purpose: Manages roll lifecycle on MovementCore. OnStateEnter marks roll
 *          active (locks direction, ignores input). OnStateExit marks
 *          roll inactive. Roll completion is driven by the roll animation's
 *          normalized playback time or an Animation Event.
 * Scene:    GameManager (attached to player prefab).
 * Ch.Ref:   Ch.6.4 State Actions, Ch.6.8 Physics / Roll Mechanics.
 */
using UnityEngine;
using Zephyr.Core.StateMachine;
using CoreSM = Zephyr.Core.StateMachine.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using Zephyr.Gameplay.Player.Core;

namespace Zephyr.Gameplay.Player.StateMachine
{
    [CreateAssetMenu(fileName = "RollActiveAction", menuName = "State Machines/Actions/Player/Roll Active")]
    public class RollActiveActionSO : StateActionSO
    {
        [Tooltip("Cooldown duration after roll ends before you can roll again.")]
        [Min(0f)] public float cooldownDuration = 0.5f;

        protected override StateAction CreateAction() => new RollActiveAction(cooldownDuration);
    }

    public class RollActiveAction : StateAction
    {
        private MovementCore _movementCore;
        private readonly float _cooldownDuration;

        public RollActiveAction(float cooldownDuration)
        {
            _cooldownDuration = cooldownDuration;
        }

        public override void Awake(CoreSM stateMachine)
        {
            _movementCore = stateMachine.GetCachedComponent<MovementCore>();
        }

        public override void OnStateEnter()
        {
            _movementCore.SetRollActive(true);
        }

        public override void OnStateExit()
        {
            _movementCore.SetRollActive(false);
            _movementCore.StartRollCooldown(_cooldownDuration);
        }

        public override void OnUpdate() { }
    }
}
