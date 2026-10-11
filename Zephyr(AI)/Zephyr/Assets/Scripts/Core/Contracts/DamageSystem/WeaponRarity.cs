/*
 * WeaponRarity.cs
 * ---------------
 * Module:  Core / DamageSystem
 * Purpose: Defines the rarity tier of a weapon, used for rarity-based damage multiplier
 *          lookups (e.g., a rare rarity buff applies only to rare weapons). Rarity also
 *          influences base damage scaling and drop/pool rates. Values are carried in
 *          DamageInfo so the damage pipeline and downstream systems can react.
 * Dependencies: None (pure enum).
 * Scene:    N/A (compilation-only; referenced by DamageInfo, weapon data, loot system).
 * Ch.Ref:   Ch.2.8 Damage Calculation Pipeline, Ch.9 Combat System, Ch.11 Loot System.
 */

namespace Zephyr.Core.DamageSystem
{
    /// <summary>
    /// Rarity tier of a weapon. Higher rarities have stronger base stats and can
    /// trigger rarity-based buff multipliers from the player's in-run buffs or meta upgrades.
    /// </summary>
    public enum WeaponRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
}