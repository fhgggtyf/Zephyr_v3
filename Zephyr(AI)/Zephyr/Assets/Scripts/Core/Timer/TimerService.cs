/*
 * TimerService.cs
 * ---------------
 * Module:  Core / Timer
 * Purpose: Centralized timer service singleton. Replaces ad-hoc StartCoroutine/Invoke calls
 *          with a unified interface for scheduling timed callbacks. Provides SetTimeout(duration,
 *          callback) for one-shot delays and SetInterval(interval, callback) for recurring
 *          callbacks. Both return TimerHandle objects that can be cancelled before firing.
 *          Uses coroutines internally but exposes a clean API to consumers. Lives in the
 *          Persistent scene so timers survive scene loads.
 * Dependencies: System, UnityEngine (MonoBehaviour, Time, WaitForSeconds).
 * Scene:    Persistent (Ch.14.2 — infrastructure manager, never unloaded).
 * Ch.Ref:   Ch.24.2 Timer Service.
 */
using System;
using UnityEngine;

namespace Zephyr.Core.Timer
{
    /// <summary>
    /// Centralized timer service. Replaces ad-hoc StartCoroutine/Invoke calls.
    /// All timed callbacks (spawn telegraphs, damage-over-time, UI timers) go through this.
    /// Lives in the Persistent scene (Ch.14.2).
    /// </summary>
    [DefaultExecutionOrder(-400)]
    public sealed class TimerService : MonoBehaviour
    {
        public enum Clock { Gameplay, UI }
        public static TimerService Instance { get; private set; }
        public TimerService Gameplay => this;
        public TimerService UI => this;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Schedule a callback to fire once after <paramref name="duration"/> seconds.
        /// Returns a TimerHandle that can be cancelled before firing.
        /// </summary>
        public TimerHandle SetTimeout(float duration, Action callback)
        {
            return SetTimeout(duration, callback, Clock.Gameplay);
        }

        public TimerHandle SetTimeout(float duration, Action callback, Clock clock)
        {
            var handle = new TimerHandle(this);
            StartCoroutine(RunTimeout(duration, callback, handle, clock));
            return handle;
        }

        /// <summary>
        /// Schedule a callback to fire every <paramref name="interval"/> seconds.
        /// Returns a TimerHandle that can be cancelled.
        /// </summary>
        public TimerHandle SetInterval(float interval, Action callback)
        {
            return SetInterval(interval, callback, Clock.Gameplay);
        }

        public TimerHandle SetInterval(float interval, Action callback, Clock clock)
        {
            var handle = new TimerHandle(this);
            StartCoroutine(RunInterval(interval, callback, handle, clock));
            return handle;
        }

        private System.Collections.IEnumerator RunTimeout(float duration, Action callback, TimerHandle handle, Clock clock)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (handle.IsCancelled)
                    yield break;
                elapsed += clock == Clock.UI ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }
            if (!handle.IsCancelled)
                callback?.Invoke();
        }

        private System.Collections.IEnumerator RunInterval(float interval, Action callback, TimerHandle handle, Clock clock)
        {
            while (!handle.IsCancelled)
            {
                float elapsed = 0f;
                while (elapsed < interval)
                {
                    if (handle.IsCancelled)
                        yield break;
                    elapsed += clock == Clock.UI ? Time.unscaledDeltaTime : Time.deltaTime;
                    yield return null;
                }
                if (!handle.IsCancelled)
                    callback?.Invoke();
            }
        }
    }
}
