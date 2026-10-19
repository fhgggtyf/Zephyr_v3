using Zephyr.Core.Save;

namespace Zephyr.Core.Meta
{
    public static class MechanicUnlockSystem
    {
        public static bool IsUnlocked(string mechanicId, IMetaGateway metaGateway)
        {
            MetaData meta = metaGateway?.Meta;
            return meta != null && !string.IsNullOrWhiteSpace(mechanicId)
                && meta.UnlockedMechanics.Contains(mechanicId);
        }

        public static bool CanPurchase(MechanicUnlockSO unlock, MetaData meta,
            IStoryProgressionGateway storyProgression)
        {
            if (unlock == null || meta == null || string.IsNullOrWhiteSpace(unlock.Id)) return false;
            if (meta.UnlockedMechanics.Contains(unlock.Id) || meta.MetaCurrency < unlock.Cost) return false;
            return string.IsNullOrWhiteSpace(unlock.RequiredFactId)
                || storyProgression?.GetBool(unlock.RequiredFactId) == true;
        }

        public static bool Purchase(MechanicUnlockSO unlock, MetaData meta,
            IMetaGateway metaGateway, IStoryProgressionGateway storyProgression,
            bool saveImmediately = true)
        {
            if (!CanPurchase(unlock, meta, storyProgression)) return false;
            meta.MetaCurrency -= unlock.Cost;
            meta.UnlockedMechanics.Add(unlock.Id);
            if (saveImmediately && metaGateway?.Meta == meta)
                metaGateway.Save();
            return true;
        }
    }
}
