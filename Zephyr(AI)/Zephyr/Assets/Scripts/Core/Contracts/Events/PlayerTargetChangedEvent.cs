using UnityEngine;

namespace Zephyr.Contracts.Events
{
    public readonly struct PlayerTargetChangedEvent
    {
        public readonly GameObject Player;
        public Transform Target => Player != null ? Player.transform : null;

        public PlayerTargetChangedEvent(GameObject player)
        {
            Player = player;
        }
    }
}
