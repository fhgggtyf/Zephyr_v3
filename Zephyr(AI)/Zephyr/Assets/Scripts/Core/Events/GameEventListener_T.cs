/*
 * GameEventListener_T.cs
 * ----------------------
 * Module:  Core / Events
 * Purpose: Concrete generic listener for GameEventSO<T>. Extends GameEventListener with
 *          a typed reference to GameEventSO<T> and a UnityEvent<T> response. Auto-subscribes
 *          in OnEnable (via RegisterListener) and unsubscribes in OnDisable (via
 *          UnregisterListener). The _response UnityEvent<T> is invoked when the event is
 *          raised — this allows wiring up responses in the Inspector without writing code.
 *          The RegisterListener/UnregisterListener methods are the single point of
 *          subscription — called automatically by the base class lifecycle.
 *          Note: the generic variant uses GameEventSO<T> (typed payload) instead of
 *          the base class's GameEventSO (no payload). Both fields coexist; the generic
 *          one is what actually gets subscribed to.
 * Dependencies: UnityEngine, UnityEngine.Events, GameEventSO<T>.
 * Scene:    GameManager (attached to UI panels and gameplay components).
 * Ch.Ref:   Ch.3 Event System Architecture.
 */
using UnityEngine;
using UnityEngine.Events;

namespace Zephyr.Core.Events
{
    /// <summary>
    /// Generic listener for GameEventSO<T>.
    /// Subscribe in OnEnable via RegisterListener, unsubscribe in OnDisable via UnregisterListener.
    /// </summary>
    public class GameEventListener<T> : GameEventListener
    {
        [SerializeField] private GameEventSO<T> _typedEvent;
        [SerializeField] private UnityEvent<T> _response;

        private void OnEventRaised(T payload)
        {
            _response?.Invoke(payload);
        }

        /// <summary>
        /// Subscribes to the typed event. Called automatically by the base class OnEnable.
        /// </summary>
        protected override void RegisterListener()
        {
            _typedEvent?.Subscribe(OnEventRaised);
        }

        /// <summary>
        /// Unsubscribes from the typed event. Called automatically by the base class OnDisable.
        /// </summary>
        protected override void UnregisterListener()
        {
            _typedEvent?.Unsubscribe(OnEventRaised);
        }
    }
}