/*
 * StatType.cs
 * -----------
 * Module:  Core / Constants
 * Purpose: Defines the 10 core stat types that carry Potential values (0-10) in the
 *          dual-layer stat system. These are the only stats that are potential-scaled;
 *          other stats (penetration, crit chance, crit damage) exist only as meta-upgrades.
 *          Each stat maps to a corresponding field in StatsCore and is used by
 *          DamageCalculator and other combat subsystems.
 * Dependencies: None (pure enum). Referenced by StatsCore, DamageCalculator.
 * Scene:    N/A (compilation-only; runtime values stored in StatsCore).
 * Ch.Ref:   Ch.5 Stat System, Ch.5.10 Luck Mechanic.
 */
namespace Zephyr.Core
{
    /// <summary>
    /// Stat types that carry Potential (0-10). Only these 10 core stats are potential-scaled.
    /// Other stats (penetration, crit chance, crit damage) are meta-only.
    /// </summary>
    public enum StatType
    {
        Attack,
        MagicAttack,
        Armor,
        MagicResist,
        MaxHp,
        Stamina,
        Luck,
        Tenacity,
        Energy,
        AttackSpeed
    }
}
