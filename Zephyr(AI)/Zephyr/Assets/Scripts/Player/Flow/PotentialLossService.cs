using System;
using System.Collections.Generic;
using Zephyr.Core;
using Zephyr.Core.Interfaces;
using Zephyr.Core.Save;

namespace Zephyr.Gameplay.Player.Flow
{
    public readonly struct PotentialLossResult
    {
        public readonly StatType Target;
        public readonly float Before;
        public readonly float Loss;
        public readonly float After;

        public PotentialLossResult(StatType target, float before, float loss, float after)
        {
            Target = target;
            Before = before;
            Loss = loss;
            After = after;
        }
    }

    public sealed class PotentialLossService
    {
        public PotentialLossResult Calculate(IStatSource stats, StatType target, float potentialAttack, int downCount)
        {
            if (stats == null) throw new ArgumentNullException(nameof(stats));

            float before = stats.GetPotential(target);
            float loss = Math.Max(0f, potentialAttack) * (float)Math.Pow(2d, Math.Min(Math.Max(0, downCount), 30));
            float after = Math.Max(0f, before - loss);
            return new PotentialLossResult(target, before, loss, after);
        }

        public Dictionary<int, float> Capture(IStatSource stats)
        {
            if (stats == null) throw new ArgumentNullException(nameof(stats));

            Dictionary<int, float> potentials = new Dictionary<int, float>();
            foreach (StatType stat in Enum.GetValues(typeof(StatType)))
                potentials[(int)stat] = stats.GetPotential(stat);
            return potentials;
        }

        public void Restore(IStatSource stats, IDictionary<int, float> potentials, Action<StatType, float> apply)
        {
            if (stats == null || potentials == null || apply == null) return;

            foreach (KeyValuePair<int, float> pair in potentials)
                if (Enum.IsDefined(typeof(StatType), pair.Key))
                    apply((StatType)pair.Key, pair.Value);
        }

        public void SaveToRun(RunData run, IDictionary<int, float> potentials)
        {
            if (run == null || potentials == null) return;
            run.Potentials = new Dictionary<int, float>(potentials);
        }
    }
}
