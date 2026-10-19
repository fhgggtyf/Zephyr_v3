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

        public static bool TryGetOverride(IStatModifierTarget target,
            StatModifierTargetKind targetKind, StatType statType, MetaStatType metaStatType,
            string category, out float value)
        {
            for (int i = Sources.Count - 1; i >= 0; i--)
            {
                IStatModifierSource source = Sources[i];
                if (source == null)
                {
                    Sources.RemoveAt(i);
                    continue;
                }

                if (!source.AppliesTo(target) || source.Modifiers == null) continue;
                for (int j = 0; j < source.Modifiers.Count; j++)
                {
                    StatModifierDefinition definition = source.Modifiers[j];
                    if (definition == null || definition.Operation != StatModifierOperation.Override ||
                        definition.TargetKind != targetKind || !definition.AppliesTo(target)) continue;
                    if (targetKind == StatModifierTargetKind.CoreStat && definition.StatType != statType) continue;
                    if (targetKind == StatModifierTargetKind.MetaStat && definition.MetaStatType != metaStatType) continue;
                    if (targetKind == StatModifierTargetKind.CategoryMultiplier &&
                        !string.Equals(definition.Category, category, System.StringComparison.Ordinal)) continue;
                    value = definition.Value;
                    return true;
                }
            }

            value = 0f;
            return false;
        }

        public static bool TryGetSceneRuleOverride(IStatModifierTarget target,
            SceneRuleOverrideKind rule, out float value)
        {
            for (int i = Sources.Count - 1; i >= 0; i--)
            {
                IStatModifierSource source = Sources[i];
                if (source == null)
                {
                    Sources.RemoveAt(i);
                    continue;
                }

                if (!(source is ISceneRuleSource ruleSource) || !source.AppliesTo(target) ||
                    ruleSource.RuleOverrides == null) continue;
                for (int j = 0; j < ruleSource.RuleOverrides.Count; j++)
                {
                    SceneRuleOverrideDefinition definition = ruleSource.RuleOverrides[j];
                    if (definition != null && definition.Rule == rule && definition.AppliesTo(target))
                    {
                        value = definition.Value;
                        return true;
                    }
                }
            }

            value = 0f;
            return false;
        }


        public static void Apply(IStatModifierTarget target, ref float value,
            StatModifierTargetKind targetKind, StatType statType, MetaStatType metaStatType, string category)
        {
            Collect(target, targetKind, statType, metaStatType, category, out float additive, out float percent);
            value += additive;
            value *= 1f + percent;
        }

        public static void Collect(IStatModifierTarget target,
            StatModifierTargetKind targetKind, StatType statType, MetaStatType metaStatType,
            string category, out float additive, out float percent)
        {
            additive = 0f;
            percent = 0f;
            for (int i = Sources.Count - 1; i >= 0; i--)
            {
                IStatModifierSource source = Sources[i];
                if (source == null)
                {
                    Sources.RemoveAt(i);
                    continue;
                }

                if (!source.AppliesTo(target) || source.Modifiers == null) continue;
                for (int j = 0; j < source.Modifiers.Count; j++)
                {
                    StatModifierDefinition definition = source.Modifiers[j];
                    if (definition == null || definition.Operation == StatModifierOperation.Override ||
                        definition.TargetKind != targetKind || !definition.AppliesTo(target)) continue;
                    if (targetKind == StatModifierTargetKind.CoreStat && definition.StatType != statType) continue;
                    if (targetKind == StatModifierTargetKind.MetaStat && definition.MetaStatType != metaStatType) continue;
                    if (targetKind == StatModifierTargetKind.CategoryMultiplier &&
                        !string.Equals(definition.Category, category, System.StringComparison.Ordinal)) continue;

                    if (definition.Operation == StatModifierOperation.Add) additive += definition.Value;
                    else percent += definition.Value;
                }
            }
        }


    }
}
