/*
 * SaveSystem.cs
 * -------------
 * Module:  Core / Save
 * Purpose: Central save/load system singleton. Handles JSON serialization of MetaData
 *          (persistent cross-run data) and RunData (session-local data). Converts runtime
 *          Dictionary and HashSet collections to JSON-compatible list DTOs so all save data
 *          round-trips through JsonUtility without changing the public runtime models. Provides
 *          LoadMeta(), SaveMeta(), LoadRun(), SaveRun(), ClearRun(), and StartNewRun(seed).
 *          Files are stored in Application.persistentDataPath/Save/. On first load, creates a
 *          fresh MetaData with a new GUID profile ID. Lives in the Persistent scene.
 * Dependencies: System, System.Collections.Generic, System.IO, UnityEngine.JsonUtility,
 *               MetaData, RunData.
 * Scene:    Persistent (Ch.14.2 — infrastructure manager, never unloaded).
 * Ch.Ref:   Ch.16 Save System, Ch.17 Meta Progression.
 */
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Zephyr.Core.Save
{
    /// <summary>
    /// Central save system. Lives in the Persistent scene (Ch.14.2).
    /// Handles saving/loading MetaData and RunData to JSON files.
    /// </summary>
    [DefaultExecutionOrder(-600)]
    public sealed class SaveSystem : MonoBehaviour
    {
        public static SaveSystem Instance { get; private set; }
        public const int MaxSaveSlots = 3;

        private const string MetaDataFileName = "meta_data.json";
        private const string RunDataFileName = "run_data.json";
        private const string SlotDirectoryPrefix = "slot_";

        private string _savePath;

        public MetaData Meta { get; private set; }
        public RunData CurrentRun { get; private set; }
        public int SelectedSlotIndex { get; private set; } = -1;
        public bool HasAnyValidSave
        {
            get
            {
                for (int i = 0; i < MaxSaveSlots; i++)
                {
                    if (GetSlotInfo(i).IsOccupied) return true;
                }

                return false;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _savePath = Path.Combine(Application.persistentDataPath, "Save");

            if (!Directory.Exists(_savePath))
            {
                Directory.CreateDirectory(_savePath);
            }

        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public MetaData LoadMeta()
        {
            if (!IsValidSlotIndex(SelectedSlotIndex)) return null;

            string path = GetSlotFilePath(SelectedSlotIndex, MetaDataFileName);
            if (File.Exists(path))
            {
                MetaDataDto data = ReadMetaDto(path);
                Meta = IsValidMeta(data) ? data.ToRuntime() : null;
            }
            else Meta = null;

            return Meta;
        }

        public void SaveMeta()
        {
            if (Meta == null)
            {
                return;
            }

            if (!IsValidSlotIndex(SelectedSlotIndex)) return;

            string path = GetSlotFilePath(SelectedSlotIndex, MetaDataFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string json = JsonUtility.ToJson(MetaDataDto.FromRuntime(Meta), true);
            File.WriteAllText(path, json);
        }

        public RunData LoadRun()
        {
            if (!IsValidSlotIndex(SelectedSlotIndex)) return null;

            string path = GetSlotFilePath(SelectedSlotIndex, RunDataFileName);
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                RunDataDto data = JsonUtility.FromJson<RunDataDto>(json);
                CurrentRun = data?.ToRuntime();
            }
            else CurrentRun = null;

            return CurrentRun;
        }

        public void SaveRun()
        {
            if (CurrentRun == null)
            {
                return;
            }

            if (!IsValidSlotIndex(SelectedSlotIndex)) return;

            string path = GetSlotFilePath(SelectedSlotIndex, RunDataFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string json = JsonUtility.ToJson(RunDataDto.FromRuntime(CurrentRun), true);
            File.WriteAllText(path, json);
        }

        public void ClearRun()
        {
            CurrentRun = null;
            if (!IsValidSlotIndex(SelectedSlotIndex)) return;

            string path = GetSlotFilePath(SelectedSlotIndex, RunDataFileName);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public void StartNewRun(int seed)
        {
            CurrentRun = new RunData
            {
                Seed = seed,
                CurrentStage = 1,
                CurrentRoomDepth = 0
            };
        }

        public SaveSlotInfo[] GetSlotInfos()
        {
            var slots = new SaveSlotInfo[MaxSaveSlots];
            for (int i = 0; i < slots.Length; i++) slots[i] = GetSlotInfo(i);
            return slots;
        }

        public SaveSlotInfo GetSlotInfo(int slotIndex)
        {
            if (!IsValidSlotIndex(slotIndex)) return SaveSlotInfo.Invalid(slotIndex);

            string metaPath = GetSlotFilePath(slotIndex, MetaDataFileName);
            if (!File.Exists(metaPath)) return SaveSlotInfo.Empty(slotIndex);

            MetaDataDto data = ReadMetaDto(metaPath);
            if (!IsValidMeta(data)) return SaveSlotInfo.Empty(slotIndex);

            string runPath = GetSlotFilePath(slotIndex, RunDataFileName);
            return new SaveSlotInfo(
                slotIndex,
                true,
                File.Exists(runPath),
                data.ProfileId,
                data.HasCompletedTutorial,
                File.GetLastWriteTimeUtc(metaPath));
        }

        public bool TrySelectSlot(int slotIndex)
        {
            if (!GetSlotInfo(slotIndex).IsOccupied) return false;

            SelectedSlotIndex = slotIndex;
            LoadMeta();
            LoadRun();
            if (Meta != null) return true;

            SelectedSlotIndex = -1;
            CurrentRun = null;
            return false;
        }

        public bool TryCreateSlot(int slotIndex)
        {
            if (!IsValidSlotIndex(slotIndex) || GetSlotInfo(slotIndex).IsOccupied) return false;

            SelectedSlotIndex = slotIndex;
            Meta = CreateNewMeta();
            CurrentRun = null;

            string runPath = GetSlotFilePath(slotIndex, RunDataFileName);
            if (File.Exists(runPath)) File.Delete(runPath);

            SaveMeta();
            return true;
        }

        public bool DeleteSlot(int slotIndex)
        {
            if (!IsValidSlotIndex(slotIndex)) return false;

            string slotPath = GetSlotDirectory(slotIndex);
            if (!Directory.Exists(slotPath)) return false;

            Directory.Delete(slotPath, true);
            if (SelectedSlotIndex == slotIndex)
            {
                SelectedSlotIndex = -1;
                Meta = null;
                CurrentRun = null;
            }

            return true;
        }

        private string GetSlotDirectory(int slotIndex)
            => Path.Combine(_savePath, $"{SlotDirectoryPrefix}{slotIndex + 1}");

        private string GetSlotFilePath(int slotIndex, string fileName)
            => Path.Combine(GetSlotDirectory(slotIndex), fileName);

        private static bool IsValidSlotIndex(int slotIndex)
            => slotIndex >= 0 && slotIndex < MaxSaveSlots;

        private static MetaDataDto ReadMetaDto(string path)
        {
            try
            {
                return JsonUtility.FromJson<MetaDataDto>(File.ReadAllText(path));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"SaveSystem: could not read save metadata at '{path}': {exception.Message}");
                return null;
            }
        }

        private static bool IsValidMeta(MetaDataDto data)
            => data != null && !string.IsNullOrWhiteSpace(data.ProfileId);

        private MetaData CreateNewMeta()
        {
            return new MetaData
            {
                ProfileId = Guid.NewGuid().ToString(),
                HasCompletedTutorial = false,
                MetaCurrency = 0,
                ChallengeModeLevel = 0
            };
        }

        private static List<StringFloatEntry> ToEntries(Dictionary<string, float> values)
        {
            var entries = new List<StringFloatEntry>(values?.Count ?? 0);
            if (values == null)
            {
                return entries;
            }

            foreach (KeyValuePair<string, float> pair in values)
            {
                entries.Add(new StringFloatEntry(pair.Key, pair.Value));
            }

            return entries;
        }

        private static Dictionary<string, float> ToStringFloatDictionary(List<StringFloatEntry> entries)
        {
            var values = new Dictionary<string, float>(entries?.Count ?? 0);
            if (entries == null)
            {
                return values;
            }

            foreach (StringFloatEntry entry in entries)
            {
                values[entry.Key] = entry.Value;
            }

            return values;
        }

        private static List<IntFloatEntry> ToEntries(Dictionary<int, float> values)
        {
            var entries = new List<IntFloatEntry>(values?.Count ?? 0);
            if (values == null)
            {
                return entries;
            }

            foreach (KeyValuePair<int, float> pair in values)
            {
                entries.Add(new IntFloatEntry(pair.Key, pair.Value));
            }

            return entries;
        }

        private static Dictionary<int, float> ToIntFloatDictionary(List<IntFloatEntry> entries)
        {
            var values = new Dictionary<int, float>(entries?.Count ?? 0);
            if (entries == null)
            {
                return values;
            }

            foreach (IntFloatEntry entry in entries)
            {
                values[entry.Key] = entry.Value;
            }

            return values;
        }

        [Serializable]
        private sealed class MetaDataDto
        {
            public string ProfileId;
            public bool HasCompletedTutorial;
            public List<string> StoryFacts = new List<string>();
            public List<StringIntEntry> StoryFactIntegers = new List<StringIntEntry>();
            public List<string> CompletedMilestones = new List<string>();
            public List<string> UnlockedMechanics = new List<string>();
            public List<string> PurchasedUpgrades = new List<string>();
            public long MetaCurrency;
            public List<int> UnlockedWeapons = new List<int>();
            public List<StringFloatEntry> BaseStatUpgrades = new List<StringFloatEntry>();
            public List<StringFloatEntry> CombatAcquiredUpgrades = new List<StringFloatEntry>();
            public List<StringFloatEntry> PotentialBudgetUpgrades = new List<StringFloatEntry>();
            public List<StringFloatEntry> PotentialAllocatableUpgrades = new List<StringFloatEntry>();
            public int ChallengeModeLevel;

            public static MetaDataDto FromRuntime(MetaData data)
            {
                return new MetaDataDto
                {
                    ProfileId = data.ProfileId,
                    HasCompletedTutorial = data.HasCompletedTutorial,
                    StoryFacts = new List<string>(data.StoryFacts ?? new HashSet<string>()),
                    StoryFactIntegers = ToEntries(data.StoryFactIntegers),
                    CompletedMilestones = new List<string>(data.CompletedMilestones ?? new HashSet<string>()),
                    UnlockedMechanics = new List<string>(data.UnlockedMechanics ?? new HashSet<string>()),
                    PurchasedUpgrades = new List<string>(data.PurchasedUpgrades ?? new HashSet<string>()),
                    MetaCurrency = data.MetaCurrency,
                    UnlockedWeapons = new List<int>(data.UnlockedWeapons ?? new HashSet<int>()),
                    BaseStatUpgrades = ToEntries(data.BaseStatUpgrades),
                    CombatAcquiredUpgrades = ToEntries(data.CombatAcquiredUpgrades),
                    PotentialBudgetUpgrades = ToEntries(data.PotentialBudgetUpgrades),
                    PotentialAllocatableUpgrades = ToEntries(data.PotentialAllocatableUpgrades),
                    ChallengeModeLevel = data.ChallengeModeLevel
                };
            }

            public MetaData ToRuntime()
            {
                return new MetaData
                {
                    ProfileId = ProfileId,
                    HasCompletedTutorial = HasCompletedTutorial,
                    StoryFacts = new HashSet<string>(StoryFacts ?? new List<string>()),
                    StoryFactIntegers = ToStringIntDictionary(StoryFactIntegers),
                    CompletedMilestones = new HashSet<string>(CompletedMilestones ?? new List<string>()),
                    UnlockedMechanics = new HashSet<string>(UnlockedMechanics ?? new List<string>()),
                    PurchasedUpgrades = new HashSet<string>(PurchasedUpgrades ?? new List<string>()),
                    MetaCurrency = MetaCurrency,
                    UnlockedWeapons = new HashSet<int>(UnlockedWeapons ?? new List<int>()),
                    BaseStatUpgrades = ToStringFloatDictionary(BaseStatUpgrades),
                    CombatAcquiredUpgrades = ToStringFloatDictionary(CombatAcquiredUpgrades),
                    PotentialBudgetUpgrades = ToStringFloatDictionary(PotentialBudgetUpgrades),
                    PotentialAllocatableUpgrades = ToStringFloatDictionary(PotentialAllocatableUpgrades),
                    ChallengeModeLevel = ChallengeModeLevel
                };
            }
        }

        [Serializable]
        private sealed class RunDataDto
        {
            public int Seed;
            public int CurrentStage;
            public int CurrentRoomDepth;
            public long RunCurrency;
            public int DownCount;
            public List<IntFloatEntry> Potentials = new List<IntFloatEntry>();
            public List<IntFloatEntry> BaseStats = new List<IntFloatEntry>();
            public List<IntFloatEntry> AcquiredModifiers = new List<IntFloatEntry>();
            public int EquippedWeaponId;
            public List<int> AcquiredWeaponIds = new List<int>();
            public bool IsSettled;
            public float TotalDamageTaken;
            public int EnemiesKilled;

            public static RunDataDto FromRuntime(RunData data)
            {
                return new RunDataDto
                {
                    Seed = data.Seed,
                    CurrentStage = data.CurrentStage,
                    CurrentRoomDepth = data.CurrentRoomDepth,
                    RunCurrency = data.RunCurrency,
                    DownCount = data.DownCount,
                    Potentials = ToEntries(data.Potentials),
                    BaseStats = ToEntries(data.BaseStats),
                    AcquiredModifiers = ToEntries(data.AcquiredModifiers),
                    EquippedWeaponId = data.EquippedWeaponId,
                    AcquiredWeaponIds = new List<int>(data.AcquiredWeaponIds ?? new HashSet<int>()),
                    IsSettled = data.IsSettled,
                    TotalDamageTaken = data.TotalDamageTaken,
                    EnemiesKilled = data.EnemiesKilled
                };
            }

            public RunData ToRuntime()
            {
                return new RunData
                {
                    Seed = Seed,
                    CurrentStage = CurrentStage,
                    CurrentRoomDepth = CurrentRoomDepth,
                    RunCurrency = RunCurrency,
                    DownCount = DownCount,
                    Potentials = ToIntFloatDictionary(Potentials),
                    BaseStats = ToIntFloatDictionary(BaseStats),
                    AcquiredModifiers = ToIntFloatDictionary(AcquiredModifiers),
                    EquippedWeaponId = EquippedWeaponId,
                    AcquiredWeaponIds = new HashSet<int>(AcquiredWeaponIds ?? new List<int>()),
                    IsSettled = IsSettled,
                    TotalDamageTaken = TotalDamageTaken,
                    EnemiesKilled = EnemiesKilled
                };
            }
        }

        [Serializable]
        private struct StringFloatEntry
        {
            public string Key;
            public float Value;

            public StringFloatEntry(string key, float value)
            {
                Key = key;
                Value = value;
            }
        }

        private static List<StringIntEntry> ToEntries(Dictionary<string, int> values)
        {
            var entries = new List<StringIntEntry>(values?.Count ?? 0);
            if (values == null) return entries;
            foreach (KeyValuePair<string, int> pair in values)
                entries.Add(new StringIntEntry(pair.Key, pair.Value));
            return entries;
        }

        private static Dictionary<string, int> ToStringIntDictionary(List<StringIntEntry> entries)
        {
            var values = new Dictionary<string, int>(entries?.Count ?? 0);
            if (entries == null) return values;
            foreach (StringIntEntry entry in entries)
                values[entry.Key] = entry.Value;
            return values;
        }

        [Serializable]
        private struct StringIntEntry
        {
            public string Key;
            public int Value;

            public StringIntEntry(string key, int value)
            {
                Key = key;
                Value = value;
            }
        }

        [Serializable]
        private struct IntFloatEntry
        {
            public int Key;
            public float Value;

            public IntFloatEntry(int key, float value)
            {
                Key = key;
                Value = value;
            }
        }
    }

    public readonly struct SaveSlotInfo
    {
        public readonly int SlotIndex;
        public readonly bool IsOccupied;
        public readonly bool HasRun;
        public readonly string ProfileId;
        public readonly bool HasCompletedTutorial;
        public readonly DateTime LastWriteTimeUtc;

        public SaveSlotInfo(int slotIndex, bool isOccupied, bool hasRun, string profileId,
            bool hasCompletedTutorial, DateTime lastWriteTimeUtc)
        {
            SlotIndex = slotIndex;
            IsOccupied = isOccupied;
            HasRun = hasRun;
            ProfileId = profileId;
            HasCompletedTutorial = hasCompletedTutorial;
            LastWriteTimeUtc = lastWriteTimeUtc;
        }

        public static SaveSlotInfo Empty(int slotIndex)
            => new SaveSlotInfo(slotIndex, false, false, string.Empty, false, default);

        public static SaveSlotInfo Invalid(int slotIndex)
            => new SaveSlotInfo(slotIndex, false, false, string.Empty, false, default);
    }
}
