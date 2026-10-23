using UnityEngine;
using Zephyr.Contracts.Damage;
using Zephyr.Contracts.Interfaces;

namespace Zephyr.Core.Health
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HealthComponent))]
    public sealed class IndividualDamageHandler : MonoBehaviour,
        IDamageReceiver, ICombatFeedbackConsumer
    {
        [SerializeField] private HealthComponent _health;

        private ICombatFeedbackGateway _combatFeedback;

        public void Configure(ICombatFeedbackGateway combatFeedback)
        {
            _combatFeedback = combatFeedback;
        }

        private void Awake()
        {
            _health ??= GetComponent<HealthComponent>();
        }

        public DamageResolution ReceiveDamage(DamageInfo damageInfo, Vector2 impactPoint)
        {
            if (_health == null) return DamageResolution.Blocked;

            DamageResolution resolution = _health.ApplyDamage(damageInfo);
            if (!resolution.Accepted || resolution.Result.TotalDamage <= 0f) return resolution;

            _combatFeedback?.RequestDamageFeedback(
                damageInfo,
                resolution.Result,
                impactPoint);

            return resolution;
        }
    }
}
