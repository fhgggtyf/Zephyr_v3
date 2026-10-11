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

        public static bool IsUnlocked(string mechanicId)
            => IsUnlocked(mechanicId, new MetaGateway(SaveSystem.Instance));

        public static bool CanPurchase(MechanicUnlockSO unlock, MetaData meta)
        {
            if (unlock == null || meta == null || string.IsNullOrWhiteSpace(unlock.Id)) return false;
            if (meta.UnlockedMechanics.Contains(unlock.Id) || meta.MetaCurrency < unlock.Cost) return false;
            return string.IsNullOrWhiteSpace(unlock.RequiredFactId)
                || (StoryProgressionService.Instance != null
                    && StoryProgressionService.Instance.GetBool(unlock.RequiredFactId));
        }

        public static bool Purchase(MechanicUnlockSO unlock, MetaData meta,
            IMetaGateway metaGateway, bool saveImmediately = true)
        {
            if (!CanPurchase(unlock, meta)) return false;
            meta.MetaCurrency -= unlock.Cost;
            meta.UnlockedMechanics.Add(unlock.Id);
            if (saveImmediately && metaGateway?.Meta == meta)
                metaGateway.Save();
            return true;
        }
    }
}
