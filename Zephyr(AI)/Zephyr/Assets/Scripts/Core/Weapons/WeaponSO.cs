using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zephyr.Core.DamageSystem;

namespace Zephyr.Core.Weapons
{
    [CreateAssetMenu(fileName = "Weapon", menuName = "Zephyr/Weapons/Weapon")]
    public class WeaponSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string m_weaponId;
        [SerializeField] private string m_displayName;
        [SerializeField] private string m_displayNameLocalizationKey;
        [TextArea] [SerializeField] private string m_description;
        [SerializeField] private Sprite m_icon;
        [SerializeField] private WeaponRarity m_rarity;
        [SerializeField] private WeaponCategory m_category = WeaponCategory.Melee;

        [Header("Damage")]
        [SerializeField] private DamageType m_damageType = DamageType.AD;
        [Min(0f)] [SerializeField] private float m_baseFlatDamage;
        [Min(0f)] [SerializeField] private float m_basePercentDamage = 1f;
        [Min(0f)] [SerializeField] private float m_shieldCoeff = 1f;
        [SerializeField] private ElementalType m_elementalType = ElementalType.Physical;
        [Range(0f, 1f)] [SerializeField] private float m_baseCritChance;
        [Min(1f)] [SerializeField] private float m_baseCritMultiplier = 1.5f;
        [Min(1f)] [SerializeField] private float m_baseBackstabMultiplier = 1.5f;
        [Min(0.01f)] [SerializeField] private float m_baseAttackSpeed = 1f;
        [Min(0f)] [SerializeField] private float m_baseFlatPen;
        [Range(0f, 1f)] [SerializeField] private float m_basePercentPen;

        [Header("Attack")]
        [SerializeField] private ComboData m_combo = new ComboData();
        [SerializeField] private AttackResourceCost m_attackResourceCost;
        [SerializeField] private RuntimeAnimatorController m_animatorController;

        [Header("Ranged Extension")]
        [SerializeField] private GameObject m_projectilePrefab;
        [Min(0f)] [SerializeField] private float m_projectileSpeed;
        [Min(0f)] [SerializeField] private float m_projectileLifetime;
        [Min(0f)] [SerializeField] private float m_projectileRange;

        [SerializeReference] private List<ComponentData> m_componentData = new List<ComponentData>();

        public string WeaponId => m_weaponId;
        public string DisplayName => m_displayName;
        public string DisplayNameLocalizationKey => m_displayNameLocalizationKey;
        public string Description => m_description;
        public Sprite Icon => m_icon;
        public WeaponRarity Rarity => m_rarity;
        public WeaponCategory Category => m_category;
        public DamageType DamageType => m_damageType;
        public float BaseFlatDamage => m_baseFlatDamage;
        public float BasePercentDamage => m_basePercentDamage;
        public float ShieldCoeff => m_shieldCoeff;
        public ElementalType ElementalType => m_elementalType;
        public float BaseCritChance => m_baseCritChance;
        public float BaseCritMultiplier => m_baseCritMultiplier;
        public float BaseBackstabMultiplier => m_baseBackstabMultiplier;
        public float BaseAttackSpeed => m_baseAttackSpeed;
        public float BaseFlatPen => m_baseFlatPen;
        public float BasePercentPen => m_basePercentPen;
        public ComboData Combo => m_combo;
        public AttackResourceCost AttackResourceCost => m_attackResourceCost;
        public RuntimeAnimatorController AnimatorController => m_animatorController;
        public GameObject ProjectilePrefab => m_projectilePrefab;
        public float ProjectileSpeed => m_projectileSpeed;
        public float ProjectileLifetime => m_projectileLifetime;
        public float ProjectileRange => m_projectileRange;
        public IReadOnlyList<ComponentData> Components => m_componentData;

        public T GetData<T>() where T : ComponentData
        {
            return m_componentData.OfType<T>().FirstOrDefault();
        }

        private void OnValidate()
        {
            int attackCount = m_combo?.StepCount ?? 0;
            foreach (ComponentData data in m_componentData)
            {
                data?.SynchronizeAttackData(attackCount);
            }
        }
    }
}
