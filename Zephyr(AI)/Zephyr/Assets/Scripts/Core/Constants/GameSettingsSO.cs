/*
 * GameSettingsSO.cs
 * -----------------
 * Module:  Core / Constants
 * Purpose: Centralized runtime-tunable settings stored as a ScriptableObject asset.
 *          Unlike GameConstants (compile-time constants), these values can be edited in the
 *          Unity Inspector and changed without recompilation. Covers: potential system
 *          budgets (N = initial points, M = player-allocatable), player movement
 *          (speed, jump, dash, roll), and camera parameters (deadzone, follow smoothness).
 *          Created once via CreateAssetMenu at "Zephyr/Settings/Game Settings".
 * Dependencies: UnityEngine.ScriptableObject.
 * Scene:    Persistent (loaded at boot; referenced by GameFlow, camera, player controller).
 * Ch.Ref:   Ch.1.1 Tuning Architecture, Ch.5 Potential System, Ch.10 Camera System.
 */
using UnityEngine;

namespace Zephyr.Core
{
    /// <summary>
    /// Centralized runtime-tunable settings. Created once in a GameSettingsSO asset
    /// (ScriptableObjects/GameSettings/) and loaded at boot.
    /// </summary>
    [CreateAssetMenu(menuName = "Zephyr/Settings/Game Settings")]
    public class GameSettingsSO : ScriptableObject
    {
        [Header("Potential System")]
        [Tooltip("Starting potential points at run entry (N)")]
        public int InitialPotentialBudget = 20;

        [Tooltip("Starting player-allocatable potential points (M, M <= N)")]
        public int InitialPotentialAllocatable = 5;

        [Header("Player Movement")]
        [Tooltip("Default move speed; detailed speeds (idle/walk/sprint/roll) are in PlayerMovementSpeedDataSO")]
        public float MoveSpeed = 5f;
        public float JumpForce = 12f;
        public float DoubleJumpForce = 10f;
        public float DashSpeed = 15f;

        [Header("Camera")]
        public float CameraDeadzoneWidth = 2f;
        public float CameraDeadzoneHeight = 1.5f;
        public float CameraFollowSmoothTime = 0.1f;
    }
}
