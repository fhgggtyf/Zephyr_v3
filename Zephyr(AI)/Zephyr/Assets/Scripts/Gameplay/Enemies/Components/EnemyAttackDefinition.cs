using System;
using UnityEngine;

namespace Zephyr.Gameplay.Enemies
{
    /// <summary>
    /// One attack authored directly on an EnemySO. Regular enemies do not need
    /// WeaponSO assets; this describes their built-in attack instead.
    /// </summary>
    [Serializable]
    public sealed class EnemyAttackDefinition
    {
        [SerializeField] private string _id = "BasicMelee";
        [Tooltip("Animator state/tag used by this attack.")]
        [SerializeField] private string _animationState = "Attack";
        [Min(0f)] [SerializeField] private float _range = 1.1f;
        [Min(0f)] [SerializeField] private float _cooldown = 1.2f;
        [Min(0f)] [SerializeField] private float _telegraphDuration = 0.35f;
        [Min(0.01f)] [SerializeField] private float _totalDuration = 0.6f;
        [Min(0f)] [SerializeField] private float _activeStartTime;
        [Min(0f)] [SerializeField] private float _activeDuration = 0.18f;
        [Min(0f)] [SerializeField] private float _damageMultiplier = 1f;

        public string Id => string.IsNullOrWhiteSpace(_id) ? "Attack" : _id;
        public string AnimationState => string.IsNullOrWhiteSpace(_animationState) ? "Attack" : _animationState;
        public float Range => Mathf.Max(0f, _range);
        public float Cooldown => Mathf.Max(0f, _cooldown);
        public float TelegraphDuration => Mathf.Max(0f, _telegraphDuration);
        public float TotalDuration => Mathf.Max(0.01f, _totalDuration);
        public float ActiveStartTime => Mathf.Clamp(_activeStartTime, 0f, TotalDuration);
        public float ActiveDuration => Mathf.Clamp(_activeDuration, 0f, TotalDuration - ActiveStartTime);
        public float DamageMultiplier => Mathf.Max(0f, _damageMultiplier);
    }
}
