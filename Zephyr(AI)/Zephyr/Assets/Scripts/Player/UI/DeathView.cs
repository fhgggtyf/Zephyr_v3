using System;
using UnityEngine;
using UnityEngine.UI;
using Zephyr.Core;
using Zephyr.Core.Localization;

namespace Zephyr.Gameplay.Player.UI
{
    /// <summary>
    /// Authored death screen. All images are serialized so artists can replace the
    /// backdrop, panel, and button sprites directly in the prefab.
    /// </summary>
    public sealed class DeathView : MonoBehaviour
    {
        private static Font s_fallbackFont;
        [Header("Replaceable artwork")]
        [SerializeField] private Image _backdrop;
        [SerializeField] private Image _panel;
        [SerializeField] private Image _respawnButtonImage;
        [SerializeField] private Image _giveUpButtonImage;

        [Header("Localized text")]
        [SerializeField] private Text _title;
        [SerializeField] private Text _potentialLoss;
        [SerializeField] private Text _respawnLabel;
        [SerializeField] private Text _giveUpLabel;
        [SerializeField] private Button _respawnButton;
        [SerializeField] private Button _giveUpButton;

        public event Action RespawnRequested;
        public event Action GiveUpRequested;

        private StatType _target;
        private float _before;
        private float _loss;
        private bool _canRespawn;

        private void Awake()
        {
            ResolveReferences();
            _respawnButton?.onClick.AddListener(HandleRespawn);
            _giveUpButton?.onClick.AddListener(HandleGiveUp);
            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.OnLocaleChanged += HandleLocaleChanged;
        }

        private void OnDestroy()
        {
            _respawnButton?.onClick.RemoveListener(HandleRespawn);
            _giveUpButton?.onClick.RemoveListener(HandleGiveUp);
            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.OnLocaleChanged -= HandleLocaleChanged;
        }

        public void Show(StatType target, float before, float loss, bool canRespawn)
        {
            ResolveReferences();
            _target = target;
            _before = before;
            _loss = loss;
            _canRespawn = canRespawn;
            RefreshText();
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        private void RefreshText()
        {
            if (_title != null) _title.text = Localize("death.title", "You Died");
            if (_respawnLabel != null) _respawnLabel.text = Localize("death.respawn", "Respawn");
            if (_giveUpLabel != null) _giveUpLabel.text = Localize("death.give_up", "Give Up");
            if (_potentialLoss != null)
            {
                string statName = Localize("stat." + _target, _target.ToString());
                _potentialLoss.text = $"<color=#FFFFFF>{statName}: {_before:F0}</color> " +
                                      $"<color=#E04444>-{_loss:F0}</color>";
            }

            if (_respawnButton != null) _respawnButton.interactable = _canRespawn;
        }

        private void HandleLocaleChanged(LocaleId _) => RefreshText();
        private void HandleRespawn() { if (_canRespawn) RespawnRequested?.Invoke(); }
        private void HandleGiveUp() => GiveUpRequested?.Invoke();

        private void ResolveReferences()
        {
            _backdrop ??= FindImage("Backdrop");
            _panel ??= FindImage("Panel");
            _respawnButtonImage ??= FindImage("Panel/RespawnButton");
            _giveUpButtonImage ??= FindImage("Panel/GiveUpButton");
            _title ??= FindText("Panel/Title");
            _potentialLoss ??= FindText("Panel/PotentialLoss");
            _respawnLabel ??= FindText("Panel/RespawnButton/Label");
            _giveUpLabel ??= FindText("Panel/GiveUpButton/Label");
            _respawnButton ??= transform.Find("Panel/RespawnButton")?.GetComponent<Button>();
            _giveUpButton ??= transform.Find("Panel/GiveUpButton")?.GetComponent<Button>();
            EnsureFonts();
        }

        private void EnsureFonts()
        {
            if (s_fallbackFont == null)
            {
                s_fallbackFont = Font.CreateDynamicFontFromOSFont(new[]
                {
                    "Microsoft YaHei", "Noto Sans CJK SC", "PingFang SC", "Arial Unicode MS", "Arial"
                }, 32);
                if (s_fallbackFont == null)
                    s_fallbackFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            SetFallbackFont(_title);
            SetFallbackFont(_potentialLoss);
            SetFallbackFont(_respawnLabel);
            SetFallbackFont(_giveUpLabel);
        }

        private static void SetFallbackFont(Text text)
        {
            if (text != null && text.font == null) text.font = s_fallbackFont;
        }

        private Image FindImage(string path) => transform.Find(path)?.GetComponent<Image>();
        private Text FindText(string path) => transform.Find(path)?.GetComponent<Text>();

        private static string Localize(string key, string fallback)
        {
            string value = LocalizationManager.Instance?.Get(key);
            return string.IsNullOrEmpty(value) || value == key ? fallback : value;
        }
    }
}
