using System.Collections.Generic;
using Zephyr.Core.Stats;

namespace Zephyr.Core.Runtime.Stats
{
    public static class StatModifierRegistry
    {
        private static readonly List<IStatModifierSource> Sources = new List<IStatModifierSource>();

        public static void Register(IStatModifierSource source)
        {
            if (source != null && !Sources.Contains(source)) Sources.Add(source);
        }

        public static void Unregister(IStatModifierSource source)
        {
            if (source != null) Sources.Remove(source);
        }

        public static void Apply(IStatModifierTarget target, ref float value,
            StatModifierTargetKind targetKind, StatType statType, MetaStatType metaStatType, string category)
        {
            for (int i = Sources.Count - 1; i >= 0; i--)
            {
                IStatModifierSource source = Sources[i];
                if (source == null)
                {
                    Sources.RemoveAt(i);
                    continue;
                }

                if (!source.AppliesTo(target)) continue;
                ApplyDefinitions(source.Modifiers, target, ref value, targetKind, statType, metaStatType, category);
            }
        }

        private static void ApplyDefinitions(IReadOnlyList<StatModifierDefinition> definitions,
            IStatModifierTarget target, ref float value, StatModifierTargetKind targetKind,
            StatType statType, MetaStatType metaStatType, string category)
        {
            if (definitions == null) return;

            for (int i = 0; i < definitions.Count; i++)
            {
                StatModifierDefinition definition = definitions[i];
                if (definition == null || definition.TargetKind != targetKind || !definition.AppliesTo(target)) continue;
                if (targetKind == StatModifierTargetKind.CoreStat && definition.StatType != statType) continue;
                if (targetKind == StatModifierTargetKind.MetaStat && definition.MetaStatType != metaStatType) continue;
                if (targetKind == StatModifierTargetKind.CategoryMultiplier &&
                    !string.Equals(definition.Category, category, System.StringComparison.Ordinal)) continue;

                switch (definition.Operation)
                {
                    case StatModifierOperation.Add: value += definition.Value; break;
                    case StatModifierOperation.Multiply: value *= definition.Value; break;
                    case StatModifierOperation.Override: value = definition.Value; break;
                }
            }
        }
    }
}
