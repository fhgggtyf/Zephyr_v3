using System;
using UnityEngine;
using UnityEngine.UI;
using Zephyr.Core.Weapons;

namespace Zephyr.Gameplay.Player.UI
{
    /// <summary>
    /// Authored gameplay HUD view. The prefab owns the complete UI hierarchy;
    /// runtime code only injects localized labels and player values.
    /// </summary>
    public sealed class GameplayHudView : MonoBehaviour
    {
        [Header("Bars")]
        [SerializeField] private Image _healthFill;
        [SerializeField] private Text _healthLabel;
        [SerializeField] private Text _healthValue;
        [SerializeField] private Image _staminaFill;
        [SerializeField] private Text _staminaLabel;
        [SerializeField] private Text _staminaValue;
        [SerializeField] private Image _energyFill;
        [SerializeField] private Text _energyLabel;
        [SerializeField] private Text _energyValue;

        [Header("Currency")]
        [SerializeField] private Text _coinsText;

        [Header("Weapons")]
        [SerializeField] private Image _primaryWeaponIcon;
        [SerializeField] private Text _primaryWeaponLabel;
        [SerializeField] private Image _secondaryWeaponIcon;
        [SerializeField] private Text _secondaryWeaponLabel;

        private Func<string, string, string> _localize;

        public Transform Root => transform;

        public void Initialize(Func<string, string, string> localize)
        {
            _localize = localize;
            RefreshLocalization();
            SetVisible(false);
        }

        public void SetVisible(bool visible) => gameObject.SetActive(visible);

        public void SetHealth(float current, float maximum)
            => SetBar(_healthFill, _healthValue, current, maximum);

        public void SetHealthUnbounded()
        {
            if (_healthFill != null) _healthFill.fillAmount = 1f;
            if (_healthValue != null) _healthValue.text = "∞";
        }

        public void SetStamina(float current, float maximum)
            => SetBar(_staminaFill, _staminaValue, current, maximum);

        public void SetEnergy(float current, float maximum)
            => SetBar(_energyFill, _energyValue, current, maximum);

        public void SetCoins(string value)
        {
            if (_coinsText != null) _coinsText.text = value;
        }

        public void SetWeapons(WeaponSO primary, WeaponSO secondary)
        {
            SetWeapon(_primaryWeaponIcon, _primaryWeaponLabel, primary);
            SetWeapon(_secondaryWeaponIcon, _secondaryWeaponLabel, secondary);
        }

        public void RefreshLocalization()
        {
            if (_healthLabel != null)
                _healthLabel.text = Localize("ui.hud.hp.label", "HP");
            if (_staminaLabel != null)
                _staminaLabel.text = Localize("ui.hud.stamina.label", "Stamina");
            if (_energyLabel != null)
                _energyLabel.text = Localize("ui.hud.energy.label", "Energy");
        }

        private void SetBar(Image fill, Text value, float current, float maximum)
        {
            maximum = Mathf.Max(0.01f, maximum);
            current = Mathf.Clamp(current, 0f, maximum);
            if (fill != null) fill.fillAmount = current / maximum;
            if (value != null) value.text = $"{current:F0}/{maximum:F0}";
        }

        private void SetWeapon(Image icon, Text label, WeaponSO weapon)
        {
            if (icon != null)
            {
                icon.sprite = weapon != null ? weapon.Icon : null;
                icon.color = weapon != null && weapon.Icon != null
                    ? Color.white
                    : new Color(0.25f, 0.27f, 0.27f, 1f);
            }

            if (label == null) return;
            if (weapon == null)
            {
                label.text = "-";
                return;
            }

            label.text = !string.IsNullOrWhiteSpace(weapon.DisplayNameLocalizationKey)
                ? Localize(weapon.DisplayNameLocalizationKey, weapon.DisplayName)
                : weapon.DisplayName;
        }

        private string Localize(string key, string fallback)
            => _localize != null ? _localize(key, fallback) : fallback;
    }
}
