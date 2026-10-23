using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zephyr.Core.Localization;
using Zephyr.Core.Weapons;

namespace Zephyr.Gameplay.Player.UI
{
    public sealed class WeaponSlotHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image _icon;
        [SerializeField] private Text _label;
        [SerializeField] private WeaponStatsTooltipView _tooltip;
        private WeaponSO _weapon;
        private ILocalizationGateway _localization;

        public void Configure(ILocalizationGateway localization)
        {
            _localization = localization;
            RefreshLabel();
        }

        public void SetWeapon(WeaponSO weapon)
        {
            _weapon = weapon;
            if (_icon != null)
            {
                _icon.sprite = weapon != null ? weapon.Icon : null;
                _icon.color = weapon != null && weapon.Icon != null ? Color.white : new Color(1f, 1f, 1f, 0.12f);
            }
            RefreshLabel();
            if (_tooltip != null) _tooltip.Hide();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_weapon != null) _tooltip?.Show(_weapon);
        }

        public void OnPointerExit(PointerEventData eventData) => _tooltip?.Hide();

        private void RefreshLabel()
        {
            if (_label == null) return;
            if (_weapon == null)
            {
                _label.text = "-";
                return;
            }

            _label.text = !string.IsNullOrWhiteSpace(_weapon.DisplayNameLocalizationKey)
                ? (_localization?.GetOrFallback(_weapon.DisplayNameLocalizationKey, _weapon.DisplayName)
                    ?? _weapon.DisplayName)
                : _weapon.DisplayName;
        }
    }
}
