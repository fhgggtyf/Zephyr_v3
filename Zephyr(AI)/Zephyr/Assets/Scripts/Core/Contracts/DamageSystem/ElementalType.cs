/*
 * ElementalType.cs
 * ----------------
 * Module:  Core / DamageSystem
 * Purpose: Defines the elemental affinity of a weapon's damage, used for elemental-based
 *          damage multiplier lookups (e.g., a fire elemental buff applies only to fire
 *          weapons). None represents non-elemental damage (pure physical). Values are carried
 *          in DamageInfo so the damage pipeline and downstream systems can react to the
 *          attack's elemental type.
 * Dependencies: None (pure enum).
 * Scene:    N/A (compilation-only; referenced by DamageInfo, weapon data, buff system).
 * Ch.Ref:   Ch.2.8 Damage Calculation Pipeline, Ch.9 Combat System.
 */

namespace Zephyr.Core.DamageSystem
{
    /// <summary>
    /// Elemental affinity of a weapon or attack source.
    /// Used for elemental-based damage multiplier lookups and elemental reaction systems.
    /// </summary>
    public enum ElementalType
    {
        None,
        Physical,
        Fire,
        Ice,
        Lightning,
        Wind,
        Earth
    }
}