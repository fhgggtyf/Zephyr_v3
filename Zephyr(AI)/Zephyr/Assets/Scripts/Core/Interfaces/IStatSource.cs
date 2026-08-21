/*
 * IStatSource.cs
 * --------------
 * Module:  Core / Interfaces
 * Purpose: Contract for any component that provides stat values for damage calculation.
 *          Implemented by StatsCore on both player and enemy. GetStatValue(StatType) returns
 *          the full aggregated value: Permanent Base + In-Run Gains + In-Run Modifiers
 *          + Temporary Modifiers (4-layer model). GetPotential(StatType) returns the
 *          current run's Potential value (re-rolled each run from total budget).
 * Dependencies: StatType (enum), Zephyr.Core namespace.
 * Scene:    GameManager (implemented by StatsCore on entity prefabs).
 * Ch.Ref:   Ch.5 Stat System, Ch.5.3 Potential Mechanic, Ch.2.8 Damage Calculation Pipeline.
 */
using Zephyr.Core;

namespace Zephyr.Core.Interfaces
{
    /// <summary>
    /// Source of stat values (4-layer aggregation: Base + RunGains + Modifiers + TempModifiers).
    /// Implemented by StatsCore on player/enemy. GetPotential returns the per-run Potential
    /// value (not the total budget — that's MetaStatType.PotentialBudget).
    /// </summary>
    public interface IStatSource
    {
        float GetStatValue(StatType statType);
        float GetPotential(StatType statType);
    }
}
