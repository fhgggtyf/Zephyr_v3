/*
 * IHealable.cs
 * ------------
 * Module:  Core / Interfaces
 * Purpose: Contract for entities that can be healed. Implemented by HealthComponent on the
 *          player only — enemies cannot be healed by external sources. Heal(float) is called
 *          by healing pickups, skills, and the MetaHub recovery system. Returns void because
 *          healing always succeeds; overhealing is silently clamped to MaxHp.
 * Dependencies: None.
 * Scene:    GameManager (implemented by player HealthComponent).
 * Ch.Ref:   Ch.2.9 Healing System, Ch.9 Combat System.
 */
namespace Zephyr.Core.Interfaces
{
    /// <summary>
    /// Entity that can be healed. Implemented by HealthComponent on player only.
    /// </summary>
    public interface IHealable
    {
        void Heal(float amount);
    }
}
