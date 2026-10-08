/*
 * DamageCalculator.cs
 * -------------------
 * Module:  Core / DamageSystem
 * Purpose: Centralized stateless damage calculation engine. Computes final damage values
 *          after applying all modifiers in the correct order (Ch.4.6 pipeline):
 *          1. Critical hit (multiplicative with backstab, pre-mitigation)
 *          2. Backstab (multiplicative with crit, pre-mitigation)
 *          3. Additional multipliers (post-backstab, pre-variance, category-based
 *             buffs/debuffs from attacker's in-run buffs and meta upgrades).
 *             Two-level stacking: (a) same-category buffs sum additively in StatsCore
 *             (meta + in-run relic buffs of the same category are added together),
 *             (b) different-category sums are applied multiplicatively here:
 *             damage *= (1 + sum_melee) * (1 + sum_fire) * (1 + sum_rare) * ...
 *             Each multiplier is a decimal (+5% = 0.05f, -10% debuff = -0.10f).
 *          4. Variance (±5% normal distribution, post-multiplier)
 *          5. Mitigation (armor for AD, MR for AP, piercing)
 *          6. Shield-break split (Ch.4.7, locked formula):
 *             P = ShieldCoeff (weapon-inherent, ≥ 0, no upper bound)
 *             S = CurrentShield of target
 *             Case A (P < 1):
 *               shieldDamage = D × P
 *               if shieldDamage ≤ S:  hpDamage = D × (1−P) × 0.3  (0.3 leak)
 *               else:                  hpDamage = D × (1−P) + (D×P − S)
 *             Case B (P ≥ 1):
 *               shieldDamage = D × P
 *               if shieldDamage ≤ S:  hpDamage = 0  (full absorb)
 *               else:                  hpDamage = (D×P − S) / P
 *             P = 0 → Case A: shield takes 0, HP takes 0.3D.
 *          Returns a DamageResult with DealtToShield, DealtToHp, ShieldBroken.
 * Dependencies: IDamageable, IStatSource, IShieldable (Interfaces),
 *               GameConstants (tuning values), DamageInfo, DamageResult (value types).
 * Scene:    GameManager (runtime, called during combat by ProjectileSystem and melee hit logic).
 * Ch.Ref:   Ch.2.7 Core Data Structures, Ch.4.6-4.7 Damage Pipeline & Shield-Break, Ch.9 Combat.
 */
using UnityEngine;
using Zephyr.Core;
using Zephyr.Core.Interfaces;
using Zephyr.Core.Stats;

namespace Zephyr.Core.DamageSystem
{
    /// <summary>
    /// Central damage calculator. Stateless — call as a static method.
    /// Handles: crit, backstab, additional multipliers (category-based buffs),
    /// variance, mitigation (armor/MR), and shield-break split (Ch.4.7).
    /// Returns a DamageResult with shield/hp split for the caller to apply.
    /// </summary>
    public static class DamageCalculator
    {
        /// <summary>
        /// Compute final damage after all modifiers. Returns a DamageResult with the
        /// split between shield and health damage.
        /// Pipeline (Ch.4.6): crit → backstab → additional multipliers → variance →
        ///           mitigation → shield-break split (Ch.4.7).
        /// </summary>
        /// <param name="target">The damageable target (must implement IDamageable).</param>
        /// <param name="targetStats">The target's stat source (for mitigation calculation).</param>
        /// <param name="info">The damage payload with all metadata.</param>
        /// <returns>DamageResult with DealtToShield, DealtToHp, and ShieldBroken flag.</returns>
        public static DamageResult Calculate(IDamageable target, IStatSource targetStats, DamageInfo info)
        {
            float damage = info.Damage;

            // 1. Apply critical hit (multiplicative, pre-mitigation)
            if (info.IsCritical)
            {
                damage *= info.CriticalMultiplier;
            }

            // 2. Apply backstab (multiplicative with crit, pre-mitigation)
            if (info.IsBackstab)
            {
                damage *= info.BackstabMultiplier;
            }

            // 3. Apply additional multipliers (category-based buffs/debuffs)
            damage = ApplyAdditionalMultipliers(damage, info.AdditionalMultipliers);

            // 4. Apply variance
            damage = ApplyVariance(damage);

            // 5. Apply mitigation
            float mitigated = ApplyMitigation(damage, info.Type, targetStats, info.Piercing);

            // 5.5 Apply target-side scene/challenge damage reduction.
            // The modifier is read through HealthComponent when available so
            // scene rules can affect the actual damage receiver without
            // changing the existing shield split contract.
            var metaSource = target as IMetaStatSource ?? targetStats as IMetaStatSource;
            float damageReduction = metaSource?.GetMetaStat(MetaStatType.DamageReduction) ?? 0f;
            mitigated *= 1f - Mathf.Clamp01(damageReduction);

            // 6. Apply shield-break split (Ch.4.7 locked formula)
            var result = ApplyShieldSplit(mitigated, info.ShieldCoeff, target);

            return result;
        }

        /// <summary>
        /// Mitigate damage by armor (AD) or magic resist (AP), with piercing.
        /// True damage passes through unchanged.
        /// </summary>
        private static float ApplyMitigation(float damage, DamageType type, IStatSource stats, float pierce)
        {
            float effectivePierce = Mathf.Clamp01(pierce);

            switch (type)
            {
                case DamageType.AD:
                    float armor = stats?.GetStatValue(StatType.Armor) ?? 0f;
                    return damage * (1f - Mathf.Clamp01(armor / (armor + 100f)) * (1f - effectivePierce));

                case DamageType.AP:
                    float mr = stats?.GetStatValue(StatType.MagicResist) ?? 0f;
                    return damage * (1f - Mathf.Clamp01(mr / (mr + 100f)) * (1f - effectivePierce));

                case DamageType.True:
                default:
                    return damage;
            }
        }

        /// <summary>
        /// Apply ±5% normal-distribution variance (Luck-weighted mean, non-deterministic per hit).
        /// </summary>
        private static float ApplyVariance(float damage)
        {
            float variance = 1f + Random.Range(-3f, 3f) * GameConstants.Damage.DamageVarianceStdDev;
            return damage * variance;
        }

        /// <summary>
        /// Apply additional multipliers multiplicatively. Each multiplier is the
        /// already-summed total for its category (meta + in-run sources were added
        /// together in StatsCore). Cross-category stacking is multiplicative:
        /// damage *= (1 + m1) * (1 + m2) * ... where m1, m2 are per-category sums
        /// (e.g., sum_melee=0.05, sum_fire=0.15).
        /// Returns the input unchanged if the array is null or empty.
        /// </summary>
        private static float ApplyAdditionalMultipliers(float damage, float[] multipliers)
        {
            if (multipliers == null || multipliers.Length == 0)
                return damage;

            float result = damage;
            for (int i = 0; i < multipliers.Length; i++)
                result *= 1f + multipliers[i];

            return result;
        }

        /// <summary>
        /// Shield-break split per Ch.4.7 (locked formula).
        /// Given D (post-mitigation), P (ShieldCoeff, weapon-inherent ≥ 0), S (CurrentShield):
        ///
        /// Case A — P < 1:
        ///   shieldDamage = D × P
        ///   if shieldDamage ≤ S:   hpDamage = D × (1−P) × 0.3   (0.3 leak mechanic)
        ///   else:                   hpDamage = D × (1−P) + (D×P − S)
        ///
        /// Case B — P ≥ 1:
        ///   shieldDamage = D × P
        ///   if shieldDamage ≤ S:   hpDamage = 0                  (full absorb)
        ///   else:                   hpDamage = (D×P − S) / P
        ///
        /// P = 0 falls under Case A: shield takes 0, HP takes 0.3D.
        /// If target has no shield (IShieldable null or S ≤ 0), all damage goes to HP.
        /// </summary>
        private static DamageResult ApplyShieldSplit(float D, float P, IDamageable target)
        {
            // Check if target has a shield
            var shieldable = target as IShieldable;
            if (shieldable == null)
            {
                // No shield implementation — all post-mitigation damage goes to HP
                return new DamageResult(0f, D, false);
            }

            float S = shieldable.CurrentShield;
            if (S <= 0f)
            {
                // Shield pool exhausted — all damage goes to HP
                return new DamageResult(0f, D, false);
            }

            // Ensure P is non-negative (weapon-inherent, ≥ 0 by design)
            P = Mathf.Max(0f, P);

            if (P < 1f)
            {
                // Case A — P < 1
                float shieldDamage = D * P;
                if (shieldDamage <= S)
                {
                    // Shield holds — 0.3 leak mechanic (only 30% of non-shield portion reaches HP)
                    float hpDamage = D * (1f - P) * 0.3f;
                    return new DamageResult(shieldDamage, hpDamage, false);
                }
                else
                {
                    // Shield breaks — full non-shield portion + overflow to HP (no 0.3 leak)
                    float hpDamage = D * (1f - P) + (shieldDamage - S);
                    return new DamageResult(S, hpDamage, true);
                }
            }
            else
            {
                // Case B — P ≥ 1
                float shieldDamage = D * P;
                if (shieldDamage <= S)
                {
                    // Shield holds — full absorb, HP takes 0
                    return new DamageResult(shieldDamage, 0f, false);
                }
                else
                {
                    // Shield breaks — overflow divided by P
                    float hpDamage = (shieldDamage - S) / P;
                    return new DamageResult(S, hpDamage, true);
                }
            }
        }
    }
}
