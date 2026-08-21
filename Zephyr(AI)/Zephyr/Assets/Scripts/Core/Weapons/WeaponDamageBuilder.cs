using UnityEngine;
using Zephyr.Core.DamageSystem;
using Zephyr.Core.Interfaces;
using Zephyr.Core.Stats;

namespace Zephyr.Core.Weapons
{
    public static class WeaponDamageBuilder
    {
        public static ResolvedWeaponStats Resolve(
            WeaponSO weapon,
            IStatSource statSource,
            int comboIndex,
            float attackDamageMultiplier = 1f)
        {
            StatType scalingStat = weapon.DamageType == DamageType.AP ? StatType.MagicAttack : StatType.Attack;
            float sourceAttack = statSource?.GetStatValue(scalingStat) ?? 0f;
            float comboMultiplier = weapon.Combo?.GetStep(comboIndex)?.DamageMultiplier ?? 1f;
            float damage = (weapon.BaseFlatDamage + sourceAttack * weapon.BasePercentDamage)
                * comboMultiplier
                * Mathf.Max(0f, attackDamageMultiplier);

            float[] multipliers = null;
            if (statSource is StatsCore statsCore)
            {
                multipliers = statsCore.GetMultipliersForWeapon(
                    weapon.Category,
                    weapon.ElementalType,
                    weapon.Rarity);
            }

            return new ResolvedWeaponStats(
                damage,
                weapon.DamageType,
                weapon.ShieldCoeff,
                weapon.BaseCritChance,
                weapon.BaseCritMultiplier,
                weapon.BaseBackstabMultiplier,
                weapon.BaseAttackSpeed,
                weapon.BaseFlatPen,
                weapon.BasePercentPen,
                weapon.Category,
                weapon.ElementalType,
                weapon.Rarity,
                multipliers);
        }

        public static DamageInfo Build(
            WeaponSO weapon,
            IStatSource statSource,
            int comboIndex,
            GameObject source,
            Vector2 sourcePosition,
            float attackDamageMultiplier = 1f,
            bool? isCritical = null,
            bool isBackstab = false)
        {
            ResolvedWeaponStats stats = Resolve(weapon, statSource, comboIndex, attackDamageMultiplier);
            bool resolvedCritical = isCritical ?? Random.value < Mathf.Clamp01(stats.CriticalChance);

            return DamageInfo.Create(
                stats.Damage,
                stats.DamageType,
                stats.Category,
                stats.Element,
                stats.Rarity,
                stats.AdditionalMultipliers,
                resolvedCritical,
                isBackstab,
                stats.Piercing,
                stats.ShieldCoeff,
                sourcePosition,
                source,
                stats.CriticalMultiplier,
                stats.BackstabMultiplier);
        }
    }
}
