/*
 * GameEventSO_T.cs
 * ----------------
 * Module:  Core / Events
 * Purpose: Generic GameEvent ScriptableObject that carries a typed payload T. This is the
 *          primary event mechanism for cross-module communication — events are authored
 *          as assets in the editor and raised at runtime with a typed payload. Subscribers
 *          register via Subscribe<T>/Unsubscribe<T>, and the Raise(T) method iterates
 *          listeners in reverse order (so recently-added listeners are called last,
 *          matching standard Unity event semantics). Payloads should be readonly structs
 *          to prevent mutation during dispatch.
 * Dependencies: System.Collections.Generic, UnityEngine.
 * Scene:    N/A (assets live in Project window; referenced at runtime by GameFlow, GameManager).
 * Ch.Ref:   Ch.3 Event System Architecture.
 */
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zephyr.Core.Events
{
    /// <summary>
    /// Generic GameEventSO that carries a typed payload T.
    /// Payloads should be readonly structs or implement IDisposable to prevent leaks (Ch.3 red line).
    /// </summary>
    [CreateAssetMenu(menuName = "Zephyr/Events/GameEvent<T>")]
    public class GameEventSO<T> : GameEventSO
    {
        private readonly List<Action<T>> _listeners = new List<Action<T>>();

        public void Subscribe(Action<T> listener)
        {
            if (!_listeners.Contains(listener))
                _listeners.Add(listener);
        }

        public void Unsubscribe(Action<T> listener)
        {
            _listeners.Remove(listener);
        }

        public void Raise(T payload)
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
                _listeners[i]?.Invoke(payload);
        }

        public override void Raise()
        {
            Raise(default);
        }

        private void OnDisable()
        {
            _listeners.Clear();
        }
    }
}
