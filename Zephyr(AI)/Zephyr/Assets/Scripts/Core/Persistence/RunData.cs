/*
 * RunData.cs
 * ----------
 * Module:  Core / Save
 * Purpose: Session-local data created at run start and discarded at run end. Contains the
 *          seed (for deterministic RNG), current stage/room depth, run currency, base stats
 *          brought into the run, acquired in-run modifiers, equipped weapon ID, and damage/kill
 *          statistics. Stored as
 *          a JSON file via SaveSystem and loaded on resume. The seed is predetermined at
 *          run creation and drives all reward randomness.
 * Dependencies: System.Collections.Generic (Dictionary, HashSet).
 * Scene:    GameManager (runtime; serialized to disk during run via SaveSystem).
 * Ch.Ref:   Ch.12 Run Data Model, Ch.16 Save System, Ch.17 Meta Progression.
 */
using System.Collections.Generic;

namespace Zephyr.Core.Save
{
    /// <summary>
    /// Run-local data. Created at run start, discarded at run end.
    /// Seed-locked RNG drives all reward randomness.
    /// </summary>
    public class RunData
    {
        public int Seed;
        public int CurrentStage;
        public int CurrentRoomDepth;
        public long RunCurrency;

        // Monotonic for the current run. Down loss scales as PotentialAttack * 2^DownCount.
        public int DownCount;

        // Current per-stat Potential values, keyed by StatType's integer value.
        public Dictionary<int, float> Potentials = new Dictionary<int, float>();

        // Potentially-scaled base values (brought into run)
        public Dictionary<int, float> BaseStats = new Dictionary<int, float>();

        // Potentially-scaled gains acquired during run
        public Dictionary<int, float> AcquiredModifiers = new Dictionary<int, float>();

        // Weapon picked up in this run
        public int EquippedWeaponId;
        public HashSet<int> AcquiredWeaponIds = new HashSet<int>();

        // Prevents a run result from being committed more than once.
        public bool IsSettled;

        // Damage taken this run (for story/logic)
        public float TotalDamageTaken;

        // Enemies killed this run
        public int EnemiesKilled;
    }
}
