using System.Collections.Generic;
using Zephyr.Core;

namespace Zephyr.Core.Meta
{
    /// <summary>Transiently carries the confirmed run-entry allocation into LoadingRun.</summary>
    public static class RunEntryPotentialService
    {
        private static Dictionary<StatType, float> s_pending;
        public static bool HasPending => s_pending != null;

        public static void SetPending(Dictionary<StatType, float> potentials)
            => s_pending = potentials == null ? null : new Dictionary<StatType, float>(potentials);

        public static bool TryConsume(out Dictionary<StatType, float> potentials)
        {
            if (s_pending == null) { potentials = null; return false; }
            potentials = new Dictionary<StatType, float>(s_pending);
            s_pending = null;
            return true;
        }

        public static void Clear() => s_pending = null;
    }
}
