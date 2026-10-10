using UnityEngine;
using Zephyr.Core.Health;
using Zephyr.Core.Items;
using Zephyr.Core.Weapons;

namespace Zephyr.Gameplay.Enemies
{
    /// <summary>
    /// Minimal boss-specific bridge from the shared WeaponPickup prefab to a
    /// boss death reward. The reward is authored as data on the prefab so it
    /// can be replaced later without changing the boss timeline.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HealthComponent))]
    public sealed class BossRewardDrop : MonoBehaviour
    {
        [SerializeField] private GameObject _pickupPrefab;
        
        [SerializeField] private Transform _spawnPoint;
[SerializeField] private WeaponSO _weapon;
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
            if (_dropped || _pickupPrefab == null || _weapon == null) return;
            _dropped = true;

            Vector3 spawnPosition = _spawnPoint != null
                ? _spawnPoint.position
                : transform.position + (Vector3)_spawnOffset;
            GameObject pickupObject = Instantiate(_pickupPrefab, spawnPosition, Quaternion.identity);
            pickupObject.name = $"{_weapon.name}_BossDrop";

            WeaponPickup pickup = pickupObject.GetComponent<WeaponPickup>();
            if (pickup == null)
            {
                Debug.LogError("BossRewardDrop requires the configured pickup prefab to contain WeaponPickup.", pickupObject);
                Destroy(pickupObject);
                return;
            }

            pickup.Spawn(spawnPosition, _weapon);
        }
    }
}
