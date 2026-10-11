using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Zephyr.Core;
using Zephyr.Core.Stats;
using Zephyr.Core.Localization;

namespace Zephyr.UI
{
    /// <summary>
    /// Displays all player stats in a panel. Subscribes to StatsCore for updates.
    /// Attach to a UI panel and assign stat rows in the Inspector.
    /// </summary>
    public class StatsPanelUI : LocalizedBehaviour
    {
        [Header("Source")]
        [SerializeField] private StatsCore _statsCore;

        [Header("Stat Rows")]
        [SerializeField] private List<StatRow> _rows = new List<StatRow>();

        [System.Serializable]
        public class StatRow
        {
            public StatType statType;
            public Text labelText;
            public Text valueText;
            public string format = "{0:F1}";
        }

        private void Start()
        {
            Refresh();
        }

        private void OnEnable()
        {
            BindLocalization(HandleLocaleChanged);
            Refresh();
        }

        private void OnDisable()
        {
            UnbindLocalization();
        }

        public void Refresh()
        {
            if (_statsCore == null) return;

            foreach (StatRow row in _rows)
            {
                if (row == null) continue;

                if (row.labelText != null)
                    row.labelText.text = LocalizeStat(row.statType);

                float value = _statsCore.GetStatValue(row.statType);
                if (row.valueText != null)
                    row.valueText.text = string.Format(row.format, value);
            }
        }

        private void HandleLocaleChanged(LocaleId _) => Refresh();

        private string LocalizeStat(StatType statType)
        {
            string fallback = statType.ToString();
            return Localize("stat." + statType, fallback);
        }

        public void SetStatsSource(StatsCore statsCore)
        {
            _statsCore = statsCore;
            Refresh();
        }
    }
}
