using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using Zephyr.Core.Runtime.Lifecycle;

namespace Zephyr.Core.Localization
{
    public enum LocaleId { zh_CN, en_US }

    /// <summary>Persistent locale service. Tables can be populated by authored resources later.</summary>
    [DefaultExecutionOrder(-300)]
    [SingletonLifetime(SingletonLifetime.Persistent, "Persistent")]
    public sealed class LocalizationManager : MonoBehaviour, ILocalizationGateway
    {
        private static LocalizationManager _instance;
        public static LocalizationManager Instance => _instance;
        public LocaleId ActiveLocale { get; private set; } = LocaleId.en_US;
        public LocaleId FallbackLocale => LocaleId.en_US;
        public event Action<LocaleId> OnLocaleChanged;
        private readonly Dictionary<string, string> _entries = new Dictionary<string, string>();

        [Header("Localization Assets")]
        [SerializeField] private TextAsset _englishTable;
        [SerializeField] private TextAsset _chineseTable;
        [SerializeField] private Locale _englishLocale;
        [SerializeField] private Locale _chineseLocale;

        private void Awake()
        {
            if (!SingletonLifecycle.TryClaim(this, ref _instance, nameof(LocalizationManager)))
            {
                Destroy(gameObject);
                return;
            }
            ActiveLocale = (LocaleId)PlayerPrefs.GetInt("zephyr.locale", (int)LocaleId.en_US);
            LoadLocale(ActiveLocale);
            LocalizationSettings.SelectedLocaleChanged += HandleUnityLocaleChanged;
            SyncUnityLocale(ActiveLocale);
        }

        private void OnDestroy()
        {
            LocalizationSettings.SelectedLocaleChanged -= HandleUnityLocaleChanged;
            SingletonLifecycle.Release(this, ref _instance);
        }

        public string Get(string key)
        {
            if (TryGet(key, out string value)) return value;
            Debug.LogWarning($"Localization: missing key '{key}'", this);
            return key;
        }

        public bool TryGet(string key, out string value)
        {
            if (!string.IsNullOrEmpty(key) && _entries.TryGetValue(key, out value) && !string.IsNullOrEmpty(value))
                return true;

            value = null;
            return false;
        }

        public string GetOrFallback(string key, string fallback)
            => TryGet(key, out string value) ? value : fallback;

        public string Format(string key, string fallback, params object[] args)
            => string.Format(GetOrFallback(key, fallback), args ?? Array.Empty<object>());

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
            TextAsset table = locale == LocaleId.zh_CN ? _chineseTable : _englishTable;
            if (table == null) return;
            foreach (Match match in Regex.Matches(table.text, "\\\"([^\\\"]+)\\\"\\s*:\\s*\\\"((?:\\\\.|[^\\\"])*)\\\""))
                _entries[match.Groups[1].Value] = Regex.Unescape(match.Groups[2].Value);
        }

        private void SyncUnityLocale(LocaleId locale)
        {
            Locale unityLocale = locale == LocaleId.zh_CN ? _chineseLocale : _englishLocale;

            if (unityLocale == null)
            {
                Debug.LogWarning($"Unity Localization locale asset for '{locale}' is not assigned.", this);
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
