using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zephyr.Core;
using Zephyr.Core.Localization;
using Zephyr.Core.Meta;
using Zephyr.Core.Save;
using Zephyr.Core.UI;

namespace Zephyr.Gameplay.Interaction
{
    /// <summary>Run-entry Potential UI. M = P - N is rolled immediately and N is edited by the player.</summary>
    public sealed class PotentialAllocationPanel : LocalizedBehaviour
    {
        [SerializeField] private Text _title;
        [SerializeField] private Text _summary;
        [SerializeField] private Text _remaining;
        [SerializeField] private Transform _rowsRoot;
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _cancelButton;

        private readonly List<RowView> _rows = new List<RowView>();
        private readonly Dictionary<StatType, int> _randomValues = new Dictionary<StatType, int>();
        private readonly Dictionary<StatType, int> _manualValues = new Dictionary<StatType, int>();
        private RunStartInteractable _owner;
        private IMetaGateway _metaGateway;
        private int _budget;
        private int _allocatable;
        private int _remainingManual;
        private bool _rowsBound;
        private bool _rowsValid;

        private void Awake()
        {
            ResolveReferences();
            BindLocalization(HandleLocaleChanged);
            _startButton?.onClick.AddListener(StartRun);
            _cancelButton?.onClick.AddListener(Close);
        }

        private void OnDestroy()
        {
            _startButton?.onClick.RemoveListener(StartRun);
            _cancelButton?.onClick.RemoveListener(Close);
            UnbindLocalization();
            GameplayTimePause.SetModalOpen(this, false);
        }

        private void Update()
        {
            if (Keyboard.current == null) return;
            if (Keyboard.current.escapeKey.wasPressedThisFrame) Close();
            if (Keyboard.current.enterKey.wasPressedThisFrame && CanStartRun()) StartRun();
        }

        public void Show(RunStartInteractable owner)
        {
            _owner = owner;
            _metaGateway = owner?.MetaGateway;
            ResolveReferences();
            BuildAllocation();
            GameplayTimePause.SetModalOpen(this, true);
            Refresh();
            gameObject.SetActive(true);
        }

        public void CloseWithoutDestroyingOwner()
        {
            GameplayTimePause.SetModalOpen(this, false);
            if (gameObject != null) Destroy(gameObject);
        }

        private void Close()
        {
            GameplayTimePause.SetModalOpen(this, false);
            _owner?.ClosePanel();
            if (gameObject != null) Destroy(gameObject);
        }

        private void HandleLocaleChanged(LocaleId _) => Refresh();

        private void BuildAllocation()
        {
            _budget = MetaUpgradeSystem.GetPotentialBudget(_metaGateway?.Meta);
            _allocatable = MetaUpgradeSystem.GetPotentialAllocatable(_metaGateway?.Meta);
            _allocatable = Mathf.Clamp(_allocatable, 0, Mathf.Min(_budget, MetaUpgradeSystem.MaxPotentialAllocatable));
            _randomValues.Clear();
            _manualValues.Clear();
            StatType[] stats = (StatType[])Enum.GetValues(typeof(StatType));
            for (int i = 0; i < stats.Length; i++)
            {
                _randomValues[stats[i]] = 0;
                _manualValues[stats[i]] = 0;
            }

            int randomBudget = Mathf.Max(0, _budget - _allocatable);
            var order = new List<StatType>(stats);
            for (int i = order.Count - 1; i > 0; i--)
            {
                int swapIndex = UnityEngine.Random.Range(0, i + 1);
                StatType temp = order[i];
                order[i] = order[swapIndex];
                order[swapIndex] = temp;
            }

            for (int i = 0; i < Mathf.Min(randomBudget, order.Count); i++)
                _randomValues[order[i]] = 1;
            int randomRemaining = randomBudget - Mathf.Min(randomBudget, stats.Length);
            while (randomRemaining > 0)
            {
                int index = UnityEngine.Random.Range(0, stats.Length);
                if (_randomValues[stats[index]] >= StatConstants.PotentialMax) continue;
                _randomValues[stats[index]]++;
                randomRemaining--;
            }

            _remainingManual = _allocatable;
            BindAuthoredRows(stats);
        }

        private void BindAuthoredRows(StatType[] stats)
        {
            if (_rowsBound) return;

            Transform resolvedRows = _rowsRoot != null ? _rowsRoot : transform.Find("Panel/Rows");
            if (resolvedRows == null)
            {
                Debug.LogError("PotentialAllocationPanel: Panel/Rows is missing from the prefab.", this);
                _rowsValid = false;
                return;
            }

            _rowsRoot = resolvedRows;
            _rows.Clear();
            _rowsValid = true;
            for (int i = 0; i < stats.Length; i++)
            {
                StatType stat = stats[i];
                Transform row = _rowsRoot.Find(string.Concat(stat, "Row"));
                if (row == null)
                {
                    Debug.LogError($"PotentialAllocationPanel: missing authored row for {stat}.", this);
                    _rowsValid = false;
                    continue;
                }

                Text label = row.Find("Label")?.GetComponent<Text>();
                Text value = row.Find("Value")?.GetComponent<Text>();
                Text random = row.Find("Random")?.GetComponent<Text>();
                Text manual = row.Find("Manual")?.GetComponent<Text>();
                Button minus = row.Find("Minus")?.GetComponent<Button>();
                Button plus = row.Find("Plus")?.GetComponent<Button>();
                if (label == null || value == null || random == null || manual == null ||
                    minus == null || plus == null)
                {
                    Debug.LogError($"PotentialAllocationPanel: row {stat} has incomplete authored bindings.", this);
                    _rowsValid = false;
                    continue;
                }

                StatType capturedStat = stat;
                minus.onClick.AddListener(() => ChangeManual(capturedStat, -1));
                plus.onClick.AddListener(() => ChangeManual(capturedStat, 1));
                _rows.Add(new RowView(stat, label, value, random, manual));
            }

            _rowsBound = true;
            _rowsValid &= _rows.Count == stats.Length;
        }

        private void ChangeManual(StatType stat, int delta)
        {
            int randomValue = _randomValues[stat];
            int manual = _manualValues[stat];
            int minimum = randomValue == 0 ? 1 : 0;
            if (delta > 0)
            {
                if (_remainingManual <= 0 || randomValue + manual >= StatConstants.PotentialMax) return;
                manual++;
                _remainingManual--;
            }
            else
            {
                if (manual <= minimum) return;
                manual--;
                _remainingManual++;
            }

            _manualValues[stat] = manual;
            Refresh();
        }

        private void StartRun()
        {
            if (!CanStartRun() || _owner == null) return;
            var potentials = new Dictionary<StatType, float>();
            foreach (StatType stat in Enum.GetValues(typeof(StatType)))
                potentials[stat] = _randomValues[stat] + _manualValues[stat];
            RunEntryPotentialService.SetPending(potentials);
            _owner.BeginRun();
        }

        private void Refresh()
        {
            if (_title != null) _title.text = Localize("potential.entry.title", "Prepare Your Potential");
            if (_summary != null)
                _summary.text = Localize("potential.entry.summary", "Random foundation: {0} | Your points: {1}",
                    _budget - _allocatable, _allocatable);
            if (_remaining != null)
                _remaining.text = Localize("potential.entry.remaining", "Points remaining: {0}", _remainingManual);
            if (_startButton != null) _startButton.interactable = CanStartRun();

            for (int i = 0; i < _rows.Count; i++)
            {
                RowView row = _rows[i];
                int random = _randomValues[row.Stat];
                int manual = _manualValues[row.Stat];
                row.Label.text = Localize(string.Concat("stat.", row.Stat), row.Stat.ToString());
                row.Value.text = (random + manual).ToString();
                row.Random.text = Localize("potential.entry.random_value", "Random: {0}", random);
                row.Manual.text = Localize("potential.entry.manual_value", "You: {0}", manual);
            }
        }

        private void ResolveReferences()
        {
            _title ??= FindText("Panel/Title");
            _summary ??= FindText("Panel/Summary");
            _remaining ??= FindText("Panel/Remaining");
            _rowsRoot ??= transform.Find("Panel/Rows");
            _startButton ??= transform.Find("Panel/StartButton")?.GetComponent<Button>();
            _cancelButton ??= transform.Find("Panel/CancelButton")?.GetComponent<Button>();
        }

        private Text FindText(string path) => transform.Find(path)?.GetComponent<Text>();

        private bool CanStartRun()
        {
            if (!_rowsValid || _remainingManual != 0) return false;
            foreach (StatType stat in Enum.GetValues(typeof(StatType)))
            {
                if (_randomValues[stat] + _manualValues[stat] < 1) return false;
            }

            return true;
        }

        private readonly struct RowView
        {
            public readonly StatType Stat;
            public readonly Text Label;
            public readonly Text Value;
            public readonly Text Random;
            public readonly Text Manual;

            public RowView(StatType stat, Text label, Text value, Text random, Text manual)
            {
                Stat = stat;
                Label = label;
                Value = value;
                Random = random;
                Manual = manual;
            }
        }
    }
}
