using System;
using UnityEngine;

namespace Zephyr.Core.Localization
{
    /// <summary>Resolves the persistent localization gateway once for a UI boundary.</summary>
    public abstract class LocalizedBehaviour : MonoBehaviour
    {
        private ILocalizationGateway _localization;
        private Action<LocaleId> _localeChangedListener;

        protected ILocalizationGateway Localization => _localization;

        protected bool BindLocalization(Action<LocaleId> localeChangedListener)
        {
            if (_localization != null) return true;

            _localization = LocalizationManager.Instance;
            if (_localization == null) return false;

            _localeChangedListener = localeChangedListener;
            if (_localeChangedListener != null)
                _localization.OnLocaleChanged += _localeChangedListener;
            return true;
        }

        protected void UnbindLocalization()
        {
            if (_localization != null && _localeChangedListener != null)
                _localization.OnLocaleChanged -= _localeChangedListener;

            _localeChangedListener = null;
            _localization = null;
        }

        protected string Localize(string key, string fallback, params object[] args)
        {
            if (_localization == null)
                return string.Format(fallback, args ?? Array.Empty<object>());

            return args == null || args.Length == 0
                ? _localization.GetOrFallback(key, fallback)
                : _localization.Format(key, fallback, args);
        }

        protected string FormatLocalized(string key, string fallback, params object[] args)
            => _localization?.Format(key, fallback, args) ?? string.Format(fallback, args ?? Array.Empty<object>());
    }
}
