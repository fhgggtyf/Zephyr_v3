/*
 * DamageInfo.cs
 * -------------
 * Module:  Core / DamageSystem
 * Purpose: Immutable readonly struct carrying all damage-related data from source to target.
 *          Serves as the standard payload for IDamageable.TakeDamage and the damage pipeline.
 *          Contains raw damage, type (AD/AP/True), critical/backstab flags, piercing value,
 *          source identity (position + GameObject), and optional weapon metadata
 *          (category, elemental type, rarity) for multiplier lookups. Carries an optional
 *          AdditionalMultipliers array — percentage-based post-backstab,
 *          pre-variance multipliers (e.g., +5% melee buff, +10% fire elemental buff)
 *          applied multiplicatively by DamageCalculator after backstab and crit.
 *          Two-level stacking: (a) same-category buffs sum additively in StatsCore
 *          (meta + in-run relic/weapon buffs of the same category → one summed value),
 *          (b) different-category sums are applied multiplicatively here:
 *          damage *= (1 + sum_melee) * (1 + sum_fire) * (1 + sum_rare) * ...
 *          Values are expressed as decimals: +5% = 0.05f, -10% debuff = -0.10f.
 *          ShieldCoeff (P, 破盾系数): weapon-inherent value (≥ 0, no upper bound)
 *          that controls how damage is split between shield and health per Ch.4.7.
 *          Case A (P < 1): shield takes D×P, HP takes D×(1−P)×0.3 if shield holds,
 *          or D×(1−P)+(D×P−S) if shield breaks (0.3 leak mechanic).
 *          Case B (P ≥ 1): shield takes D×P, HP takes 0 if shield holds,
 *          or (D×P−S)/P if shield breaks (overflow divided by P).
 * Dependencies: UnityEngine.Vector2, UnityEngine.GameObject, WeaponCategory, ElementalType,
 *               WeaponRarity, DamageType (enums in same namespace).
 * Scene:    GameManager (passed through the event bus and direct method calls during combat).
 * Ch.Ref:   Ch.2.7 Core Data Structures, Ch.4.6-4.7 Damage Pipeline & Shield-Break.
 */
using UnityEngine;

namespace Zephyr.Core.DamageSystem
{
    /// <summary>
    /// Immutable damage info. Should be a readonly struct in production to prevent leaks.
    /// </summary>
    public readonly struct DamageInfo
    {
        public readonly float Damage;
        public readonly DamageType Type;
        public readonly bool IsCritical;
        public readonly bool IsBackstab;
        public readonly float CriticalMultiplier;
        public readonly float BackstabMultiplier;
        public readonly float Piercing;
        public readonly Vector2 SourcePosition;
        public readonly GameObject Source;

        /// <summary>
        /// Shield coefficient P (破盾系数). Weapon-inherent value ≥ 0, no upper bound.
        /// Controls the shield-split formula in DamageCalculator per Ch.4.7.
        /// P < 1: shield takes D×P, HP takes D×(1−P)×0.3 if shield holds;
        ///        HP takes D×(1−P)+(D×P−S) if shield breaks.
        /// P ≥ 1: shield takes D×P, HP takes 0 if shield holds;
        ///        HP takes (D×P−S)/P if shield breaks.
        /// P = 0: weapon has no shield interaction; HP takes 0.3D (leak).
        /// Default is 0 (no shield interaction).
        /// </summary>
        public readonly float ShieldCoeff;

        /// <summary>
        /// Physical category of the source weapon (Melee/Ranged). Null for non-weapon sources.
        /// Used by the attack creator to decide which category multipliers to populate.
        /// </summary>
        public readonly WeaponCategory? Category;

        /// <summary>
        /// Elemental affinity of the source weapon. Null for non-elemental or non-weapon sources.
        /// Used by the attack creator to decide which elemental multipliers to populate.
        /// </summary>
        public readonly ElementalType? Element;

        /// <summary>
        /// Rarity tier of the source weapon. Null for non-weapon sources.
        /// Used by the attack creator to decide which rarity multipliers to populate.
        /// </summary>
        public readonly WeaponRarity? Rarity;

        /// <summary>
        /// Percentage-based post-backstab, pre-variance multipliers applied multiplicatively
        /// after crit/backstab but before variance.
        /// Each entry is a decimal: +5% = 0.05f, -10% = -0.10f.
        /// Example: [0.05f, 0.10f, 0.10f] → damage *= 1.05f * 1.10f * 1.10f.
        /// Null or empty array means no additional multipliers.
        /// Populated by the attack creator (e.g., ProjectileSystem) from the attacker's
        /// category-based buffs and meta upgrades.
        /// </summary>
        public readonly float[] AdditionalMultipliers;

        public DamageInfo(
            float damage,
            DamageType type,
            bool isCritical = false,
            bool isBackstab = false,
            float piercing = 0f,
            Vector2 sourcePosition = default,
            GameObject source = null,
            float shieldCoeff = 0f,
            WeaponCategory? category = null,
            ElementalType? element = null,
            WeaponRarity? rarity = null,
            float[] additionalMultipliers = null,
            float criticalMultiplier = 1f,
            float backstabMultiplier = 1f)
        {
            Damage = damage;
            Type = type;
            IsCritical = isCritical;
            IsBackstab = isBackstab;
            CriticalMultiplier = Mathf.Max(0f, criticalMultiplier);
            BackstabMultiplier = Mathf.Max(0f, backstabMultiplier);
            Piercing = piercing;
            SourcePosition = sourcePosition;
            Source = source;
            ShieldCoeff = shieldCoeff;
            Category = category;
            Element = element;
            Rarity = rarity;
            AdditionalMultipliers = additionalMultipliers;
        }

        /// <summary>
        /// Factory helper that creates a DamageInfo with weapon metadata and pre-resolved
        /// additional multipliers. Centralizes the common pattern of constructing a DamageInfo
        /// with weapon properties and category-based buff values.
        /// </summary>
        /// <param name="damage">Base damage value (before crit/backstab).</param>
        /// <param name="type">Damage type (AD/AP/True).</param>
        /// <param name="category">Weapon physical category (Melee/Ranged), or null.</param>
        /// <param name="element">Weapon elemental type, or null.</param>
        /// <param name="rarity">Weapon rarity tier, or null.</param>
        /// <param name="additionalMultipliers">
        /// Pre-resolved post-backstab, pre-variance multipliers from the attacker's StatsCore.
        /// Use StatsCore.GetMultipliersForWeapon() to resolve these.
        /// Each value is a decimal: +5% = 0.05f, -10% debuff = -0.10f.
        /// </param>
        /// <param name="isCritical">Whether this is a critical hit.</param>
        /// <param name="isBackstab">Whether this is a backstab.</param>
        /// <param name="piercing">Piercing value (0-1).</param>
        /// <param name="shieldCoeff">Shield coefficient P (破盾系数, ≥ 0). Weapon-inherent value.</param>
        /// <param name="sourcePosition">Position of the attack source.</param>
        /// <param name="source">GameObject that is the source of the attack.</param>
        public static DamageInfo Create(
            float damage,
            DamageType type,
            WeaponCategory? category = null,
            ElementalType? element = null,
            WeaponRarity? rarity = null,
            float[] additionalMultipliers = null,
            bool isCritical = false,
            bool isBackstab = false,
            float piercing = 0f,
            float shieldCoeff = 0f,
            Vector2 sourcePosition = default,
            GameObject source = null,
            float criticalMultiplier = 1f,
            float backstabMultiplier = 1f)
        {
            return new DamageInfo(
                damage, type,
                isCritical, isBackstab, piercing,
                sourcePosition, source,
                shieldCoeff,
                category, element, rarity,
                additionalMultipliers,
                criticalMultiplier, backstabMultiplier);
        }
    }
}