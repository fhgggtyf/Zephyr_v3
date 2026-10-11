/*
 * GameEventListener.cs
 * --------------------
 * Module:  Core / Events
 * Purpose: Abstract base listener MonoBehaviour for GameEventSO (non-generic). Attach to any
 *          GameObject and assign a GameEventSO in the Inspector — the listener auto-subscribes
 *          in OnEnable and unsubscribes in OnDisable. Subclasses override RegisterListener/
 *          UnregisterListener to wire up to the specific GameEventSO they care about.
 *          OnEnable/OnDisable are protected virtual so the generic subclass (GameEventListener<T>)
 *          can optionally extend the lifecycle while keeping the subscribe/unsubscribe
 *          logic centralized in the abstract methods.
 *          For typed payload events, use GameEventListener<T> instead.
 * Dependencies: UnityEngine, UnityEngine.Events.
 * Scene:    GameManager (attached to UI panels, gameplay components that respond to events).
 * Ch.Ref:   Ch.3 Event System Architecture.
 */
using UnityEngine;
using UnityEngine.Events;

namespace Zephyr.Core.Events
{
    /// <summary>
    /// Abstract base listener for GameEventSO (non-generic).
    /// Attach to any GameObject; assigns a GameEventSO in the inspector.
    /// </summary>
    public abstract class GameEventListener : MonoBehaviour
    {
        [SerializeField] protected GameEventSO Event;

        protected virtual void OnEnable()
        {
            RegisterListener();
        }

        protected virtual void OnDisable()
        {
            UnregisterListener();
        }

        protected abstract void RegisterListener();
        protected abstract void UnregisterListener();
    }
}
