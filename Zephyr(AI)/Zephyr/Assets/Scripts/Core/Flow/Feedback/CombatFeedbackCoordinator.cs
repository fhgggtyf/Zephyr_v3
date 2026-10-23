using System.Collections.Generic;
using UnityEngine;
using Zephyr.Contracts.Damage;
using Zephyr.Contracts.Interfaces;
using Zephyr.Core.Flow;
using Zephyr.Core.UI;

namespace Zephyr.Core.Combat.Feedback
{
    [DisallowMultipleComponent]
    public sealed class CombatFeedbackCoordinator : MonoBehaviour, ICombatFeedbackGateway, IGameManagerDependencyConsumer
    {
        [Header("Normal hitstop")]
        [SerializeField, Min(0f)] private float _normalDurationPerIntensity = 0.018f;
        [SerializeField, Min(0f)] private float _normalMaxDuration = 0.08f;
        [SerializeField, Min(0f)] private float _normalCooldown = 0.055f;
        [Header("Hitback hitstop")]
        [SerializeField, Min(0f)] private float _hitbackBaseDuration = 0.11f;
        [SerializeField, Min(0f)] private float _hitbackDurationPerIntensity = 0.06f;
        [SerializeField, Min(0f)] private float _hitbackMaxDuration = 0.28f;
        [SerializeField, Min(0f)] private float _hitbackCooldown = 0.2f;
        [Header("Camera")]
        [SerializeField, Min(0f)] private float _shakePerIntensity = 1f;

        private ICameraShakeGateway _cameraShake;
        private float _hitstopEndsAt;
        private readonly Dictionary<int, float> _lastNormalRequestByGroup = new Dictionary<int, float>();
        private readonly Dictionary<int, float> _lastHitbackRequestByGroup = new Dictionary<int, float>();
        private int _activePriority = int.MinValue;
        private bool _hitstopActive;

        public void Configure(IGameManagerServices services)
        {
            _cameraShake = services?.CameraShake;
        }

        public void RequestDamageFeedback(DamageInfo info, DamageResult result, Vector2 impactPoint, bool isHitback = false, float hitbackIntensity = 0f)
        {
            float actualDamage = Mathf.Max(0f, result.TotalDamage);
            if (actualDamage <= 0f) return;
            if (info.Feedback.ShakeIntensity > 0f)
                _cameraShake?.ShakeForDamage(actualDamage * info.Feedback.ShakeIntensity * _shakePerIntensity);
            float intensity = Mathf.Max(info.Feedback.HitstopIntensity, actualDamage / 20f);
            if (isHitback) RequestHitbackFeedback(info, impactPoint, Mathf.Max(intensity, hitbackIntensity));
            else RequestNormalHitstop(info, intensity);
        }

        public void RequestHitbackFeedback(DamageInfo info, Vector2 impactPoint, float intensity)
        {
            if (!TryConsumeCooldown(_lastHitbackRequestByGroup, info.Feedback.CooldownGroup, _hitbackCooldown)) return;
            float duration = info.Feedback.HitstopDuration > 0f ? info.Feedback.HitstopDuration : _hitbackBaseDuration + intensity * _hitbackDurationPerIntensity;
            RequestHitstop(Mathf.Clamp(duration, 0f, _hitbackMaxDuration), info.Feedback.Priority + 100);
        }

        private void RequestNormalHitstop(DamageInfo info, float intensity)
        {
            if (!TryConsumeCooldown(_lastNormalRequestByGroup, info.Feedback.CooldownGroup, _normalCooldown)) return;
            float duration = info.Feedback.HitstopDuration > 0f ? info.Feedback.HitstopDuration : intensity * _normalDurationPerIntensity;
            RequestHitstop(Mathf.Clamp(duration, 0f, _normalMaxDuration), info.Feedback.Priority);
        }

        private void RequestHitstop(float duration, int priority)
        {
            if (duration <= 0f) return;
            float requestedEnd = Time.unscaledTime + duration;
            if (_hitstopActive && priority < _activePriority) return;
            _activePriority = Mathf.Max(_activePriority, priority);
            if (requestedEnd > _hitstopEndsAt) _hitstopEndsAt = requestedEnd;
            if (_hitstopActive) return;
            _hitstopActive = true;
            GameplayTimePause.SetCombatHitstop(this, true);
        }

        private static bool TryConsumeCooldown(Dictionary<int, float> lastRequestByGroup, int group, float cooldown)
        {
            float now = Time.unscaledTime;
            if (lastRequestByGroup.TryGetValue(group, out float lastRequestAt)
                && now - lastRequestAt < cooldown)
                return false;

            lastRequestByGroup[group] = now;
            return true;
        }

        private void Update()
        {
            if (!_hitstopActive || Time.unscaledTime < _hitstopEndsAt) return;
            _hitstopActive = false;
            _activePriority = int.MinValue;
            GameplayTimePause.SetCombatHitstop(this, false);
        }

        private void OnDisable()
        {
            if (_hitstopActive) GameplayTimePause.SetCombatHitstop(this, false);
            _hitstopActive = false;
        }
    }
}
