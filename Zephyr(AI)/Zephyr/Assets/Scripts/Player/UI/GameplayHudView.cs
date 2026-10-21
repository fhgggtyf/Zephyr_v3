using System;
using UnityEngine;
using UnityEngine.UI;
using Zephyr.Core.UI;
using Zephyr.Core.Weapons;

namespace Zephyr.Gameplay.Player.UI
{
    internal sealed class GameplayHudView
    {
        private readonly Transform _owner;
        private readonly Func<string, string, string> _localize;
        private Canvas _canvas;
        private BarView _healthBar;
        private BarView _staminaBar;
        private BarView _energyBar;
        private Text _coinsText;
        private WeaponView _primaryWeapon;
        private WeaponView _secondaryWeapon;
        private bool _built;

        public GameplayHudView(Transform owner, Func<string, string, string> localize)
        {
            _owner = owner;
            _localize = localize;
        }

        public Transform Root => _canvas != null ? _canvas.transform : _owner;

        public void Build()
        {
            if (_built) return;
            _built = true;

            GameObject canvasObject = new GameObject("GameplayHUD", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(_owner, false);
            _canvas = canvasObject.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;
            _canvas.overrideSorting = true;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960f, 540f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform barsPanel = RuntimeUIFactory.CreateRect("Bars", _canvas.transform);
            RuntimeUIFactory.Place(barsPanel, new Vector2(0f, 1f), new Vector2(260f, 122f),
                new Vector2(145f, -72f));
            _healthBar = CreateBar(barsPanel, 18f, new Color(0.78f, 0.2f, 0.22f, 1f),
                "ui.hud.hp.label", "HP");
            _staminaBar = CreateBar(barsPanel, 51f, new Color(0.86f, 0.65f, 0.16f, 1f),
                "ui.hud.stamina.label", "Stamina");
            _energyBar = CreateBar(barsPanel, 84f, new Color(0.2f, 0.55f, 0.88f, 1f),
                "ui.hud.energy.label", "Energy");

            _coinsText = RuntimeUIFactory.CreateText("Coins", _canvas.transform, 22,
                TextAnchor.MiddleRight, Color.white);
            RuntimeUIFactory.Place((RectTransform)_coinsText.transform, new Vector2(1f, 1f),
                new Vector2(220f, 42f), new Vector2(-130f, -28f));

            RectTransform weaponsPanel = RuntimeUIFactory.CreateRect("Weapons", _canvas.transform);
            RuntimeUIFactory.Place(weaponsPanel, new Vector2(0f, 0f), new Vector2(240f, 96f),
                new Vector2(135f, 60f));
            _primaryWeapon = CreateWeaponView(weaponsPanel, -57f);
            _secondaryWeapon = CreateWeaponView(weaponsPanel, 57f);

            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            if (_canvas != null) _canvas.gameObject.SetActive(visible);
        }

        public void SetHealth(float current, float maximum) => _healthBar?.Set(current, maximum);

        public void SetHealthUnbounded() => _healthBar?.SetUnbounded();

        public void SetStamina(float current, float maximum) => _staminaBar?.Set(current, maximum);

        public void SetEnergy(float current, float maximum) => _energyBar?.Set(current, maximum);

        public void SetCoins(string value)
        {
            if (_coinsText != null) _coinsText.text = value;
        }

        public void SetWeapons(WeaponSO primary, WeaponSO secondary)
        {
            SetWeaponView(_primaryWeapon, primary);
            SetWeaponView(_secondaryWeapon, secondary);
        }

        public void RefreshLocalization()
        {
            _healthBar?.RefreshLocalization();
            _staminaBar?.RefreshLocalization();
            _energyBar?.RefreshLocalization();
        }

        private BarView CreateBar(Transform parent, float y, Color fillColor, string key, string fallback)
        {
            Image background = RuntimeUIFactory.CreateImage("Bar", parent,
                new Color(0.06f, 0.07f, 0.07f, 0.9f));
            RuntimeUIFactory.Place((RectTransform)background.transform, new Vector2(0.5f, 1f),
                new Vector2(238f, 26f), new Vector2(0f, -y));

            Image fill = RuntimeUIFactory.CreateImage("Fill", background.transform, fillColor);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            RuntimeUIFactory.Stretch((RectTransform)fill.transform);

            Text label = RuntimeUIFactory.CreateText("Label", background.transform, 13,
                TextAnchor.MiddleLeft, Color.white);
            label.text = _localize(key, fallback);
            RuntimeUIFactory.Place((RectTransform)label.transform, new Vector2(0f, 0.5f),
                new Vector2(102f, 22f), new Vector2(58f, 0f));

            Text value = RuntimeUIFactory.CreateText("Value", background.transform, 12,
                TextAnchor.MiddleRight, Color.white);
            RuntimeUIFactory.Place((RectTransform)value.transform, new Vector2(1f, 0.5f),
                new Vector2(92f, 22f), new Vector2(-50f, 0f));
            return new BarView(fill, value, label, key, fallback, _localize);
        }

        private WeaponView CreateWeaponView(Transform parent, float x)
        {
            Image slot = RuntimeUIFactory.CreateImage("WeaponSlot", parent,
                new Color(0.08f, 0.09f, 0.09f, 0.94f));
            RuntimeUIFactory.Place((RectTransform)slot.transform, new Vector2(0.5f, 0f),
                new Vector2(100f, 82f), new Vector2(x, 44f));

            Image icon = RuntimeUIFactory.CreateImage("Icon", slot.transform,
                new Color(0.25f, 0.27f, 0.27f, 1f));
            RuntimeUIFactory.Place((RectTransform)icon.transform, new Vector2(0.5f, 0.5f),
                new Vector2(52f, 48f), new Vector2(0f, 8f));

            Text label = RuntimeUIFactory.CreateText("Name", slot.transform, 11,
                TextAnchor.MiddleCenter, Color.white);
            RuntimeUIFactory.Place((RectTransform)label.transform, new Vector2(0.5f, 0f),
                new Vector2(92f, 22f), new Vector2(0f, 12f));
            return new WeaponView(icon, label);
        }

        private void SetWeaponView(WeaponView view, WeaponSO weapon)
        {
            if (view == null) return;
            view.Icon.sprite = weapon != null ? weapon.Icon : null;
            view.Icon.color = weapon != null && weapon.Icon != null
                ? Color.white
                : new Color(0.25f, 0.27f, 0.27f, 1f);
            if (weapon == null)
            {
                view.Label.text = "-";
                return;
            }

            view.Label.text = !string.IsNullOrWhiteSpace(weapon.DisplayNameLocalizationKey)
                ? _localize(weapon.DisplayNameLocalizationKey, weapon.DisplayName)
                : weapon.DisplayName;
        }

        private sealed class BarView
        {
            private readonly Image _fill;
            private readonly Text _value;
            private readonly Text _label;
            private readonly string _localizationKey;
            private readonly string _fallback;
            private readonly Func<string, string, string> _localize;

            public BarView(Image fill, Text value, Text label, string localizationKey, string fallback,
                Func<string, string, string> localize)
            {
                _fill = fill;
                _value = value;
                _label = label;
                _localizationKey = localizationKey;
                _fallback = fallback;
                _localize = localize;
            }

            public void RefreshLocalization() => _label.text = _localize(_localizationKey, _fallback);

            public void SetUnbounded()
            {
                _fill.fillAmount = 1f;
                _value.text = "∞";
            }

            public void Set(float current, float maximum)
            {
                maximum = Mathf.Max(0.01f, maximum);
                current = Mathf.Clamp(current, 0f, maximum);
                _fill.fillAmount = current / maximum;
                _value.text = $"{current:F0}/{maximum:F0}";
            }
        }

        private sealed class WeaponView
        {
            public readonly Image Icon;
            public readonly Text Label;

            public WeaponView(Image icon, Text label)
            {
                Icon = icon;
                Label = label;
            }
        }
    }
}
