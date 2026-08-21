using UnityEngine;
using Zephyr.Core.Interfaces;
using Zephyr.Core.ObjectPool;
using Zephyr.Core.Save;
using Zephyr.Core.Weapons;

namespace Zephyr.Core.Items
{
    [RequireComponent(typeof(PooledObject))]
    public sealed class WeaponPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private WeaponSO _weapon;
        private PooledObject _pooledObject;
        public WeaponSO Weapon => _weapon;
        private void Awake() => _pooledObject = GetComponent<PooledObject>();
        public void ResetState() { }
        public void Spawn(Vector2 position, WeaponSO weapon)
        { transform.position = position; _weapon = weapon; ResetState(); }
        public bool CanInteract() => _weapon != null && FindPlayerController() != null;
        public void OnInteract()
        {
            WeaponController controller = FindPlayerController();
            if (controller == null || _weapon == null) return;
            controller.Equip(controller.ActiveSlot, _weapon);
            if (SaveSystem.Instance?.CurrentRun != null && int.TryParse(_weapon.WeaponId, out int id))
                SaveSystem.Instance.CurrentRun.AcquiredWeaponIds.Add(id);
            _pooledObject.ReturnToPool();
        }
        private static WeaponController FindPlayerController()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            return player != null ? player.GetComponentInChildren<WeaponController>(true) : null;
        }
    }
}
