using UnityEngine;
using Zephyr.Core.DamageSystem;
using Zephyr.Contracts.Damage;
using Zephyr.Contracts.Interfaces;

namespace Zephyr.Core.Combat
{
    /// <summary>Gameplay combat composition root. DamageCalculator remains stateless and is called by attacks.</summary>
    public sealed class CombatSystem : MonoBehaviour
    {
        public bool IsReady { get; private set; }
        public event System.Action<IDamageReceiver, DamageInfo, DamageResolution> DamageApplied;
        private void Awake() => IsReady = true;
        private void OnDisable() => IsReady = false;

        public bool ApplyDamage(IDamageReceiver target, DamageInfo info, Vector2 impactPoint)
        {
            if (!IsReady || target == null) return false;
            DamageResolution resolution = target.ReceiveDamage(info, impactPoint);
            if (!resolution.Accepted) return false;
            DamageApplied?.Invoke(target, info, resolution);
            return true;
        }
    }
}
