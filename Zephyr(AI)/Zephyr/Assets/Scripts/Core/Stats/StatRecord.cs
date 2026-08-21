/*
 * StatRecord.cs
 * -------------
 * Module:  Core / Stats
 * Purpose: Value type representing a single stat's per-run data within the 4-layer stat
 *          model. Base = permanent meta-upgrade value (Layer 1a, profile lifetime, never reset).
 *          Potential = per-run allocation (re-rolled each run via InitializeRunPotentials(),
 *          range 0-10). GetScaleMultiplier() returns Potential / PotentialMax (0.0 to 1.0),
 *          and GetScaledGain(rawGain) applies this multiplier to compute how much of a raw
 *          in-run gain actually applies to Layer 2 (In-Run Gains).
 *          This struct is mutated via the copy-modify-write pattern in StatsCore because
 *          Dictionary<,>.this[] returns a value type that cannot be modified directly.
 * Dependencies: GameConstants.Stats.PotentialMax.
 * Scene:    GameManager (runtime, one StatRecord per stat type per entity).
 * Ch.Ref:   Ch.5 Stat System, Ch.5.3 Potential Mechanic.
 */
namespace Zephyr.Core.Stats
{
    /// <summary>
    /// Stat record holding permanent Base + per-run Potential.
    /// Base: permanent meta-upgrade value (profile lifetime, never reset).
    /// Potential: per-run allocation, controls in-run gain scaling.
    /// Scaled gain = rawGain × (Potential / PotentialMax).
    /// </summary>
    public struct StatRecord
    {
        /// <summary>Permanent meta-upgrade value. Profile lifetime, never reset.</summary>
        public float Base;

        /// <summary>Per-run Potential allocation (0-10). Re-rolled each run.</summary>
        public float Potential;

        public float GetScaleMultiplier()
        {
            return Potential / (float)GameConstants.Stats.PotentialMax;
        }

        public float GetScaledGain(float rawGain)
        {
            return rawGain * GetScaleMultiplier();
        }
    }
}
