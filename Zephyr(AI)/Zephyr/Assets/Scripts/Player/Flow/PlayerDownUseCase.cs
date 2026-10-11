using UnityEngine;
using Zephyr.Core.Combat;
using Zephyr.Gameplay.Player.Input;
using CoreStateMachine = Zephyr.Core.StateMachine.StateMachine;

namespace Zephyr.Gameplay.Player.Flow
{
    public sealed class PlayerDownUseCase
    {
        private readonly CombatTargetAvailability _availability;
        private readonly PlayerInputReader _input;
        private readonly CoreStateMachine _stateMachine;
        private readonly Rigidbody2D _body;

        public PlayerDownUseCase(GameObject player)
        {
            if (player == null) return;
            _availability = player.GetComponent<CombatTargetAvailability>();
            _input = player.GetComponentInChildren<PlayerInputReader>(true);
            _stateMachine = player.GetComponent<CoreStateMachine>();
            _body = player.GetComponent<Rigidbody2D>();
        }

        public void SetDown(bool downed)
        {
            _availability?.SetAvailable(!downed);
            if (_input != null) _input.enabled = !downed;
            if (_stateMachine != null) _stateMachine.enabled = !downed;
            if (_body == null) return;

            _body.linearVelocity = Vector2.zero;
            if (downed) _body.constraints = RigidbodyConstraints2D.FreezeAll;
        }
    }
}
