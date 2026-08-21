/*
 * IFacingProvider.cs
 * ------------------
 * Module:  Core / Interfaces
 * Purpose: Contract for entities that expose their facing direction (2D Vector2). Used by
 *          the enemy AI system for detection/aggro checks (player must be within detection
 *          cone for alert state), by the backstab calculation in DamageCalculator (attacker
 *          must face target's back), and by the CameraController for directional look-ahead.
 *          Implemented by PlayerController and EnemyController.
 * Dependencies: UnityEngine.Vector2 (via property return type).
 * Scene:    GameManager (implemented by player and enemy controller components).
 * Ch.Ref:   Ch.11 Enemy AI System, Ch.9 Backstab Mechanic.
 */
namespace Zephyr.Core.Interfaces
{
    /// <summary>
    /// Provides the facing direction (used for detection, backstab, aggro).
    /// </summary>
    public interface IFacingProvider
    {
        UnityEngine.Vector2 Facing { get; }
    }
}
