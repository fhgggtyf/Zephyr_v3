using UnityEngine;
using Zephyr.Contracts.Damage;

namespace Zephyr.Contracts.Interfaces
{
    /// <summary>
    /// Public actor-level entry point for incoming damage. The receiver owns
    /// first-hand hit routing and returns the actual resolution to the caller.
    /// </summary>
    public interface IDamageReceiver
    {
        DamageResolution ReceiveDamage(DamageInfo damageInfo, Vector2 impactPoint);
    }
}
