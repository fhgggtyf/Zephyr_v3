using Zephyr.Core.Save;

namespace Zephyr.Core.Meta
{
    /// <summary>Commits one completed run into profile-persistent data.</summary>
    public static class RunEndSettlement
    {
        public static bool Settle(RunData run, MetaData meta)
        {
            if (run == null || meta == null || run.IsSettled) return false;

            meta.MetaCurrency += run.RunCurrency;
            if (run.EquippedWeaponId != 0) meta.UnlockedWeapons.Add(run.EquippedWeaponId);
            if (run.AcquiredWeaponIds != null)
                meta.UnlockedWeapons.UnionWith(run.AcquiredWeaponIds);

            StoryProgressionSystem.CommitRunObjectives(run, meta);
            run.IsSettled = true;
            return true;
        }

        public static bool SettleCurrentRun()
        {
            SaveSystem save = SaveSystem.Instance;
            if (save?.CurrentRun == null || save.Meta == null) return false;
            if (!Settle(save.CurrentRun, save.Meta)) return false;

            save.SaveMeta();
            save.ClearRun();
            return true;
        }
    }
}
