/*
 * WeaponCategory.cs
 * -----------------
 * Module:  Core / DamageSystem
 * Purpose: Defines the physical category of a weapon, used for category-based damage
 *          multiplier lookups (e.g., a melee buff applies only to melee weapons).
 *          Values are carried in DamageInfo so the damage pipeline and downstream systems
 *          (floating text, buff resolver) can react to the attack's category.
 * Dependencies: None (pure enum).
 * Scene:    N/A (compilation-only; referenced by DamageInfo, weapon data, buff system).
 * Ch.Ref:   Ch.2.8 Damage Calculation Pipeline, Ch.9 Combat System.
 */

namespace Zephyr.Core.DamageSystem
{
    /// <summary>
    /// Physical category of a weapon or attack source.
    /// Used for category-based damage multiplier lookups.
    /// </summary>
    public enum WeaponCategory
    {
        Melee,
        Ranged
    }
}