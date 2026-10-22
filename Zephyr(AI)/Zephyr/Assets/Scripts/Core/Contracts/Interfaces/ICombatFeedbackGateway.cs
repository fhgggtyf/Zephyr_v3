using UnityEngine;
using Zephyr.Contracts.Damage;

namespace Zephyr.Contracts.Interfaces
{
    public interface ICombatFeedbackGateway
    {
        void RequestDamageFeedback(DamageInfo info, DamageResult result, Vector2 impactPoint, bool isHitback = false, float hitbackIntensity = 0f);
        void RequestHitbackFeedback(DamageInfo info, Vector2 impactPoint, float intensity);
    }
}
