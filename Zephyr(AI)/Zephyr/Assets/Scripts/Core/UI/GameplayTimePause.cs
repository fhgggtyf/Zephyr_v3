using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zephyr.Core.UI
{
    /// <summary>
    /// Coordinates scaled-time pauses from modal gameplay UI and GameFlow.
    /// Each owner is independent, so closing one modal cannot resume another.
    /// HUD and death presentation intentionally never register as pause owners.
    /// </summary>
    public static class GameplayTimePause
    {
        private static readonly HashSet<object> s_modalOwners = new HashSet<object>();
        private static bool s_gameFlowPaused;
        private static bool s_lastPaused;

        public static bool IsPaused => s_gameFlowPaused || s_modalOwners.Count > 0;
        public static event Action<bool> StateChanged;

        public static void SetModalOpen(object owner, bool open)
        {
            if (owner == null) return;
            if (open) s_modalOwners.Add(owner);
            else s_modalOwners.Remove(owner);
            Apply();
        }

        public static void SetGameFlowPaused(bool paused)
        {
            s_gameFlowPaused = paused;
            Apply();
        }

        public static void ResetAll()
        {
            s_modalOwners.Clear();
            s_gameFlowPaused = false;
            Apply();
        }

        private static void Apply()
        {
            bool paused = IsPaused;
            Time.timeScale = paused ? 0f : 1f;
            if (paused == s_lastPaused) return;

            s_lastPaused = paused;
            StateChanged?.Invoke(paused);
        }
    }
}
