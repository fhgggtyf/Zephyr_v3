/*
 * GameEventSO.cs
 * --------------
 * Module:  Core / Events
 * Purpose: Abstract base class for all GameEvent ScriptableObjects (non-generic).
 *          GameEventSO is the publisher side of the SO-based event system — events are
 *          authored as assets in the editor and raised at runtime via the Raise() method.
 *          GameEventListener (and its generic variant) is the subscriber side. This
 *          non-generic variant carries no payload; use GameEventSO<T> for typed payloads.
 * Dependencies: UnityEngine.ScriptableObject.
 * Scene:    N/A (assets live in Project window; referenced at runtime by GameFlow, GameManager).
 * Ch.Ref:   Ch.3 Event System Architecture.
 */
using System;
using UnityEngine;

namespace Zephyr.Core.Events
{
    /// <summary>
    /// Abstract base for all GameEvent ScriptableObjects.
    /// GameEventSO is the publisher; GameEventListener is the subscriber (Ch.3).
    /// </summary>
    public abstract class GameEventSO : ScriptableObject
    {
        public abstract void Raise();
    }
}
