using System;

namespace Zephyr.Core.Localization
{
    public interface ILocalizationGateway
    {
        LocaleId ActiveLocale { get; }
        event Action<LocaleId> OnLocaleChanged;
        string Get(string key);
        bool TryGet(string key, out string value);
        string GetOrFallback(string key, string fallback);
        string Format(string key, string fallback, params object[] args);
        void SetLocale(LocaleId locale);
    }
}
