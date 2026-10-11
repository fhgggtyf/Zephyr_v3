/*
 * IDamageable.cs
 * --------------
 * Module:  Core / Interfaces
 * Purpose: Contract for any entity that can receive damage. Implemented by HealthComponent
 *          on both player and enemy. The TakeDamage(DamageInfo) method is the single entry
 *          point for all incoming damage — health calculation, shield split, knockback,
 *          and event publishing all flow through the HealthComponent's implementation.
 *          DamageCalculator returns a DamageResult with DealtToShield and
 *          DealtToHp, so HealthComponent must handle both portions.
 * Dependencies: DamageInfo (value type parameter), Zephyr.Core.DamageSystem namespace.
 * Scene:    GameManager (implemented by HealthComponent on entity prefabs).
 * Ch.Ref:   Ch.2.8 Damage Calculation Pipeline, Ch.9 Combat System.
 */
using Zephyr.Core.DamageSystem;

namespace Zephyr.Core.Interfaces
{
    /// <summary>
    /// Entity that can receive damage. Implemented by HealthComponent on player/enemy.
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(DamageInfo damageInfo);
    }
}
