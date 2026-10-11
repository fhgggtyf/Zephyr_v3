using System;
using Zephyr.Core.Localization;

namespace Zephyr.Core.Flow.Scenes
{
    public sealed class SettingsPresenter : IDisposable
    {
        private readonly ILocalizationGateway _localization;

        public event Action<LocaleId> LocaleChanged;

        public SettingsPresenter(ILocalizationGateway localization)
        {
            _localization = localization;
            if (_localization != null)
                _localization.OnLocaleChanged += HandleLocaleChanged;
        }

        public string Translate(string key, string fallback)
            => _localization?.GetOrFallback(key, fallback) ?? fallback;

        public string Format(string key, string fallback, params object[] args)
            => _localization?.Format(key, fallback, args)
                ?? string.Format(fallback, args ?? Array.Empty<object>());

        public void SelectLocale(LocaleId locale) => _localization?.SetLocale(locale);

        public void Dispose()
        {
            if (_localization != null)
                _localization.OnLocaleChanged -= HandleLocaleChanged;
            LocaleChanged = null;
        }

        private void HandleLocaleChanged(LocaleId locale) => LocaleChanged?.Invoke(locale);
    }
}
