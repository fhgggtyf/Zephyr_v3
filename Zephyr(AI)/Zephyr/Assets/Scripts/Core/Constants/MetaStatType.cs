/*
 * MetaStatType.cs
 * ---------------
 * Module:  Core / Constants
 * Purpose: Defines stat types that are permanently set by meta upgrades and do NOT go
 *          through the Potential system. These are one-time permanent values purchased
 *          with Potential currency in the MetaHub. They are flat, permanent values —
 *          no Potential gate, no in-run scaling. Examples: base crit chance, base crit
 *          damage, piercing, max shield, move speed, pickup range, damage variance
 *          reduction, shield coefficient bonus, etc.
 *          Contrast with StatType (10 core stats) which have a Potential layer that
 *          gates in-run gains (Ch.5.3). MetaStatType values are set once and stay.
 * Usage: MetaUpgradeSystem writes these via StatsCore.SetMetaStat(); DamageCalculator
 *          and other subsystems read them via StatsCore.GetMetaStat().
 * Dependencies: None (pure enum).
 * Scene:    N/A (compilation-only; runtime values stored in StatsCore).
 * Ch.Ref:   Ch.5 Stat System, Ch.16 Meta Progression.
 */
namespace Zephyr.Core
{
    /// <summary>
    /// Non-Potential stat types — permanently set by meta upgrades.
    /// These values are flat, permanent, and never go through the Potential gate.
    /// Contrast with StatType which has a dual-layer Base+Potential model.
    /// </summary>
    public enum MetaStatType
    {
        /// <summary>Base critical chance (decimal, e.g., 0.05 = 5%).</summary>
        CritChance,

        /// <summary>Base critical damage multiplier (e.g., 1.5 = +50% damage on crit).
        /// Added on top of GameConstants.Combat.CriticalDamageMultiplier.</summary>
        CritDamage,

        /// <summary>Base piercing value (decimal, e.g., 0.1 = 10% armor/MR pierce).</summary>
        Piercing,

        /// <summary>Max shield value (flat number, not gated by Potential).</summary>
        MaxShield,

        /// <summary>Bonus shield coefficient P (added to weapon-inherent P, Ch.4.7).</summary>
        ShieldCoeffBonus,

        /// <summary>Move speed multiplier (e.g., 0.1 = +10% move speed).</summary>
        MoveSpeed,

        /// <summary>Weapon/currency pickup range multiplier (e.g., 0.2 = +20% range).</summary>
        PickupRange,

        /// <summary>Reduces damage variance (e.g., 0.5 = half the ±5% variance range).</summary>
        VarianceReduction,

        /// <summary>Luck bonus for rare drop rates (e.g., 0.05 = +5% rare drop chance).</summary>
        LuckBonus,

        /// <summary>Damage reduction % (e.g., 0.05 = 5% less damage taken).</summary>
        DamageReduction,

        /// <summary>Additional run-entry Potential budget (increases max Potential per run).</summary>
        PotentialBudget,

        /// <summary>Player-allocatable share multiplier for in-run stat gains.</summary>
        ShareMultiplier
    }
}