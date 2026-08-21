using Zephyr.Core.Save;

namespace Zephyr.Core.Meta
{
    public static class MechanicUnlockSystem
    {
        public static bool IsUnlocked(string mechanicId)
        {
            MetaData meta = SaveSystem.Instance?.Meta;
            return meta != null && !string.IsNullOrWhiteSpace(mechanicId)
                && meta.UnlockedMechanics.Contains(mechanicId);
        }

        public static bool CanPurchase(MechanicUnlockSO unlock, MetaData meta)
        {
            if (unlock == null || meta == null || string.IsNullOrWhiteSpace(unlock.Id)) return false;
            if (meta.UnlockedMechanics.Contains(unlock.Id) || meta.MetaCurrency < unlock.Cost) return false;
            return string.IsNullOrWhiteSpace(unlock.RequiredObjectiveId)
                || meta.FulfilledObjectives.Contains(unlock.RequiredObjectiveId);
        }

        public static bool Purchase(MechanicUnlockSO unlock, MetaData meta, bool saveImmediately = true)
        {
            if (!CanPurchase(unlock, meta)) return false;
            meta.MetaCurrency -= unlock.Cost;
            meta.UnlockedMechanics.Add(unlock.Id);
            if (saveImmediately && SaveSystem.Instance?.Meta == meta)
                SaveSystem.Instance.SaveMeta();
            return true;
        }
    }
}
