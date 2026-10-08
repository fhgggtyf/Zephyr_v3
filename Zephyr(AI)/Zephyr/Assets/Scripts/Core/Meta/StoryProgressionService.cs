using System;
using System.Collections.Generic;
using UnityEngine;
using Zephyr.Core.Events;
using Zephyr.Core.Save;

namespace Zephyr.Core.Meta
{
    [DefaultExecutionOrder(-450)]
    public sealed class StoryProgressionService : MonoBehaviour
    {
        public static StoryProgressionService Instance { get; private set; }

        [SerializeField] private StoryProgressionDatabaseSO _database;
        [SerializeField] private bool _saveImmediately = true;

        private readonly Dictionary<string, bool> _boolFacts = new Dictionary<string, bool>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _intFacts = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly HashSet<string> _completedMilestones = new HashSet<string>(StringComparer.Ordinal);
        private string _boundProfileId;

        public StoryProgressionDatabaseSO Database => _database;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            BindToCurrentSave();
            EvaluateAll();
        }

        private void Update()
        {
            // Save slot selection happens from the Main Menu after Persistent is booted.
            // This lightweight identity check binds the service exactly once per profile.
            BindToCurrentSave();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool GetBool(StoryFactSO fact)
            => fact != null && _boolFacts.TryGetValue(fact.Id, out bool value) && value;

        public bool GetBool(string factId)
            => !string.IsNullOrWhiteSpace(factId) && _boolFacts.TryGetValue(factId, out bool value) && value;

        public int GetInt(StoryFactSO fact)
            => fact != null && _intFacts.TryGetValue(fact.Id, out int value) ? value : 0;

        public int GetInt(string factId)
            => !string.IsNullOrWhiteSpace(factId) && _intFacts.TryGetValue(factId, out int value) ? value : 0;

        public bool IsCompleted(MilestoneSO milestone)
            => milestone != null && IsCompleted(milestone.Id);

        public bool IsCompleted(string milestoneId)
            => !string.IsNullOrWhiteSpace(milestoneId) && _completedMilestones.Contains(milestoneId);

        public bool SetBool(StoryFactSO fact, bool value)
            => fact != null && SetBool(fact.Id, value);

        public bool SetInt(StoryFactSO fact, int value)
            => fact != null && SetInt(fact.Id, value);

        public bool SetBool(string factId, bool value)
        {
            if (string.IsNullOrWhiteSpace(factId)) return false;
            BindToCurrentSave();
            bool changed = !_boolFacts.TryGetValue(factId, out bool oldValue) || oldValue != value;
            _boolFacts[factId] = value;
            _intFacts.Remove(factId);
            PersistFact(factId, value);
            if (changed) PublishFactChanged(factId);
            if (changed) EvaluateAll();
            return changed;
        }

        public bool SetInt(string factId, int value)
        {
            if (string.IsNullOrWhiteSpace(factId)) return false;
            BindToCurrentSave();
            bool changed = !_intFacts.TryGetValue(factId, out int oldValue) || oldValue != value;
            _intFacts[factId] = value;
            _boolFacts.Remove(factId);
            PersistFact(factId, value);
            if (changed) PublishFactChanged(factId);
            if (changed) EvaluateAll();
            return changed;
        }

        public bool Complete(MilestoneSO milestone)
            => milestone != null && Complete(milestone.Id, milestone);

        public bool Complete(string milestoneId, MilestoneSO definition = null)
        {
            if (string.IsNullOrWhiteSpace(milestoneId))
                return false;

            BindToCurrentSave();
            MetaData meta = SaveSystem.Instance?.Meta;
            if (meta == null || _completedMilestones.Contains(milestoneId)) return false;
            if (definition != null && definition.CompletionCondition != null
                && !definition.CompletionCondition.Evaluate(this)) return false;

            _completedMilestones.Add(milestoneId);
            meta.CompletedMilestones.Add(milestoneId);
            if (_saveImmediately) SaveSystem.Instance?.SaveMeta();

            if (definition != null)
            {
                IReadOnlyList<MilestoneEffectSO> effects = definition.Effects;
                for (int i = 0; i < effects.Count; i++)
                    effects[i]?.Apply(this);
            }

            EventBus.Instance?.Publish(new MilestoneCompletedEvent(milestoneId, definition));
            return true;
        }

        public void EvaluateAll()
        {
            BindToCurrentSave();
            if (_database == null) return;

            IReadOnlyList<MilestoneSO> milestones = _database.Milestones;
            for (int i = 0; i < milestones.Count; i++)
            {
                MilestoneSO milestone = milestones[i];
                if (milestone == null || IsCompleted(milestone)) continue;
                MilestoneConditionSO condition = milestone.CompletionCondition;
                if (condition != null && condition.Evaluate(this))
                    Complete(milestone);
            }
        }

        public bool ReportBoolFact(string factId, bool value = true) => SetBool(factId, value);
        public bool ReportIntFact(string factId, int value) => SetInt(factId, value);
        public bool ReportMilestone(string milestoneId) => Complete(milestoneId);

        private void BindToCurrentSave()
        {
            MetaData meta = SaveSystem.Instance != null ? SaveSystem.Instance.Meta : null;
            if (meta == null || string.Equals(_boundProfileId, meta.ProfileId, StringComparison.Ordinal)) return;

            _boundProfileId = meta.ProfileId;
            _boolFacts.Clear();
            _intFacts.Clear();
            _completedMilestones.Clear();

            if (meta.StoryFacts != null)
            {
                foreach (string factId in meta.StoryFacts)
                    _boolFacts[factId] = true;
            }

            if (meta.StoryFactIntegers != null)
            {
                foreach (KeyValuePair<string, int> pair in meta.StoryFactIntegers)
                    _intFacts[pair.Key] = pair.Value;
            }

            if (meta.CompletedMilestones != null)
                _completedMilestones.UnionWith(meta.CompletedMilestones);

            EvaluateAll();
        }

        private void PersistFact(string factId, bool value)
        {
            MetaData meta = SaveSystem.Instance?.Meta;
            if (meta == null) return;
            meta.StoryFactIntegers.Remove(factId);
            if (value) meta.StoryFacts.Add(factId);
            else meta.StoryFacts.Remove(factId);
            if (_saveImmediately) SaveSystem.Instance.SaveMeta();
        }

        private void PersistFact(string factId, int value)
        {
            MetaData meta = SaveSystem.Instance?.Meta;
            if (meta == null) return;
            meta.StoryFacts.Remove(factId);
            meta.StoryFactIntegers[factId] = value;
            if (_saveImmediately) SaveSystem.Instance.SaveMeta();
        }

        private static void PublishFactChanged(string factId)
            => EventBus.Instance?.Publish(new StoryFactChangedEvent(factId));
    }

    public readonly struct StoryFactChangedEvent
    {
        public readonly string FactId;
        public StoryFactChangedEvent(string factId) => FactId = factId;
    }

    public readonly struct MilestoneCompletedEvent
    {
        public readonly string MilestoneId;
        public readonly MilestoneSO Definition;

        public MilestoneCompletedEvent(string milestoneId, MilestoneSO definition)
        {
            MilestoneId = milestoneId;
            Definition = definition;
        }
    }
}
