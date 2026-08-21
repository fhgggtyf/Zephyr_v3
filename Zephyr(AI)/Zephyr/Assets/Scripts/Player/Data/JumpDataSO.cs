/*
 * JumpDataSO.cs
 * -------------
 * Module:  Player / Data
 * Purpose: Centralized configuration for jump physics parameters.
 *          All jump-related tuning values (force, gravity, fall speed)
 *          are stored here for consistent editing in one place.
 * Scene:    Created via CreateAssetMenu at "Zephyr/Player/Jump Data".
 * Ch.Ref:   Ch.1.1 Tuning Architecture, Ch.6.8 Physics / Jump Mechanics.
 */
using UnityEngine;

namespace Zephyr.Gameplay.Player.Data
{
    [CreateAssetMenu(fileName = "JumpData", menuName = "Zephyr/Player/Jump Data")]
    public class JumpDataSO : ScriptableObject
    {
        [Header("Jump Physics")]
        [Tooltip("Upward impulse applied when the player jumps (units/second).")]
        public float jumpForce = 12f;

        [Tooltip("Gravity scale during normal jump. 1 = default Unity gravity.")]
        public float gravityScale = 1f;

        [Tooltip("Gravity scale applied when releasing jump early (variable jump height).")]
        public float variableJumpGravityScale = 3f;

        [Header("Fall Physics")]
        [Tooltip("Multiplier applied to gravityScale during the fall phase. 2x = fall twice as fast as rise.")]
        public float fallGravityMultiplier = 2f;

        [Tooltip("Maximum fall speed cap (units/second). 0 = no cap.")]
        public float maxFallSpeed = 20f;
    }
}