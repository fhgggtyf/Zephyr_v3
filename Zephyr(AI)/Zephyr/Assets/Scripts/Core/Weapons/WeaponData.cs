using System;
using UnityEngine;
using Zephyr.Core.DamageSystem;

namespace Zephyr.Core.Weapons
{
    public enum WeaponSlot
    {
        Primary,
        Secondary
    }

    public enum AttackResourceType
    {
        None,
        Stamina,
        Energy
    }

    [Serializable]
    public class ComboStep
    {
        [Header("Weapon Motion")]
        [SerializeField] private AnimationClip m_animationClip;

        [Header("Player Presentation")]
        [Tooltip("Animator state name played on the player's SpriteAnimator for this combo step.")]
        [SerializeField] private string m_playerAnimationName;
        [Tooltip("Placeholder clip in the player controller replaced by the weapon-specific body clip.")]
        [SerializeField] private AnimationClip m_playerAnimationPlaceholder;
        [Tooltip("Weapon-specific player body animation clip for this combo step.")]
        [SerializeField] private AnimationClip m_playerAnimationClip;

        [Min(0f)] [SerializeField] private float m_damageMultiplier = 1f;
        [Min(0.01f)] [SerializeField] private float m_durationInSeconds = 0.5f;
        [Min(0f)] [SerializeField] private float m_comboResetDelayInSeconds = 0.8f;

        public AnimationClip AnimationClip => m_animationClip;
        public string PlayerAnimationName => m_playerAnimationName;
        public AnimationClip PlayerAnimationPlaceholder => m_playerAnimationPlaceholder;
        public AnimationClip PlayerAnimationClip => m_playerAnimationClip;
        public float DamageMultiplier => m_damageMultiplier;
        public float DurationInSeconds => m_durationInSeconds;
        public float ComboResetDelayInSeconds => m_comboResetDelayInSeconds;
    }

    [Serializable]
    public class ComboData
    {
        [SerializeField] private ComboStep[] m_steps = { new ComboStep() };

        public int StepCount => m_steps?.Length ?? 0;

        public ComboStep GetStep(int index)
        {
            if (StepCount == 0) return null;

            return m_steps[Mathf.Clamp(index, 0, StepCount - 1)];
        }
    }

    [Serializable]
    public struct AttackResourceCost
    {
        [SerializeField] private AttackResourceType m_resourceType;
        [Min(0f)] [SerializeField] private float m_amount;

        public AttackResourceType ResourceType => m_resourceType;
        public float Amount => m_amount;
    }

    public interface IAttackResourceConsumer
    {
        bool CanAffordAttackResource(AttackResourceCost cost);
        bool TryConsumeAttackResource(AttackResourceCost cost);
    }

    public readonly struct AttackContext
    {
        public WeaponSO Weapon { get; }
        public WeaponSlot Slot { get; }
        public int ComboIndex { get; }
        public float StartedAt { get; }
        public float EndsAt { get; }
        /// <summary>
        /// Shared playback window used by both the player-body and weapon
        /// animation so their independently authored clips finish together.
        /// </summary>
        public float Duration => Mathf.Max(0.01f, EndsAt - StartedAt);

        public AttackContext(WeaponSO weapon, WeaponSlot slot, int comboIndex, float startedAt, float endsAt)
        {
            Weapon = weapon;
            Slot = slot;
            ComboIndex = comboIndex;
            StartedAt = startedAt;
            EndsAt = endsAt;
        }
    }

    [Serializable]
    public sealed class WeaponInstance
    {
        [SerializeField] private WeaponSO m_weapon;

        public WeaponSO Weapon => m_weapon;

        public WeaponInstance(WeaponSO weapon)
        {
            m_weapon = weapon;
        }
    }

    public readonly struct ResolvedWeaponStats
    {
        public readonly float Damage;
        public readonly DamageType DamageType;
        public readonly float ShieldCoeff;
        public readonly float CriticalChance;
        public readonly float CriticalMultiplier;
        public readonly float BackstabMultiplier;
        public readonly float AttackSpeed;
        public readonly float FlatPenetration;
        public readonly float PercentPenetration;
        public readonly float Piercing;
        public readonly WeaponCategory Category;
        public readonly ElementalType Element;
        public readonly WeaponRarity Rarity;
        public readonly float[] AdditionalMultipliers;

        public ResolvedWeaponStats(
            float damage,
            DamageType damageType,
            float shieldCoeff,
            float criticalChance,
            float criticalMultiplier,
            float backstabMultiplier,
            float attackSpeed,
            float flatPenetration,
            float percentPenetration,
            WeaponCategory category,
            ElementalType element,
            WeaponRarity rarity,
            float[] additionalMultipliers)
        {
            Damage = damage;
            DamageType = damageType;
            ShieldCoeff = shieldCoeff;
            CriticalChance = criticalChance;
            CriticalMultiplier = criticalMultiplier;
            BackstabMultiplier = backstabMultiplier;
            AttackSpeed = attackSpeed;
            FlatPenetration = flatPenetration;
            PercentPenetration = percentPenetration;
            Piercing = Mathf.Clamp01(percentPenetration + flatPenetration / 100f);
            Category = category;
            Element = element;
            Rarity = rarity;
            AdditionalMultipliers = additionalMultipliers;
        }
    }

    [Serializable]
    public class WeaponSet
    {
        [SerializeField] private WeaponSO m_primary;
        [SerializeField] private WeaponSO m_secondary;

        public WeaponSO Primary => m_primary;
        public WeaponSO Secondary => m_secondary;

        public WeaponSO GetWeapon(WeaponSlot slot)
        {
            return slot == WeaponSlot.Primary ? m_primary : m_secondary;
        }

        public void SetWeapon(WeaponSlot slot, WeaponSO weapon)
        {
            if (slot == WeaponSlot.Primary)
            {
                m_primary = weapon;
                return;
            }

            m_secondary = weapon;
        }
    }
}
