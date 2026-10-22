using UnityEngine;
using Zephyr.Contracts.Events;
using Zephyr.Core.Events;
using Zephyr.Core.Flow;
using Zephyr.Core.Health;
using Zephyr.Core.Localization;
using Zephyr.Core.Save;
using Zephyr.Contracts.Interfaces;
using Zephyr.Core.Weapons;
using Zephyr.Gameplay.Player.Core;

namespace Zephyr.Gameplay.Player.UI
{
    /// <summary>
    /// Gameplay HUD hosted by GameManager. It is visible only while a player scene
    /// is loaded, which keeps tutorial/run presentation out of the menu and hubs.
    /// </summary>
    public sealed class GameplayHudController : LocalizedBehaviour, IGameManagerDependencyConsumer
    {
        [SerializeField] private GameplayHudView _viewPrefab;

        private HealthComponent _health;
        private PlayerResourceController _resources;
        private WeaponController _weapons;
        private GameplayHudView _view;
        private bool _subscribedToPlayerTarget;
        private ISaveDataGateway _saveData;
        private IEventBusGateway _eventBus;

        internal Transform ViewRoot => _view?.Root ?? transform;

        private void Start()
        {
            if (_viewPrefab == null)
            {
                Debug.LogError("GameplayHudController is missing its authored GameplayHudView prefab.", this);
                return;
            }

            _view = Instantiate(_viewPrefab, transform);
            _view.Initialize((localizationKey, fallback) => Localize(localizationKey, fallback));
            BindLocalization(HandleLocaleChanged);

            if (_eventBus != null)
            {
                _eventBus.Subscribe<PlayerTargetChangedEvent>(HandlePlayerTargetChanged);
                _subscribedToPlayerTarget = true;
                if (_eventBus.TryGetLatest(out PlayerTargetChangedEvent current))
                    HandlePlayerTargetChanged(current);
                _eventBus.Publish(new GameplayHudReady());
            }
        }

        public void Configure(IGameManagerServices services)
        {
            _saveData = services?.SaveData;
            _eventBus = services?.EventBus;
        }

        private void OnDestroy()
        {
            UnbindLocalization();

            if (_subscribedToPlayerTarget && _eventBus != null)
                _eventBus.Unsubscribe<PlayerTargetChangedEvent>(HandlePlayerTargetChanged);
            _subscribedToPlayerTarget = false;

            UnsubscribeWeapons();
        }

        private void LateUpdate()
        {
            if (_view == null || _health == null) return;

            if (_health.IsHealthUnbounded)
                _view.SetHealthUnbounded();
            else
                _view.SetHealth(_health.CurrentHp, _health.MaxHp);
            _view.SetStamina(_resources != null ? _resources.CurrentStamina : 0f,
                _resources != null ? _resources.MaxStamina : 1f);
            _view.SetEnergy(_resources != null ? _resources.CurrentEnergy : 0f,
                _resources != null ? _resources.MaxEnergy : 1f);

            long coins = _saveData?.CurrentRun?.RunCurrency ?? 0L;
            _view.SetCoins(FormatLocalized("hud.coins", "Coins: {0}", coins));
            RefreshWeapons();
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

            _view?.SetVisible(player != null);
            RefreshWeapons();
        }

        private void HandlePlayerTargetChanged(PlayerTargetChangedEvent change) => BindPlayer(change.Player);

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
            _view?.RefreshLocalization();
        }

        private void RefreshWeapons()
        {
            if (_weapons == null) return;
            _view?.SetWeapons(
                _weapons.GetWeapon(WeaponSlot.Primary),
                _weapons.GetWeapon(WeaponSlot.Secondary));
        }
    }
}
