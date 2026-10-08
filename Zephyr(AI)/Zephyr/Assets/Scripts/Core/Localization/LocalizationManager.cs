using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Zephyr.Core.Localization
{
    public enum LocaleId { zh_CN, en_US }

    /// <summary>Persistent locale service. Tables can be populated by authored resources later.</summary>
    [DefaultExecutionOrder(-300)]
    public sealed class LocalizationManager : MonoBehaviour
    {
        public static LocalizationManager Instance { get; private set; }
        public LocaleId ActiveLocale { get; private set; } = LocaleId.en_US;
        public LocaleId FallbackLocale => LocaleId.en_US;
        public event Action<LocaleId> OnLocaleChanged;
        private readonly Dictionary<string, string> _entries = new Dictionary<string, string>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            ActiveLocale = (LocaleId)PlayerPrefs.GetInt("zephyr.locale", (int)LocaleId.en_US);
            LoadLocale(ActiveLocale);
            LocalizationSettings.SelectedLocaleChanged += HandleUnityLocaleChanged;
            SyncUnityLocale(ActiveLocale);
        }

        private void OnDestroy()
        {
            LocalizationSettings.SelectedLocaleChanged -= HandleUnityLocaleChanged;
            if (Instance == this) Instance = null;
        }

        public string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            if (_entries.TryGetValue(key, out var value)) return value;
            Debug.LogWarning($"Localization: missing key '{key}'", this);
            return key;
        }

        public string Get(string key, params object[] args) => string.Format(Get(key), args ?? Array.Empty<object>());

        public void SetLocale(LocaleId locale)
        {
            bool changed = ActiveLocale != locale;
            ActiveLocale = locale;
            PlayerPrefs.SetInt("zephyr.locale", (int)locale);
            PlayerPrefs.Save();
            LoadLocale(locale);
            SyncUnityLocale(locale);
            if (changed) OnLocaleChanged?.Invoke(locale);
        }

        public void Register(string key, string value) => _entries[key] = value;

        private void LoadLocale(LocaleId locale)
        {
            _entries.Clear();
            TextAsset table = Resources.Load<TextAsset>($"Localization/strings.{locale}");
            if (table == null) return;
            foreach (Match match in Regex.Matches(table.text, "\\\"([^\\\"]+)\\\"\\s*:\\s*\\\"((?:\\\\.|[^\\\"])*)\\\""))
                _entries[match.Groups[1].Value] = Regex.Unescape(match.Groups[2].Value);
        }

        private void SyncUnityLocale(LocaleId locale)
        {
            string resourcePath = locale == LocaleId.zh_CN
                ? "Localization/Locales/Chinese (Simplified) (zh-Hans)"
                : "Localization/Locales/English (en)";
            Locale unityLocale = Resources.Load<Locale>(resourcePath);

            if (unityLocale == null)
            {
                Debug.LogWarning($"Unity Localization locale resource '{resourcePath}' was not found.", this);
                return;
            }

            // Reading LocalizationSettings.SelectedLocale during Awake may call
            // WaitForCompletion on an active Addressables operation. Set the locale
            // directly instead of synchronously reading the async-backed getter.
            try
            {
                LocalizationSettings.SelectedLocale = unityLocale;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Localization: could not set Unity locale '{unityLocale.Identifier.Code}': {exception.Message}", this);
            }
        }

        private void HandleUnityLocaleChanged(Locale unityLocale)
        {
            string expectedCode = ActiveLocale == LocaleId.zh_CN ? "zh-Hans" : "en";
            if (unityLocale == null || unityLocale.Identifier.Code != expectedCode)
                SyncUnityLocale(ActiveLocale);
        }
    }
}
