/*
 * HurtReceiver.cs
 * ---------------
 * Module:  Player / Core
 * Purpose: Manages the player's invincibility frames and exposes a narrow damage filter.
 *          HealthComponent queries the filter before applying damage.
 *          Lives on the player prefab's "Core" child alongside MovementCore.
 * Scene:    GameManager (attached to player prefab's "Core" child).
 * Ch.Ref:   Ch.6.10 Presentation / Combat Hooks, Ch.7 Combat System.
 */
using UnityEngine;
using Zephyr.Core.DamageSystem;
using Zephyr.Core.Interfaces;

namespace Zephyr.Gameplay.Player.Core
{
    /// <summary>
    /// Damage filter for the player. Invincibility is toggled by state actions
    /// (InvincibilityActionSO) during roll or other invulnerable states.
    /// </summary>
    public class HurtReceiver : MonoBehaviour, IDamageFilter
    {
        [Header("Damage Debug")]
        [Tooltip("If true, logs damage messages to Console (editor only).")]
        [SerializeField] private bool _logDamage = true;

        /// <summary>
        /// True when the player is invincible (e.g., during roll).
        /// Set by InvincibilityActionSO via SetInvincible().
        /// </summary>
        public bool IsInvincible { get; private set; }

        /// <summary>
        /// Sets the invincibility flag. Called by InvincibilityActionSO on
        /// state enter (true) and exit (false).
        /// </summary>
        public void SetInvincible(bool invincible)
        {
            IsInvincible = invincible;
        }

        public bool ShouldBlockDamage(DamageInfo damageInfo)
        {
            if (!IsInvincible)
            {
                return false;
            }

#if UNITY_EDITOR
            if (_logDamage)
            {
                Debug.Log($"[HurtReceiver] Damage blocked (invincible). " +
                          $"Source: {damageInfo.Source?.name ?? "null"}", this);
            }
#endif
            return true;
        }
    }
}
