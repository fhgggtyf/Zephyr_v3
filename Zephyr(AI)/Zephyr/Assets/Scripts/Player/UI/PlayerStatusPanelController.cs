using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zephyr.Contracts.Events;
using Zephyr.Contracts.Interfaces;
using Zephyr.Core;
using Zephyr.Core.Events;
using Zephyr.Core.Flow;
using Zephyr.Core.Health;
using Zephyr.Core.Localization;
using Zephyr.Core.Stats;
using Zephyr.Core.UI;
using Zephyr.Core.Weapons;
using Zephyr.Gameplay.Player.Core;

namespace Zephyr.Gameplay.Player.UI
{
    public sealed class PlayerStatusPanelController : MonoBehaviour, IGameManagerDependencyConsumer
    {
        [SerializeField] private CanvasGroup _panelGroup;
        [SerializeField] private Button _potentialButton;
        [SerializeField] private Button _statsButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Image _portrait;
        [SerializeField] private WeaponSlotHover _primaryWeapon;
        [SerializeField] private WeaponSlotHover _secondaryWeapon;
        [SerializeField] private WeaponStatsTooltipView _tooltip;
        [SerializeField] private Sprite _portraitSprite;
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private Scrollbar _scrollbar;
        [SerializeField] private Text _title;
        [SerializeField] private Text _potentialLabel;
        [SerializeField] private Text _statsLabel;
        [SerializeField] private Text _closeLabel;
        [SerializeField] private Text _weaponsTitle;
        [SerializeField] private List<PlayerStatusValueRow> _rows = new List<PlayerStatusValueRow>();

        private IEventBusGateway _eventBus;
        private ILocalizationGateway _localization;
        private StatsCore _stats;
        private HealthComponent _health;
        private PlayerResourceController _resources;
        private WeaponController _weapons;
        private bool _subscribed;
        private bool _isOpen;
        private readonly Dictionary<PlayerStatusValueRow, string> _rowLabels = new Dictionary<PlayerStatusValueRow, string>();

        public void Configure(IGameManagerServices services)
        {
            if (_subscribed) _eventBus?.Unsubscribe<PlayerTargetChangedEvent>(HandlePlayerChanged);
            _subscribed = false;
            _eventBus = services?.EventBus;

            _localization = services?.Localization;
            if (_localization != null)
            {
                _localization.OnLocaleChanged -= HandleLocaleChanged;
                _localization.OnLocaleChanged += HandleLocaleChanged;
            }

            _primaryWeapon?.Configure(_localization);
            _secondaryWeapon?.Configure(_localization);
            _tooltip?.Configure(_localization);
            RefreshRowLabels();
            RefreshStaticText();

            if (_eventBus == null) return;
            _eventBus.Subscribe<PlayerTargetChangedEvent>(HandlePlayerChanged);
            _subscribed = true;
            if (_eventBus.TryGetLatest(out PlayerTargetChangedEvent current)) HandlePlayerChanged(current);
        }

        private void Awake()
        {
            if (_portrait != null) _portrait.sprite = _portraitSprite;
            _rows.Clear();
            _rows.AddRange(GetComponentsInChildren<PlayerStatusValueRow>(true));
            RefreshRowLabels();
            _potentialButton?.onClick.AddListener(ShowPotential);
            _statsButton?.onClick.AddListener(ShowStats);
            _closeButton?.onClick.AddListener(Close);
            SetOpen(false);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame) Toggle();
            if (_isOpen) Refresh();
        }

        private void OnDestroy()
        {
            if (_subscribed) _eventBus?.Unsubscribe<PlayerTargetChangedEvent>(HandlePlayerChanged);
            if (_localization != null) _localization.OnLocaleChanged -= HandleLocaleChanged;
            GameplayTimePause.SetModalOpen(this, false);
        }

        private void HandleLocaleChanged(LocaleId _)
        {
            RefreshRowLabels();
            RefreshStaticText();
            RefreshWeapons();
            Refresh();
        }

        private void RefreshStaticText()
        {
            SetLabel(_title, "ui.player_status.title", "Player Status");
            SetLabel(_potentialLabel, "ui.player_status.tab.potential", "Potential");
            SetLabel(_statsLabel, "ui.player_status.tab.stats", "Stats");
            SetLabel(_closeLabel, "common.close", "Close");
            SetLabel(_weaponsTitle, "ui.player_status.weapons", "Weapons");
        }

        private void SetLabel(Text target, string key, string fallback)
        {
            if (target == null) return;
            target.text = _localization != null ? _localization.GetOrFallback(key, fallback) : fallback;
        }

        private void RefreshRowLabels()
        {
            _rowLabels.Clear();
            for (int i = 0; i < _rows.Count; i++)
            {
                PlayerStatusValueRow row = _rows[i];
                if (row == null) continue;
                _rowLabels[row] = _localization != null
                    ? _localization.GetOrFallback("stat." + row.Key, row.Key)
                    : row.Key;
            }
        }

        private void HandlePlayerChanged(PlayerTargetChangedEvent change)
        {
            GameObject player = change.Player;
            _stats = player != null ? player.GetComponent<StatsCore>() : null;
            _health = player != null ? player.GetComponent<HealthComponent>() : null;
            _resources = player != null ? player.GetComponent<PlayerResourceController>() : null;
            _weapons = player != null ? player.GetComponentInChildren<WeaponController>(true) : null;
            RefreshWeapons();
            Refresh();
        }

        private void RefreshWeapons()
        {
            _primaryWeapon?.SetWeapon(_weapons?.GetWeapon(WeaponSlot.Primary));
            _secondaryWeapon?.SetWeapon(_weapons?.GetWeapon(WeaponSlot.Secondary));
        }

        private void Toggle()
        {
            if (_isOpen) Close();
            else Open();
        }

        private void Open()
        {
            SetOpen(true);
            ShowPotential();
            Refresh();
        }

        private void Close()
        {
            _tooltip?.Hide();
            SetOpen(false);
        }

        private void SetOpen(bool open)
        {
            _isOpen = open;
            if (_panelGroup == null) return;
            _panelGroup.alpha = open ? 1f : 0f;
            _panelGroup.interactable = open;
            _panelGroup.blocksRaycasts = open;
            GameplayTimePause.SetModalOpen(this, open);
        }

        private void ShowPotential()
        {
            SetContentVisibility(true);
            Refresh();
        }

        private void ShowStats()
        {
            SetContentVisibility(false);
            Refresh();
        }

        private void SetContentVisibility(bool potential)
        {
            foreach (PlayerStatusValueRow row in _rows)
            {
                if (row == null) continue;
                bool visible = potential ? row.Kind == PlayerStatusValueKind.Potential
                    : row.Kind != PlayerStatusValueKind.Potential;
                row.gameObject.SetActive(visible);
            }

            if (_scrollRect != null)
            {
                _scrollRect.vertical = !potential;
                _scrollRect.verticalNormalizedPosition = 1f;
            }
            if (_scrollbar != null) _scrollbar.gameObject.SetActive(!potential);
        }

        private void Refresh()
        {
            foreach (PlayerStatusValueRow row in _rows)
            {
                if (row == null) continue;
                float value = ReadValue(row);
                string label = _rowLabels.TryGetValue(row, out string cached) ? cached : row.Key;
                row.Set(label, value.ToString("0.##"));
            }
        }

        private float ReadValue(PlayerStatusValueRow row)
        {
            if (_stats == null) return 0f;
            if (row.Kind == PlayerStatusValueKind.Core && Enum.TryParse(row.Key, out StatType core))
                return _stats.GetStatValue(core);
            if (row.Kind == PlayerStatusValueKind.Potential && Enum.TryParse(row.Key, out StatType potential))
                return _stats.GetPotential(potential);
            if (row.Kind == PlayerStatusValueKind.Meta && Enum.TryParse(row.Key, out MetaStatType meta))
                return _stats.GetMetaStat(meta);
            if (row.Kind == PlayerStatusValueKind.Category) return _stats.GetCategoryMultiplier(row.Key);
            if (row.Kind == PlayerStatusValueKind.Resource)
            {
                return row.Key switch
                {
                    "CurrentHp" => _health?.CurrentHp ?? 0f,
                    "CurrentStamina" => _resources?.CurrentStamina ?? 0f,
                    "CurrentEnergy" => _resources?.CurrentEnergy ?? 0f,
                    "MaxHp" => _health?.MaxHp ?? 0f,
                    "MaxStamina" => _resources?.MaxStamina ?? 0f,
                    "MaxEnergy" => _resources?.MaxEnergy ?? 0f,
                    _ => 0f
                };
            }
            return 0f;
        }
    }
}
