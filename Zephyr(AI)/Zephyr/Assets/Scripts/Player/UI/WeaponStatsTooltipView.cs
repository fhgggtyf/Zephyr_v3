using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zephyr.Core.Localization;
using Zephyr.Core.Weapons;

namespace Zephyr.Gameplay.Player.UI
{
    public sealed class WeaponStatsTooltipView : MonoBehaviour
    {
        private const string StatsFallback =
            "Damage {0} x{1}\nSpeed {2}\nCrit {3}  Crit Dmg {4}\nPen {5} / {6}\n{7} | {8}";

        [SerializeField] private RectTransform _panel;
        [SerializeField] private Image _icon;
        [SerializeField] private Text _name;
        [SerializeField] private Text _details;
        [SerializeField] private Canvas _canvas;

        private Camera _uiCamera;
        private ILocalizationGateway _localization;
        private WeaponSO _currentWeapon;

        public void Configure(ILocalizationGateway localization)
        {
            if (_localization == localization) return;
            if (_localization != null) _localization.OnLocaleChanged -= HandleLocaleChanged;
            _localization = localization;
            if (_localization != null) _localization.OnLocaleChanged += HandleLocaleChanged;
            if (gameObject.activeSelf && _currentWeapon != null) Show(_currentWeapon);
        }

        private void Awake()
        {
            if (_panel == null) _panel = transform as RectTransform;
            if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
            _uiCamera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _canvas.worldCamera
                : null;
            Hide();
        }

        private void OnDestroy()
        {
            if (_localization != null) _localization.OnLocaleChanged -= HandleLocaleChanged;
        }

        private void Update()
        {
            if (gameObject.activeSelf) FollowPointer();
        }

        public void Show(WeaponSO weapon)
        {
            if (weapon == null)
            {
                Hide();
                return;
            }

            _currentWeapon = weapon;
            if (_icon != null) _icon.sprite = weapon.Icon;
            if (_name != null)
            {
                _name.text = !string.IsNullOrWhiteSpace(weapon.DisplayNameLocalizationKey)
                    ? (_localization?.GetOrFallback(weapon.DisplayNameLocalizationKey, weapon.DisplayName)
                        ?? weapon.DisplayName)
                    : weapon.DisplayName;
            }
            if (_details != null)
            {
                string flatDamage = weapon.BaseFlatDamage.ToString("F0");
                string percentDamage = weapon.BasePercentDamage.ToString("F2");
                string attackSpeed = weapon.BaseAttackSpeed.ToString("F2");
                string critChance = weapon.BaseCritChance.ToString("P0");
                string critMultiplier = weapon.BaseCritMultiplier.ToString("F2");
                string flatPen = weapon.BaseFlatPen.ToString("F0");
                string percentPen = weapon.BasePercentPen.ToString("P0");
                _details.text = _localization != null
                    ? _localization.Format("ui.weapon.tooltip.stats", StatsFallback,
                        flatDamage, percentDamage, attackSpeed, critChance, critMultiplier,
                        flatPen, percentPen, weapon.ElementalType, weapon.Category)
                    : string.Format(StatsFallback, flatDamage, percentDamage, attackSpeed, critChance,
                        critMultiplier, flatPen, percentPen, weapon.ElementalType, weapon.Category);
            }

            gameObject.SetActive(true);
            FollowPointer();
        }

        public void Hide() => gameObject.SetActive(false);

        private void HandleLocaleChanged(LocaleId _)
        {
            if (gameObject.activeSelf && _currentWeapon != null) Show(_currentWeapon);
        }

        private void FollowPointer()
        {
            if (_panel == null || _canvas == null) return;
            RectTransform canvasRect = _canvas.transform as RectTransform;
            Vector2 pointerPosition = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, pointerPosition, _uiCamera, out Vector2 localPoint)) return;

            Vector2 size = _panel.rect.size;
            Vector2 canvasSize = canvasRect.rect.size;
            Vector2 position = localPoint + new Vector2(size.x * 0.5f + 18f, -size.y * 0.5f - 18f);
            position.x = Mathf.Clamp(position.x, -canvasSize.x * 0.5f + size.x * 0.5f,
                canvasSize.x * 0.5f - size.x * 0.5f);
            position.y = Mathf.Clamp(position.y, -canvasSize.y * 0.5f + size.y * 0.5f,
                canvasSize.y * 0.5f - size.y * 0.5f);
            _panel.anchoredPosition = position;
        }
    }
}
