using UnityEngine;
using Zephyr.Contracts.Interfaces;
using Zephyr.Core.Health;

namespace Zephyr.Gameplay.Enemies
{
    /// <summary>
    /// Spawns the weapon pickup configured on the reward prefab when the boss dies.
    /// The enemy domain does not resolve weapon assets or weapon runtime types.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HealthComponent))]
    public sealed class BossRewardDrop : MonoBehaviour, IDropSpawnGatewayConsumer
    {
        [SerializeField] private GameObject _pickupPrefab;
        
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private Vector2 _spawnOffset = new Vector2(0f, 0.8f);

        private HealthComponent _health;
        private IDropSpawnGateway _dropSpawnGateway;
        private bool _dropped;

        public void Configure(IDropSpawnGateway gateway) => _dropSpawnGateway = gateway;

        private void Awake() => _health ??= GetComponent<HealthComponent>();

        private void OnEnable()
        {
            _dropped = false;
            _health ??= GetComponent<HealthComponent>();
            if (_health != null) _health.Died += HandleDeath;
        }

        private void OnDisable()
        {
            if (_health != null) _health.Died -= HandleDeath;
        }

        private void HandleDeath()
        {
            if (_dropped || _pickupPrefab == null) return;
            if (_dropSpawnGateway == null)
            {
                Debug.LogError("BossRewardDrop requires an injected drop spawn gateway.", this);
                return;
            }
            if (!(_pickupPrefab.GetComponent(typeof(IWeaponPickup)) is IWeaponPickup))
            {
                Debug.LogError("BossRewardDrop requires a prefab with a weapon pickup contract.", this);
                return;
            }

            Vector3 spawnPosition = _spawnPoint != null
                ? _spawnPoint.position
                : transform.position + (Vector3)_spawnOffset;
            spawnPosition += Vector3.up;

            GameObject pickupObject = _dropSpawnGateway.Spawn(_pickupPrefab, spawnPosition, Quaternion.identity);
            if (pickupObject == null) return;
            _dropped = true;

            IWeaponPickup pickup = pickupObject.GetComponent(typeof(IWeaponPickup)) as IWeaponPickup;
            if (pickup == null)
            {
                Debug.LogError("BossRewardDrop requires the configured pickup prefab to contain a weapon pickup contract.", pickupObject);
                pickupObject.SetActive(false);
                return;
            }

            pickup.Spawn(spawnPosition);
        }
    }
}
