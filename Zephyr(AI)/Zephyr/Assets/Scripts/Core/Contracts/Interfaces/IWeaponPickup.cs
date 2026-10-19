using UnityEngine;

namespace Zephyr.Contracts.Interfaces
{
    public interface IWeaponPickup
    {
        void Spawn(Vector3 position);
        void Spawn(Vector3 position, string weaponId);
    }

    public interface ICurrencyPickup
    {
        void Spawn(Vector3 position, int amount);
    }
}
