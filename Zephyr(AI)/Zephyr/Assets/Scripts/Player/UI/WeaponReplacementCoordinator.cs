using UnityEngine;
using Zephyr.Contracts.Interfaces;
using Zephyr.Core.Flow;
using Zephyr.Core.Items;
using Zephyr.Core.ObjectPool;
using Zephyr.Core.Weapons;

namespace Zephyr.Gameplay.Player.UI
{
    public sealed class WeaponReplacementCoordinator : MonoBehaviour, IGameManagerDependencyConsumer,
        IWeaponReplacementGateway
    {
        [SerializeField] private GameObject _weaponReplacementPanelPrefab;
        private GameplayHudController _hud;
        private IDropSpawnGateway _dropSpawn;
        private WeaponReplacementPanel _weaponReplacementPanel;

        private void Awake()
        {
            _hud = GetComponent<GameplayHudController>();
        }

        public void Configure(IGameManagerServices services)
        {
            _dropSpawn = services?.DropSpawn;
        }

        public bool RequestWeaponPickup(GameObject pickupObject, GameObject player)
        {
            WeaponPickup pickup = pickupObject != null ? pickupObject.GetComponent<WeaponPickup>() : null;
            WeaponController controller = player != null
                ? player.GetComponentInChildren<WeaponController>(true)
                : null;
            if (pickup == null || controller == null || pickup.Weapon == null) return true;

            WeaponSlot? emptySlot = controller.GetWeapon(WeaponSlot.Primary) == null
                ? WeaponSlot.Primary
                : controller.GetWeapon(WeaponSlot.Secondary) == null
                    ? WeaponSlot.Secondary
                    : (WeaponSlot?)null;
            if (emptySlot.HasValue)
            {
                pickup.TryCompletePickup(controller, emptySlot.Value, out _);
                return true;
            }

            if (_weaponReplacementPanel != null) return true;
            if (_weaponReplacementPanelPrefab == null)
            {
                Debug.LogError("WeaponReplacementCoordinator is missing the weapon replacement panel prefab.", this);
                return true;
            }

            Transform panelParent = _hud != null ? _hud.ViewRoot : transform;
            GameObject instance = Instantiate(_weaponReplacementPanelPrefab, panelParent);
            _weaponReplacementPanel = instance.GetComponent<WeaponReplacementPanel>();
            if (_weaponReplacementPanel == null)
            {
                Destroy(instance);
                Debug.LogError("Weapon replacement panel prefab is missing WeaponReplacementPanel.", this);
                return true;
            }

            _weaponReplacementPanel.Show(
                controller.GetWeapon(WeaponSlot.Primary),
                controller.GetWeapon(WeaponSlot.Secondary),
                pickup.Weapon,
                slot => ConfirmWeaponReplacement(pickup, controller, slot),
                () => _weaponReplacementPanel = null);
            return true;
        }

        private bool ConfirmWeaponReplacement(WeaponPickup pickup, WeaponController controller,
            WeaponSlot slot)
        {
            if (pickup == null || controller == null || pickup.Weapon == null) return false;
            WeaponSO replacedWeapon = controller.GetWeapon(slot);
            if (replacedWeapon == null || _dropSpawn == null) return false;

            Vector3 dropPosition = controller.transform.root.position + Vector3.up;
            GameObject dropped = _dropSpawn.SpawnWeapon(dropPosition, replacedWeapon.WeaponId);
            if (dropped == null) return false;
            if (!pickup.TryCompletePickup(controller, slot, out _))
            {
                dropped.GetComponent<PooledObject>()?.ReturnToPool();
                return false;
            }

            _weaponReplacementPanel = null;
            return true;
        }
    }
}
