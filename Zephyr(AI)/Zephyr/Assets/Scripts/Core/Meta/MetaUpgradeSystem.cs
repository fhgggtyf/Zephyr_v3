using System.Collections.Generic;
using Zephyr.Core.Save;

namespace Zephyr.Core.Meta
{
    public static class MetaUpgradeSystem
    {
        public const int InitialPotentialBudget = 20;
        public const int InitialPotentialAllocatable = 5;
        public const int MaxPotentialBudget = 100;
        public const int MaxPotentialAllocatable = 20;

        public static int GetPotentialBudget(MetaData meta)
        {
            if (meta == null) return InitialPotentialBudget;
            return System.Math.Min(MaxPotentialBudget, InitialPotentialBudget + Sum(meta.PotentialBudgetUpgrades));
        }

        public static int GetPotentialAllocatable(MetaData meta)
        {
            if (meta == null) return InitialPotentialAllocatable;
            int value = System.Math.Min(MaxPotentialAllocatable, InitialPotentialAllocatable + Sum(meta.PotentialAllocatableUpgrades));
            return value > GetPotentialBudget(meta) ? GetPotentialBudget(meta) : value;
        }

        public static bool CanPurchaseTarget(MetaUpgradeTarget target, MetaData meta)
        {
            if (meta == null) return false;
            if (target == MetaUpgradeTarget.PotentialBudget && GetPotentialBudget(meta) >= MaxPotentialBudget) return false;
            if (target == MetaUpgradeTarget.PotentialAllocatable && GetPotentialAllocatable(meta) >= MaxPotentialAllocatable) return false;
            return true;
        }

        public static bool CanPurchase(MetaUpgradeSO upgrade, MetaData meta)
        {
            if (upgrade == null || meta == null || string.IsNullOrWhiteSpace(upgrade.Id)) return false;
            if (!CanPurchaseTarget(upgrade.Target, meta)) return false;
            if (!upgrade.Repeatable && meta.PurchasedUpgrades.Contains(upgrade.Id)) return false;
            return meta.MetaCurrency >= upgrade.Cost;
        }

        public static bool Purchase(MetaUpgradeSO upgrade, MetaData meta, bool saveImmediately = true)
        {
            if (!CanPurchase(upgrade, meta)) return false;
            Dictionary<string, float> target = ResolveTarget(upgrade.Target, meta);
            if (target == null) return false;

            string key = string.IsNullOrWhiteSpace(upgrade.TargetKey) ? upgrade.Id : upgrade.TargetKey;
            float next = target.GetValueOrDefault(key) + upgrade.Amount;
            if (upgrade.Target == MetaUpgradeTarget.PotentialBudget)
                next = System.Math.Min(next, MaxPotentialBudget - InitialPotentialBudget);
            if (upgrade.Target == MetaUpgradeTarget.PotentialAllocatable)
                next = System.Math.Min(next, MaxPotentialAllocatable - InitialPotentialAllocatable);
            if (next <= target.GetValueOrDefault(key)) return false;

            meta.MetaCurrency -= upgrade.Cost;
            target[key] = next;
            meta.PurchasedUpgrades.Add(upgrade.Id);
            if (saveImmediately && SaveSystem.Instance?.Meta == meta) SaveSystem.Instance.SaveMeta();
            return true;
        }

        private static int Sum(Dictionary<string, float> values)
        {
            float total = 0f;
            if (values != null)
                foreach (float value in values.Values) total += value;
            return (int)total;
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
