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
    /// <summary>Run-entry Potential UI. N-M is rolled immediately and M is edited by the player.</summary>
    public sealed class PotentialAllocationPanel : MonoBehaviour
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
        private int _budget;
        private int _allocatable;
        private int _remainingManual;

        private void Awake()
        {
            ResolveReferences();
            _startButton?.onClick.AddListener(StartRun);
            _cancelButton?.onClick.AddListener(Close);
        }

        private void OnDestroy()
        {
            _startButton?.onClick.RemoveListener(StartRun);
            _cancelButton?.onClick.RemoveListener(Close);
            Time.timeScale = 1f;
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
            ResolveReferences();
            BuildAllocation();
            Time.timeScale = 0f;
            Refresh();
            gameObject.SetActive(true);
        }

        public void CloseWithoutDestroyingOwner()
        {
            Time.timeScale = 1f;
            if (gameObject != null) Destroy(gameObject);
        }

        private void Close()
        {
            Time.timeScale = 1f;
            _owner?.ClosePanel();
            if (gameObject != null) Destroy(gameObject);
        }

        private void BuildAllocation()
        {
            _budget = MetaUpgradeSystem.GetPotentialBudget(SaveSystem.Instance?.Meta);
            _allocatable = MetaUpgradeSystem.GetPotentialAllocatable(SaveSystem.Instance?.Meta);
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

            for (int i = 0; i < Mathf.Min(randomBudget, order.Count); i++) _randomValues[order[i]] = 1;
            int randomRemaining = randomBudget - Mathf.Min(randomBudget, stats.Length);
            while (randomRemaining > 0)
            {
                int index = UnityEngine.Random.Range(0, stats.Length);
                if (_randomValues[stats[index]] >= GameConstants.Stats.PotentialMax) continue;
                _randomValues[stats[index]]++;
                randomRemaining--;
            }

            _remainingManual = _allocatable;
            BuildRows(stats);
        }

private void BuildRows(StatType[] stats)
        {
            Transform resolvedRows = _rowsRoot != null ? _rowsRoot : transform.Find("Panel/Rows");
            if (resolvedRows == null)
            {
                Debug.LogError("PotentialAllocationPanel: Panel/Rows is missing from the prefab.", this);
                return;
            }

            _rowsRoot = resolvedRows;
            RectTransform rowsRect = resolvedRows as RectTransform;
            if (rowsRect != null)
            {
                rowsRect.anchorMin = new Vector2(0.5f, 0.5f);
                rowsRect.anchorMax = new Vector2(0.5f, 0.5f);
                rowsRect.pivot = new Vector2(0.5f, 0.5f);
                rowsRect.sizeDelta = new Vector2(740f, 350f);
                rowsRect.anchoredPosition = new Vector2(0f, -42f);
            }

            for (int i = _rowsRoot.childCount - 1; i >= 0; i--)
                Destroy(_rowsRoot.GetChild(i).gameObject);

            _rows.Clear();
            const float rowHeight = 32f;
            for (int i = 0; i < stats.Length; i++)
            {
                StatType stat = stats[i];
                RectTransform row = RuntimeUIFactory.CreateRect(stat + "Row", _rowsRoot);
                RuntimeUIFactory.Place(row, new Vector2(0.5f, 1f), new Vector2(740f, rowHeight),
                    new Vector2(0f, -i * rowHeight - rowHeight * 0.5f));

                Text label = RuntimeUIFactory.CreateText("Label", row, 17, TextAnchor.MiddleLeft, Color.white);
                RuntimeUIFactory.Place(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(210f, 28f),
                    new Vector2(108f, 0f));
                label.text = Localize("stat." + stat, stat.ToString());

                Text value = RuntimeUIFactory.CreateText("Value", row, 17, TextAnchor.MiddleCenter, Color.white);
                RuntimeUIFactory.Place(value.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(72f, 28f),
                    new Vector2(-125f, 0f));

                Text random = RuntimeUIFactory.CreateText("Random", row, 15, TextAnchor.MiddleCenter,
                    new Color(0.72f, 0.84f, 1f));
                RuntimeUIFactory.Place(random.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(128f, 28f),
                    new Vector2(8f, 0f));

                Text manual = RuntimeUIFactory.CreateText("Manual", row, 15, TextAnchor.MiddleCenter,
                    new Color(1f, 0.85f, 0.48f));
                RuntimeUIFactory.Place(manual.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(128f, 28f),
                    new Vector2(145f, 0f));

                Text minusLabel;
                Button minus = RuntimeUIFactory.CreateButton("Minus", row,
                    new Color(0.18f, 0.22f, 0.28f), Color.white, out minusLabel);
                RuntimeUIFactory.Place(minus.GetComponent<RectTransform>(), new Vector2(1f, 0.5f),
                    new Vector2(34f, 28f), new Vector2(-48f, 0f));

                Text plusLabel;
                Button plus = RuntimeUIFactory.CreateButton("Plus", row,
                    new Color(0.18f, 0.32f, 0.25f), Color.white, out plusLabel);
                RuntimeUIFactory.Place(plus.GetComponent<RectTransform>(), new Vector2(1f, 0.5f),
                    new Vector2(34f, 28f), new Vector2(-8f, 0f));

                minusLabel.text = "-";
                plusLabel.text = "+";
                minus.onClick.AddListener(() => ChangeManual(stat, -1));
                plus.onClick.AddListener(() => ChangeManual(stat, 1));
                _rows.Add(new RowView(stat, value, random, manual));
            }
        }

        private void ChangeManual(StatType stat, int delta)
        {
            int randomValue = _randomValues[stat];
            int manual = _manualValues[stat];
            int minimum = randomValue == 0 ? 1 : 0;
            if (delta > 0)
            {
                if (_remainingManual <= 0 || randomValue + manual >= GameConstants.Stats.PotentialMax) return;
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
            if (_remainingManual != 0 || _owner == null) return;
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
            if (_remainingManual != 0) return false;
            foreach (StatType stat in Enum.GetValues(typeof(StatType)))
            {
                if (_randomValues[stat] + _manualValues[stat] < 1) return false;
            }
            return true;
        }

        private static string Localize(string key, string fallback, params object[] args)
        {
            string value = LocalizationManager.Instance?.Get(key, args);
            return string.IsNullOrEmpty(value) || value == key ? string.Format(fallback, args) : value;
        }

        private readonly struct RowView
        {
            public readonly StatType Stat;
            public readonly Text Value;
            public readonly Text Random;
            public readonly Text Manual;

            public RowView(StatType stat, Text value, Text random, Text manual)
            {
                Stat = stat;
                Value = value;
                Random = random;
                Manual = manual;
            }
        }
    }
}
