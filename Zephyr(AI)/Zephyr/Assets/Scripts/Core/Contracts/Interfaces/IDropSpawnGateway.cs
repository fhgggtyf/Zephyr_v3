using UnityEngine;

namespace Zephyr.Contracts.Interfaces
{
    public interface IDropSpawnGateway
    {
        GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation);
        GameObject SpawnCurrency(Vector3 position, int amount);
        GameObject SpawnWeapon(Vector3 position, string weaponId);
        int Seed { get; }
        int RoomDepth { get; }
    }

    public interface IDropSpawnGatewayConsumer
    {
        void Configure(IDropSpawnGateway gateway);
    }
}
