using System;
using System.Collections.Generic;
using UnityEngine;
using Zephyr.Core;
using Zephyr.Core.DamageSystem;
using Zephyr.Core.Interfaces;
using Zephyr.Core.Stats;

namespace Zephyr.Core.Health
{
    /// <summary>
    /// Central health component. Implements IDamageable, IHealable, IShieldable.
    /// Wraps StatsCore for stat queries and delegates damage mitigation to
    /// DamageCalculator. Publishes events for UI and audio to subscribe.
    /// </summary>
    [DisallowMultipleComponent]
    public class HealthComponent : MonoBehaviour, IDamageable, IHealable, IShieldable, IStatSource, IMetaStatSource
    {
        [Header("Dependencies")]
        [SerializeField] private StatsCore _statsCore;

        [Header("Debug")]
        [SerializeField] private bool _logDamage = true;

        private readonly List<IDamageFilter> m_damageFilters = new List<IDamageFilter>();

        public float CurrentHp { get; private set; }
        public float MaxHp => _statsCore != null ? _statsCore.GetStatValue(StatType.MaxHp) : 100f;
        public bool IsHealthUnbounded => _statsCore != null &&
                                          _statsCore.GetMetaStat(MetaStatType.UnboundedHealth) > 0.5f;
        public float CurrentShield { get; private set; }
        public float MaxShield { get; private set; }

        public event Action<DamageInfo, DamageResult> DamageTaken;
        public event Action<float> Healed;
        public event Action Died;

        private void Awake()
        {
            if (_statsCore == null)
            {
                _statsCore = GetComponentInParent<StatsCore>();
            }

            if (_statsCore == null)
            {
                _statsCore = GetComponent<StatsCore>();
            }

            CacheDamageFilters();

            CurrentHp = MaxHp;
            CurrentShield = MaxShield;
        }

        private void OnEnable()
        {
            if (CurrentHp <= 0f) CurrentHp = MaxHp;
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            if (CurrentHp <= 0f || IsDamageBlocked(damageInfo))
            {
                return;
            }

            DamageResult result = DamageCalculator.Calculate(this, _statsCore, damageInfo);

            if (result.DealtToShield > 0f)
            {
                float absorbed = ApplyShieldDamage(result.DealtToShield);
                if (absorbed < result.DealtToShield)
                {
                    float overflow = result.DealtToShield - absorbed;
                    CurrentHp = Mathf.Max(IsHealthUnbounded ? 1f : 0f, CurrentHp - overflow);
                }
            }

            if (result.DealtToHp > 0f)
            {
                CurrentHp = Mathf.Max(IsHealthUnbounded ? 1f : 0f, CurrentHp - result.DealtToHp);
            }

#if UNITY_EDITOR
            if (_logDamage)
            {
                Debug.Log($"[Health] Took {result.TotalDamage:F1} damage " +
                          $"(shield:{result.DealtToShield:F1}, hp:{result.DealtToHp:F1}) " +
                          $"from {damageInfo.Source?.name ?? "null"}. HP={CurrentHp:F1}", this);
            }
#endif

            DamageTaken?.Invoke(damageInfo, result);

            if (CurrentHp <= 0f)
            {
                Died?.Invoke();
            }
        }

        public void Heal(float amount)
        {
            if (amount <= 0f || CurrentHp <= 0f) return;

            CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
            Healed?.Invoke(amount);

#if UNITY_EDITOR
            if (_logDamage)
                Debug.Log($"[Health] Healed {amount:F1}. HP={CurrentHp:F1}", this);
#endif
        }

        public void SetShield(float value)
        {
            CurrentShield = Mathf.Clamp(value, 0f, MaxShield);
        }

        public float ApplyShieldDamage(float damage)
        {
            if (damage <= 0f || CurrentShield <= 0f) return 0f;

            float absorbed = Mathf.Min(CurrentShield, damage);
            CurrentShield -= absorbed;
            return absorbed;
        }

        public float GetStatValue(StatType statType)
        {
            return _statsCore != null ? _statsCore.GetStatValue(statType) : 0f;
        }

        public float GetPotential(StatType statType)
        {
            return _statsCore != null ? _statsCore.GetPotential(statType) : 0f;
        }

        public float GetMetaStat(MetaStatType statType)
        {
            return _statsCore != null ? _statsCore.GetMetaStat(statType) : 0f;
        }

        public void ResetHealth()
        {
            CurrentHp = MaxHp;
            CurrentShield = MaxShield;
        }

        private void CacheDamageFilters()
        {
            m_damageFilters.Clear();

            MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IDamageFilter damageFilter)
                {
                    m_damageFilters.Add(damageFilter);
                }
            }
        }

        private bool IsDamageBlocked(DamageInfo damageInfo)
        {
            foreach (IDamageFilter damageFilter in m_damageFilters)
            {
                if (damageFilter.ShouldBlockDamage(damageInfo))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
