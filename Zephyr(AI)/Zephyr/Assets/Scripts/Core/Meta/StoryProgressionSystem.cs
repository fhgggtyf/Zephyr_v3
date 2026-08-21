using System;
using System.Collections.Generic;
using System.Linq;
using Zephyr.Core.Save;

namespace Zephyr.Core.Meta
{
    public static class StoryProgressionSystem
    {
        public static bool FulfillObjective(string objectiveId)
        {
            if (string.IsNullOrWhiteSpace(objectiveId)) return false;
            SaveSystem save = SaveSystem.Instance;
            if (save?.CurrentRun != null)
                return save.CurrentRun.PendingObjectives.Add(objectiveId);
            if (save?.Meta == null) return false;

            bool added = save.Meta.FulfilledObjectives.Add(objectiveId);
            if (added) save.SaveMeta();
            return added;
        }

        public static void CommitRunObjectives(RunData run, MetaData meta)
        {
            if (run?.PendingObjectives == null || meta == null) return;
            meta.FulfilledObjectives.UnionWith(run.PendingObjectives);
            run.PendingObjectives.Clear();
        }

        public static StoryBeatSO[] EvaluatePendingBeats(IEnumerable<StoryBeatSO> beats, MetaData meta)
        {
            if (beats == null || meta == null) return Array.Empty<StoryBeatSO>();
            return beats.Where(beat => beat != null
                    && !meta.TriggeredStoryBeats.Contains(beat.Id)
                    && beat.RequiredObjectiveIds.All(meta.FulfilledObjectives.Contains))
                .OrderByDescending(beat => beat.Priority)
                .ThenBy(beat => beat.Id, StringComparer.Ordinal)
                .ToArray();
        }

        public static bool MarkBeatTriggered(MetaData meta, string beatId, bool saveImmediately = true)
        {
            if (meta == null || string.IsNullOrWhiteSpace(beatId)) return false;
            bool added = meta.TriggeredStoryBeats.Add(beatId);
            if (added && saveImmediately && SaveSystem.Instance?.Meta == meta)
                SaveSystem.Instance.SaveMeta();
            return added;
        }
    }
}
