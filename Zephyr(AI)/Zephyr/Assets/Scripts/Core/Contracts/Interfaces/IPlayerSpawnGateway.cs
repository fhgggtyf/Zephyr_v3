using System;
using UnityEngine;

namespace Zephyr.Contracts.Interfaces
{
    public interface IPlayerSpawnGateway : IPlayerRespawnGateway
    {
        GameObject CurrentPlayer { get; }
        event Action<GameObject> PlayerSpawned;
        event Action PlayerDespawned;
    }
}
