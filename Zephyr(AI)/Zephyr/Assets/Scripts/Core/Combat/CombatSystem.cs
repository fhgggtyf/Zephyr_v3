using UnityEngine;
using Zephyr.Core.DamageSystem;
using Zephyr.Core.Interfaces;

namespace Zephyr.Core.Combat
{
    /// <summary>Gameplay combat composition root. DamageCalculator remains stateless and is called by attacks.</summary>
    public sealed class CombatSystem : MonoBehaviour
    {
        public bool IsReady { get; private set; }
        public event System.Action<IDamageable, DamageInfo> DamageApplied;
        private void Awake() => IsReady = true;
        private void OnDisable() => IsReady = false;

        public bool ApplyDamage(IDamageable target, DamageInfo info)
        {
            if (!IsReady || target == null) return false;
            target.TakeDamage(info);
            DamageApplied?.Invoke(target, info);
            return true;
        }
    }
}
