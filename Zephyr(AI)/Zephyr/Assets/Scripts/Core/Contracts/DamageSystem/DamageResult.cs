/*
 * DamageResult.cs
 * ---------------
 * Module:  Core / DamageSystem
 * Purpose: Immutable readonly struct holding the final split of computed damage
 *          between health and shield after the full damage pipeline (Ch.4.6-4.7).
 *          Returned by DamageCalculator.Calculate() so the caller (HealthComponent)
 *          can apply health damage, absorb shield damage, and trigger events.
 *          DealtToShield: amount of damage absorbed by the target's shield.
 *          DealtToHp: amount of damage that goes through to health (after shield split).
 *          ShieldBroken: true if the target's shield was fully consumed by this hit
 *          (shield broke — triggers a shield-break event for VFX/sound).
 *          TargetKilled: set to true after the caller (HealthComponent) checks if
 *          HP reached zero; DamageCalculator does not determine this.
 * Dependencies: None.
 * Scene:    GameManager (returned by DamageCalculator, consumed by HealthComponent).
 * Ch.Ref:   Ch.2.7 Core Data Structures (DamageResult), Ch.4.6-4.7 Pipeline & Shield.
 */

namespace Zephyr.Core.DamageSystem
{
    /// <summary>
    /// Final result of a damage calculation, split between shield and health.
    /// Matches Ch.2.7 DamageResult definition.
    /// </summary>
    public readonly struct DamageResult
    {
        public readonly float DealtToShield;
        public readonly float DealtToHp;
        public readonly bool ShieldBroken;
        public readonly bool TargetKilled;

        public DamageResult(float dealtToShield, float dealtToHp, bool shieldBroken, bool targetKilled = false)
        {
            DealtToShield = dealtToShield;
            DealtToHp = dealtToHp;
            ShieldBroken = shieldBroken;
            TargetKilled = targetKilled;
        }

        /// <summary>
        /// Total post-mitigation, post-shield-split damage applied.
        /// </summary>
        public float TotalDamage => DealtToShield + DealtToHp;
    }
}