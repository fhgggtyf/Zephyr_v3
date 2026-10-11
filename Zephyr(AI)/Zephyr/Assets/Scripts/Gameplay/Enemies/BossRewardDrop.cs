using UnityEngine;
using Zephyr.Core.Contracts;
using Zephyr.Core.Health;

namespace Zephyr.Gameplay.Enemies
{
    /// <summary>
    /// Spawns the weapon pickup configured on the reward prefab when the boss dies.
    /// The enemy domain does not resolve weapon assets or weapon runtime types.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HealthComponent))]
    public sealed class BossRewardDrop : MonoBehaviour
    {
        [SerializeField] private GameObject _pickupPrefab;
        
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private Vector2 _spawnOffset = new Vector2(0f, 0.8f);

        private HealthComponent _health;
        private bool _dropped;

        private void Awake() => _health ??= GetComponent<HealthComponent>();

        private void OnEnable()
        {
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
            _dropped = true;

            Vector3 spawnPosition = _spawnPoint != null
                ? _spawnPoint.position
                : transform.position + (Vector3)_spawnOffset;
            GameObject pickupObject = Instantiate(_pickupPrefab, spawnPosition, Quaternion.identity);
            pickupObject.name = "BossWeaponDrop";

            IWeaponPickup pickup = pickupObject.GetComponent(typeof(IWeaponPickup)) as IWeaponPickup;
            if (pickup == null)
            {
                Debug.LogError("BossRewardDrop requires the configured pickup prefab to contain a weapon pickup contract.", pickupObject);
                Destroy(pickupObject);
                return;
            }

            pickup.Spawn(spawnPosition);
        }
    }
}
