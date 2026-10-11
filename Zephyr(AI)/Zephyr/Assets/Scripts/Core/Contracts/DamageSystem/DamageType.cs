/*
 * DamageType.cs
 * -------------
 * Module:  Core / DamageSystem
 * Purpose: Defines the three damage types used by the Zephyr combat system:
 *          AD (Attack Damage / 物理伤害), AP (Ability Power / 法术伤害), and True (真实伤害).
 *          AD is reduced by Armor, AP by MagicResist, True bypasses both.
 *          Maps to the League-of-Legends-style mitigation formulas in DamageCalculator.
 * Dependencies: None (pure enum).
 * Scene:    N/A (compilation-only; referenced by all combat subsystems).
 * Ch.Ref:   Ch.2.8 Damage Calculation Pipeline, Ch.9 Combat System.
 */
namespace Zephyr.Core.DamageSystem
{
    /// <summary>
    /// Damage types: AD (attack damage), AP (ability power / magic damage), and true damage.
    /// Armor reduces AD, MagicResist reduces AP, true damage bypasses both.
    /// </summary>
    public enum DamageType
    {
        AD,
        AP,
        True
    }
}
