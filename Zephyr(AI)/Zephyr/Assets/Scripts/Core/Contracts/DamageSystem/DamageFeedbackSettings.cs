using System;

namespace Zephyr.Contracts.Damage
{
    [Serializable]
    public readonly struct DamageFeedbackSettings
    {
        public readonly float HitstopIntensity;
        public readonly float HitstopDuration;
        public readonly float ShakeIntensity;
        public readonly int ImpactVfxId;
        public readonly int Priority;
        public readonly int CooldownGroup;

        public DamageFeedbackSettings(float hitstopIntensity = 1f, float hitstopDuration = 0f, float shakeIntensity = 1f, int impactVfxId = 0, int priority = 0, int cooldownGroup = 0)
        {
            HitstopIntensity = Math.Max(0f, hitstopIntensity);
            HitstopDuration = Math.Max(0f, hitstopDuration);
            ShakeIntensity = Math.Max(0f, shakeIntensity);
            ImpactVfxId = impactVfxId;
            Priority = priority;
            CooldownGroup = cooldownGroup;
        }

        public static DamageFeedbackSettings Default => new DamageFeedbackSettings();
    }
}
