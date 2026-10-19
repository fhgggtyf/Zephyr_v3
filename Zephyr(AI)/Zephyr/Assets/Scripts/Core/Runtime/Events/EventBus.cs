/*
 * EventBus.cs
 * -----------
 * Module:  Core / Events
 * Purpose: Central typed event bus singleton for runtime, ad-hoc event publishing.
 *          Unlike GameEventSO<T> (ScriptableObject-based, authored in editor), EventBus
 *          uses dictionary-based delegate dispatch for events created and published at
 *          runtime without pre-authored assets. Provides Subscribe<T>/Unsubscribe<T>/
 *          Publish<T> methods. Lives in the Persistent scene and is always available.
 * Dependencies: System, Dictionary<Type, Delegate>.
 * Scene:    Persistent (Ch.14.2 — infrastructure manager, never unloaded).
 * Ch.Ref:   Ch.3 Event System Architecture.
 */
using System;
using System.Collections.Generic;
using UnityEngine;
using Zephyr.Contracts.Interfaces;
using Zephyr.Core.Runtime.Lifecycle;

namespace Zephyr.Core.Events
{
    /// <summary>
    /// Central event bus. All core subsystems publish events through this singleton.
    /// Lives in the Persistent scene (Ch.14.2).
    /// </summary>
    [DefaultExecutionOrder(-900)]
    [SingletonLifetime(SingletonLifetime.Persistent, "Persistent")]
    public sealed class EventBus : MonoBehaviour, IEventBusGateway
    {
        private static EventBus _instance;
        public static EventBus Instance => _instance;

        private void Awake()
        {
            if (!SingletonLifecycle.TryClaim(this, ref _instance, nameof(EventBus))
                || _instance != this)
            {
                Destroy(gameObject);
                return;
            }

        }

        private void OnDestroy()
        {
            SingletonLifecycle.Release(this, ref _instance);
        }

        // Typed event publishing (runtime, no SO asset needed for ad-hoc events)
        private readonly Dictionary<Type, Delegate> _subscribers = new Dictionary<Type, Delegate>();
        private readonly Dictionary<Type, object> _latestPayloads = new Dictionary<Type, object>();

        public void Subscribe<T>(Action<T> handler)
        {
            var key = typeof(T);
            if (_subscribers.TryGetValue(key, out var existing))
                _subscribers[key] = Delegate.Combine(existing, handler);
            else
                _subscribers[key] = handler;
        }

        public void Unsubscribe<T>(Action<T> handler)
        {
            var key = typeof(T);
            if (_subscribers.TryGetValue(key, out var existing))
            {
                var newDelegate = Delegate.Remove(existing, handler);
                if (newDelegate == null)
                    _subscribers.Remove(key);
                else
                    _subscribers[key] = newDelegate;
            }
        }

        public void Publish<T>(T payload)
        {
            _latestPayloads[typeof(T)] = payload;
            if (_subscribers.TryGetValue(typeof(T), out var del))
                del?.DynamicInvoke(payload);
        }

        public bool TryGetLatest<T>(out T payload)
        {
            if (_latestPayloads.TryGetValue(typeof(T), out object value) && value is T typed)
            {
                payload = typed;
                return true;
            }

            payload = default;
            return false;
        }
    }
}
