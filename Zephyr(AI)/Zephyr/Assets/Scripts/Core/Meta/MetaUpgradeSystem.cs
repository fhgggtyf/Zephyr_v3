using System.Collections.Generic;
using Zephyr.Core.Save;

namespace Zephyr.Core.Meta
{
    public static class MetaUpgradeSystem
    {
        public static bool CanPurchase(MetaUpgradeSO upgrade, MetaData meta)
        {
            if (upgrade == null || meta == null || string.IsNullOrWhiteSpace(upgrade.Id)) return false;
            if (!upgrade.Repeatable && meta.PurchasedUpgrades.Contains(upgrade.Id)) return false;
            return meta.MetaCurrency >= upgrade.Cost;
        }

        public static bool Purchase(MetaUpgradeSO upgrade, MetaData meta, bool saveImmediately = true)
        {
            if (!CanPurchase(upgrade, meta)) return false;

            Dictionary<string, float> target = ResolveTarget(upgrade.Target, meta);
            if (target == null) return false;

            string key = string.IsNullOrWhiteSpace(upgrade.TargetKey) ? upgrade.Id : upgrade.TargetKey;
            meta.MetaCurrency -= upgrade.Cost;
            target[key] = target.GetValueOrDefault(key) + upgrade.Amount;
            meta.PurchasedUpgrades.Add(upgrade.Id);

            if (saveImmediately && SaveSystem.Instance?.Meta == meta)
                SaveSystem.Instance.SaveMeta();
            return true;
        }

        private static Dictionary<string, float> ResolveTarget(MetaUpgradeTarget target, MetaData meta)
        {
            switch (target)
            {
                case MetaUpgradeTarget.BaseStat: return meta.BaseStatUpgrades;
                case MetaUpgradeTarget.Combat: return meta.CombatAcquiredUpgrades;
                case MetaUpgradeTarget.PotentialBudget: return meta.PotentialBudgetUpgrades;
                case MetaUpgradeTarget.PotentialAllocatable: return meta.PotentialAllocatableUpgrades;
                default: return null;
            }
        }
    }
}
