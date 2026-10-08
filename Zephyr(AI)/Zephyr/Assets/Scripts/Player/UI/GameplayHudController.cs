using UnityEngine;
using UnityEngine.UI;
using Zephyr.Core.Flow;
using Zephyr.Core.Health;
using Zephyr.Core.Localization;
using Zephyr.Core.Save;
using Zephyr.Core.UI;
using Zephyr.Core.Weapons;
using Zephyr.Gameplay.Player.Core;
using Zephyr.Gameplay.Player.Flow;

namespace Zephyr.Gameplay.Player.UI
{
    /// <summary>
    /// Gameplay HUD hosted by GameManager. It is visible only while a player scene
    /// is loaded, which keeps tutorial/run presentation out of the menu and hubs.
    /// </summary>
    public sealed class GameplayHudController : MonoBehaviour
    {
        private Canvas _canvas;
        private HealthComponent _health;
        private PlayerResourceController _resources;
        private WeaponController _weapons;
        private BarView _healthBar;
        private BarView _staminaBar;
        private BarView _energyBar;
        private Text _coinsText;
        private WeaponView _primaryWeapon;
        private WeaponView _secondaryWeapon;
        private bool _built;

        private void Start()
        {
            BuildInterface();
            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.OnLocaleChanged += HandleLocaleChanged;

            if (PlayerSpawner.Instance != null)
            {
                PlayerSpawner.Instance.PlayerSpawned += BindPlayer;
                PlayerSpawner.Instance.PlayerDespawned += UnbindPlayer;
                BindPlayer(PlayerSpawner.Instance.CurrentPlayer);
            }
        }

        private void OnDestroy()
        {
            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.OnLocaleChanged -= HandleLocaleChanged;

            if (PlayerSpawner.Instance != null)
            {
                PlayerSpawner.Instance.PlayerSpawned -= BindPlayer;
                PlayerSpawner.Instance.PlayerDespawned -= UnbindPlayer;
            }

            UnsubscribeWeapons();
        }

        private void LateUpdate()
        {
            if (!_built || _health == null) return;

            if (_health.IsHealthUnbounded)
                _healthBar.SetUnbounded();
            else
                _healthBar.Set(_health.CurrentHp, _health.MaxHp);
            _staminaBar.Set(_resources != null ? _resources.CurrentStamina : 0f,
                _resources != null ? _resources.MaxStamina : 1f);
            _energyBar.Set(_resources != null ? _resources.CurrentEnergy : 0f,
                _resources != null ? _resources.MaxEnergy : 1f);

            long coins = SaveSystem.Instance?.CurrentRun?.RunCurrency ?? 0L;
            _coinsText.text = Format("hud.coins", "Coins: {0}", coins);
            RefreshWeapons();
        }

        private void BuildInterface()
        {
            if (_built) return;
            _built = true;

            GameObject canvasObject = new GameObject("GameplayHUD", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
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
            label.text = Localize(key, fallback);
            RuntimeUIFactory.Place((RectTransform)label.transform, new Vector2(0f, 0.5f),
                new Vector2(102f, 22f), new Vector2(58f, 0f));

            Text value = RuntimeUIFactory.CreateText("Value", background.transform, 12,
                TextAnchor.MiddleRight, Color.white);
            RuntimeUIFactory.Place((RectTransform)value.transform, new Vector2(1f, 0.5f),
                new Vector2(92f, 22f), new Vector2(-50f, 0f));
            return new BarView(fill, value, label, key, fallback);
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

        private void BindPlayer(GameObject player)
        {
            UnsubscribeWeapons();
            _health = player != null ? player.GetComponent<HealthComponent>() : null;
            _resources = player != null ? player.GetComponent<PlayerResourceController>() : null;
            _weapons = player != null ? player.GetComponentInChildren<WeaponController>(true) : null;

            if (_weapons != null)
            {
                _weapons.WeaponEquipped += HandleWeaponChanged;
                _weapons.ActiveSlotChanged += HandleActiveSlotChanged;
            }

            SetVisible(player != null);
            RefreshWeapons();
        }

        private void UnbindPlayer()
        {
            UnsubscribeWeapons();
            _health = null;
            _resources = null;
            _weapons = null;
            SetVisible(false);
        }

        private void UnsubscribeWeapons()
        {
            if (_weapons == null) return;
            _weapons.WeaponEquipped -= HandleWeaponChanged;
            _weapons.ActiveSlotChanged -= HandleActiveSlotChanged;
        }

        private void HandleWeaponChanged(WeaponSlot slot, WeaponSO weapon) => RefreshWeapons();
        private void HandleActiveSlotChanged(WeaponSlot slot) => RefreshWeapons();

        private void HandleLocaleChanged(LocaleId _)
        {
            _healthBar?.RefreshLocalization();
            _staminaBar?.RefreshLocalization();
            _energyBar?.RefreshLocalization();
        }

        private void RefreshWeapons()
        {
            if (_weapons == null) return;
            SetWeaponView(_primaryWeapon, _weapons.GetWeapon(WeaponSlot.Primary));
            SetWeaponView(_secondaryWeapon, _weapons.GetWeapon(WeaponSlot.Secondary));
        }

        private static void SetWeaponView(WeaponView view, WeaponSO weapon)
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
                ? Localize(weapon.DisplayNameLocalizationKey, weapon.DisplayName)
                : weapon.DisplayName;
        }

        private void SetVisible(bool visible)
        {
            if (_canvas != null) _canvas.gameObject.SetActive(visible);
        }

        private static string Localize(string key, string fallback)
        {
            string value = LocalizationManager.Instance?.Get(key);
            return string.IsNullOrEmpty(value) || value == key ? fallback : value;
        }

        private static string Format(string key, string fallback, params object[] args)
            => string.Format(Localize(key, fallback), args);

        private sealed class BarView
        {
            private readonly Image _fill;
            private readonly Text _value;
            private readonly Text _label;
            private readonly string _localizationKey;
            private readonly string _fallback;

            public BarView(Image fill, Text value, Text label, string localizationKey, string fallback)
            {
                _fill = fill;
                _value = value;
                _label = label;
                _localizationKey = localizationKey;
                _fallback = fallback;
            }

            public void RefreshLocalization() => _label.text = Localize(_localizationKey, _fallback);

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
