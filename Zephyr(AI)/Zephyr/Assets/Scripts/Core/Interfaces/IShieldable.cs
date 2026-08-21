/*
 * IShieldable.cs
 * --------------
 * Module:  Core / Interfaces
 * Purpose: Contract for any entity that has a shield layer that can absorb damage
 *          before health. Implemented by HealthComponent on player and enemy prefabs.
 *          Provides CurrentShield (remaining shield), MaxShield (shield capacity),
 *          and ApplyShieldDamage() for DamageCalculator to split damage through.
 *          Shield damage is non-lethal — it only drains the shield pool.
 *          When a shield breaks (reaches 0), the overflow goes to health.
 * Dependencies: None.
 * Scene:    GameManager (implemented by HealthComponent on entity prefabs).
 * Ch.Ref:   Ch.2.8 Damage Calculation Pipeline, Ch.9 Combat System, Ch.12 Shield System.
 */

namespace Zephyr.Core.Interfaces
{
    /// <summary>
    /// Entity that has a shield layer that absorbs damage before health.
    /// Implemented by HealthComponent.
    /// </summary>
    public interface IShieldable
    {
        /// <summary>
        /// Current shield value remaining.
        /// </summary>
        float CurrentShield { get; }

        /// <summary>
        /// Maximum shield capacity.
        /// </summary>
        float MaxShield { get; }

        /// <summary>
        /// Absorb damage into the shield pool. Returns the amount that was
        /// actually absorbed (may be less than requested if shield is insufficient).
        /// The caller receives the overflow that must go to health.
        /// </summary>
        /// <param name="damage">Amount of damage to absorb into shield.</param>
        /// <returns>Amount of damage that exceeded the shield (overflow to health).</returns>
        float ApplyShieldDamage(float damage);
    }
}