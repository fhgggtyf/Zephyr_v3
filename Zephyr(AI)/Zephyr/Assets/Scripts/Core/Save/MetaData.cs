/*
 * MetaData.cs
 * -----------
 * Module:  Core / Save
 * Purpose: Persistent meta-game data that survives across runs. Contains profile identity
 *          (GUID), progress flags (HasCompletedTutorial), story progression (FulfilledObjectives,
 *          TriggeredStoryBeats), mechanic unlocks (UnlockedMechanics), meta currency, weapon
 *          codex (UnlockedWeapons), meta upgrade values (BaseStatUpgrades, CombatAcquiredUpgrades,
 *          PotentialBudgetUpgrades, PotentialAllocatableUpgrades), and challenge mode level.
 *          Written to disk via SaveSystem and loaded at boot. Monotonic — values only increase.
 * Dependencies: System.Collections.Generic (Dictionary, HashSet).
 * Scene:    Persistent (loaded at boot; written via SaveSystem).
 * Ch.Ref:   Ch.17 Meta Progression, Ch.22 Settlement Flow.
 */
using System.Collections.Generic;

namespace Zephyr.Core.Save
{
    /// <summary>
    /// Meta-game data. Persistent across runs. Written to disk via SaveSystem.
    /// Includes upgrades, codex, story progression, mechanic unlocks, and tutorial flag.
    /// </summary>
    public class MetaData
    {
        // Profile identity
        public string ProfileId;

        // Progress flags
        public bool HasCompletedTutorial;

        // Story progression (monotonic, never removed)
        public HashSet<string> FulfilledObjectives = new HashSet<string>();
        public HashSet<string> TriggeredStoryBeats = new HashSet<string>();

        // Mechanic unlocks (permanent, monotonic)
        public HashSet<string> UnlockedMechanics = new HashSet<string>();

        // Purchased meta upgrades (permanent, monotonic)
        public HashSet<string> PurchasedUpgrades = new HashSet<string>();

        // Currency (meta)
        public long MetaCurrency;

        // Weapon codex (weapons seen in runs)
        public HashSet<int> UnlockedWeapons = new HashSet<int>();

        // Meta upgrade values
        public Dictionary<string, float> BaseStatUpgrades = new Dictionary<string, float>();
        public Dictionary<string, float> CombatAcquiredUpgrades = new Dictionary<string, float>();
        public Dictionary<string, float> PotentialBudgetUpgrades = new Dictionary<string, float>();
        public Dictionary<string, float> PotentialAllocatableUpgrades = new Dictionary<string, float>();

        // Challenge mode progression
        public int ChallengeModeLevel;
    }
}
