/*
 * GameConstants.cs
 * ----------------
 * Module:  Core / Constants
 * Purpose: Centralized immutable tuning values that do not change at runtime.
 *          Organized into nested static classes by subsystem: Stats (potential scaling),
 *          Damage (variance, piercing bounds), Combat (pickup ranges, backstab multiplier,
 *          crit damage multiplier), Player (aggro/detection ranges, alert duration),
 *          and Gameplay (inter-wave delay, spawn telegraph). All values are compile-time
 *          constants, not ScriptableObject data.
 *          Note: ShieldCoeff (P) is weapon-inherent (≥ 0, no upper bound), not a global
 *          constant — see Ch.4.7 / WeaponSO for its definition and the shield-break formula.
 * Dependencies: UnityEngine (for Mathf in some consumers).
 * Scene:    N/A (compilation-only; referenced by all gameplay subsystems).
 * Ch.Ref:   Ch.1.1 Tuning Architecture, Ch.5 Stat System, Ch.9 Combat System.
 */
using UnityEngine;

namespace Zephyr.Core
{
    /// <summary>
    /// Immutable game constants. Centralized tuning values that don't change at runtime.
    /// </summary>
    public static class GameConstants
    {
        public static class Stats
        {
            public const int PotentialMax = 10;
            public const float PotentialScaleMultiplier = 0.1f; // Potential / 10
        }

        public static class Damage
        {
            public const float DamageVarianceStdDev = 0.05f;
            public const float LuckVarianceMeanShift = 0.01f; // +1% per Luck point
            public const float PenetrationMin = 0f;
            public const float PenetrationMax = 1f;
        }

        public static class Combat
        {
            public const float WeaponPickupRange = 1.5f;
            public const float CurrencyMagnetRadius = 5f;
            public const float BackstabDefaultMultiplier = 1.0f;
            public const float CriticalDamageMultiplier = 2f;
        }

        public static class Player
        {
            public const float DefaultAggroRange = 6f;
            public const float DefaultDetectionRange = 8f;
            public const float DefaultAlertDuration = 0.5f;
        }

        public static class Gameplay
        {
            public const float InterWaveDelay = 2f;
            public const float SpawnTelegraphDuration = 0.35f;
        }
    }
}
