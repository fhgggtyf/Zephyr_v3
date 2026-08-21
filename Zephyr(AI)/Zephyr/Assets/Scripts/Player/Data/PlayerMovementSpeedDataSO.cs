/*
 * PlayerMovementSpeedDataSO.cs
 * ----------------------------
 * Module:  Player / Data
 * Purpose: Centralized configuration for player movement speeds.
 *          All speed values used by state machine actions (Idle, Walk,
 *          Sprint, Roll) are read from this SO instead of being defined
 *          locally in each action. Edit once here, changes propagate
 *          to all states automatically.
 * Scene:    Created via CreateAssetMenu at "Zephyr/Player/Movement Speed".
 * Ch.Ref:   Ch.1.1 Tuning Architecture, Ch.6.4 State Actions.
 */
using UnityEngine;

namespace Zephyr.Gameplay.Player.Data
{
    [CreateAssetMenu(fileName = "PlayerMovementSpeedData", menuName = "Zephyr/Player/Movement Speed")]
    public class PlayerMovementSpeedDataSO : ScriptableObject
    {
        [Header("Movement Speeds")]
        [Tooltip("Speed when standing still. Typically 0.")]
        public float idleSpeed = 0f;

        [Tooltip("Walk speed in units/second.")]
        public float walkSpeed = 3f;

        [Tooltip("Sprint speed in units/second.")]
        public float sprintSpeed = 8f;

        [Tooltip("Roll speed in units/second. Usually sprintSpeed + offset.")]
        public float rollSpeed = 10f;

        [Tooltip("Dash speed in units/second. Fixed high-speed air movement.")]
        public float dashSpeed = 14f;

        [Tooltip("Crouch speed in units/second. Defaults to walk speed.")]
        public float crouchSpeed = 3f;
    }
}