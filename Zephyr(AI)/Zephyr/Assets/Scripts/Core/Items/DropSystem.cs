using System;
using System.Collections.Generic;
using UnityEngine;
using Zephyr.Core.Weapons;

namespace Zephyr.Core.Items
{
    public enum DropKind { Weapon, Currency }

    [Serializable]
    public struct DropEntry
    {
        public DropKind Kind;
        public WeaponSO Weapon;
        [Min(0f)] public float Weight;
        public int CurrencyMin;
        public int CurrencyMax;
        [Min(0f)] public float LuckQualityBonus;
    }

    public readonly struct DropContext
    {
        public readonly int Seed;
        public readonly int SourceId;
        public readonly int RoomDepth;
        public DropContext(int seed, int sourceId, int roomDepth)
        { Seed = seed; SourceId = sourceId; RoomDepth = roomDepth; }
    }

    public readonly struct DropResult
    {
        public readonly DropKind Kind;
        public readonly WeaponSO Weapon;
        public readonly int CurrencyAmount;
        public DropResult(DropKind kind, WeaponSO weapon, int currencyAmount)
        { Kind = kind; Weapon = weapon; CurrencyAmount = currencyAmount; }
    }

    public static class DropSystem
    {
        public static DropResult[] Roll(DropTableSO table, DropContext context, float luck)
        {
            if (table == null || table.Entries.Count == 0) return Array.Empty<DropResult>();
            var random = new System.Random(HashSeed(context));
            var results = new List<DropResult>(table.RollCount);
            for (int roll = 0; roll < table.RollCount; roll++)
            {
                DropEntry entry = Select(table.Entries, random, luck);
                if (entry.Weight <= 0f) continue;
                if (entry.Kind == DropKind.Weapon)
                {
                    if (entry.Weapon != null) results.Add(new DropResult(DropKind.Weapon, entry.Weapon, 0));
                }
                else
                {
                    int min = Mathf.Min(entry.CurrencyMin, entry.CurrencyMax);
                    int max = Mathf.Max(entry.CurrencyMin, entry.CurrencyMax);
                    results.Add(new DropResult(DropKind.Currency, null, random.Next(min, max + 1)));
                }
            }
            return results.ToArray();
        }

        private static DropEntry Select(IReadOnlyList<DropEntry> entries, System.Random random, float luck)
        {
            float total = 0f;
            for (int i = 0; i < entries.Count; i++)
                total += EffectiveWeight(entries[i], luck);
            if (total <= 0f) return default;
            float pick = (float)random.NextDouble() * total;
            for (int i = 0; i < entries.Count; i++)
            {
                pick -= EffectiveWeight(entries[i], luck);
                if (pick <= 0f) return entries[i];
            }
            return entries[entries.Count - 1];
        }

        private static float EffectiveWeight(DropEntry entry, float luck)
            => Mathf.Max(0f, entry.Weight) * (entry.Kind == DropKind.Weapon
                ? 1f + Mathf.Max(0f, luck) * Mathf.Max(0f, entry.LuckQualityBonus)
                : 1f);

        private static int HashSeed(DropContext context)
        {
            unchecked { return context.Seed * 397 ^ context.SourceId * 7919 ^ context.RoomDepth * 104729; }
        }
    }

}
