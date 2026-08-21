using System;

namespace Zephyr.Core.Weapons
{
    /// <summary>
    /// Marks a weapon as capable of starting attacks while its wielder is airborne.
    /// </summary>
    public sealed class AirAttackWeaponComponent : WeaponComponent
    {
    }

    [Serializable]
    public sealed class AirAttackData : ComponentData
    {
        public override Type ComponentDependency => typeof(AirAttackWeaponComponent);
    }
}
