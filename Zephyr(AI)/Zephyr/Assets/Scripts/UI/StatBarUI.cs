using UnityEngine;
using UnityEngine.UI;
using Zephyr.Core.Localization;

namespace Zephyr.UI
{
    /// <summary>
    /// Generic stat bar UI. Displays current/max as a slider or image fill.
    /// Attach to any UI bar (Slider or Image with Fill method).
    /// </summary>
    public class StatBarUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Slider _slider;
        [SerializeField] private Image _fillImage;
        [SerializeField] private Text _labelText;
        [SerializeField] private Text _valueText;

        [Header("Display")]
        [SerializeField] private string _label = "HP";
        [SerializeField] private string _labelLocalizationKey = "ui.hud.hp.label";
        [SerializeField] private bool _showValueText = true;
        [SerializeField] private string _valueFormat = "{0:F0}/{1:F0}";

        private float _currentValue;
        private float _maxValue;

        private void Awake()
        {
            if (_slider == null && _fillImage == null)
            {
                _slider = GetComponentInParent<Slider>();
                if (_slider == null && _fillImage == null)
                    _fillImage = GetComponent<Image>();
            }

            RefreshLabel();
        }

        private void OnEnable()
        {
            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.OnLocaleChanged += HandleLocaleChanged;
            RefreshLabel();
        }

        private void OnDisable()
        {
            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.OnLocaleChanged -= HandleLocaleChanged;
        }

        private void HandleLocaleChanged(LocaleId _) => RefreshLabel();

        private void RefreshLabel()
        {
            if (_labelText == null) return;
            string value = LocalizationManager.Instance?.Get(_labelLocalizationKey);
            _labelText.text = string.IsNullOrEmpty(value) || value == _labelLocalizationKey ? _label : value;
        }

        public void UpdateBar(float currentValue, float maxValue)
        {
            _currentValue = currentValue;
            _maxValue = Mathf.Max(0.01f, maxValue);

            float normalized = Mathf.Clamp01(currentValue / _maxValue);

            if (_slider != null)
            {
                _slider.minValue = 0f;
                _slider.maxValue = _maxValue;
                _slider.value = currentValue;
            }

            if (_fillImage != null)
            {
                _fillImage.fillAmount = normalized;
            }

            if (_valueText != null && _showValueText)
            {
                _valueText.text = string.Format(_valueFormat, currentValue, _maxValue);
            }
        }
    }
}
