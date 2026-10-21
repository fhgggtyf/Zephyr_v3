using UnityEngine;

namespace Zephyr.Contracts.Interfaces
{
    public interface IWeaponReplacementGateway
    {
        bool RequestWeaponPickup(GameObject pickup, GameObject player);
    }
}
