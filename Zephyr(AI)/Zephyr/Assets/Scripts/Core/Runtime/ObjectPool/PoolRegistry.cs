/*
 * PoolRegistry.cs
 * ---------------
 * Module:  Core / ObjectPool
 * Purpose: Central registry singleton for all object pools in the game. Subsystems register
 *          their ObjectPool instances here by string ID during initialization. Spawning and
 *          despawning go through GetPool(id) or the convenience Spawn/Despawn methods.
 *          This allows gameplay code to reference pools by name string without holding direct
 *          references to the ObjectPool component. Lives in the Persistent scene.
 * Dependencies: ObjectPool, PooledObject (concrete pool types).
 * Scene:    Persistent (Ch.14.2 — infrastructure manager, never unloaded).
 * Ch.Ref:   Ch.8 Object Pooling System.
 */
using System.Collections.Generic;
using UnityEngine;
using Zephyr.Contracts.Interfaces;
using Zephyr.Core.Runtime.Lifecycle;

namespace Zephyr.Core.ObjectPool
{
    /// <summary>
    /// Central registry for all object pools. Lives in the Persistent scene (Ch.14.2).
    /// Subsystems register their pools here; spawning goes through GetPool.
    /// </summary>
    [DefaultExecutionOrder(-700)]
    [SingletonLifetime(SingletonLifetime.Persistent, "Persistent")]
    public sealed class PoolRegistry : MonoBehaviour, IDropSpawnGateway
    {
        private static PoolRegistry _instance;
        public static PoolRegistry Instance => _instance;

        private readonly Dictionary<string, ObjectPool> _pools = new Dictionary<string, ObjectPool>();
        private readonly Dictionary<PooledObject, ObjectPool> _prefabPools = new Dictionary<PooledObject, ObjectPool>();
        private IPickupRuntimeContext _pickupContext;
        private ITimerGateway _timerGateway;
        [SerializeField] private GameObject _currencyPickupPrefab;
        [SerializeField] private GameObject _weaponPickupPrefab;

        public int Seed { get; set; }
        public int RoomDepth { get; set; }


        private void Awake()
        {
            if (!SingletonLifecycle.TryClaim(this, ref _instance, nameof(PoolRegistry))
                || _instance != this)
            {
                Destroy(gameObject);
                return;
            }

        }

        private void OnDestroy()
        {
            SingletonLifecycle.Release(this, ref _instance);
        }

        public void RegisterPool(string id, ObjectPool pool)
        {
            _pools[id] = pool;
            if (pool != null && pool.Prefab != null)
                _prefabPools[pool.Prefab] = pool;
            pool?.Configure(_pickupContext);
        }

        public void ConfigurePickupContext(IPickupRuntimeContext pickupContext)
        {
            _pickupContext = pickupContext;
            var configuredPools = new HashSet<ObjectPool>();
            foreach (ObjectPool pool in _pools.Values)
                if (pool != null && configuredPools.Add(pool)) pool.Configure(_pickupContext);
            foreach (ObjectPool pool in _prefabPools.Values)
                if (pool != null && configuredPools.Add(pool)) pool.Configure(_pickupContext);
        }

        public void ConfigureTimer(ITimerGateway timerGateway)
        {
            _timerGateway = timerGateway;
            var configuredPools = new HashSet<ObjectPool>();
            foreach (ObjectPool pool in _pools.Values)
                if (pool != null && configuredPools.Add(pool)) pool.ConfigureTimer(_timerGateway);
            foreach (ObjectPool pool in _prefabPools.Values)
                if (pool != null && configuredPools.Add(pool)) pool.ConfigureTimer(_timerGateway);
        }

        public ObjectPool GetPool(string id)
        {
            return _pools.GetValueOrDefault(id);
        }

        public PooledObject Spawn(string poolId, Vector3 position, Quaternion rotation)
        {
            var pool = GetPool(poolId);
            if (pool == null)
            {
                Debug.LogError($"PoolRegistry: No pool registered with id '{poolId}'");
                return null;
            }
            return pool.Spawn(position, rotation);
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
            {
                Debug.LogError("PoolRegistry: Cannot spawn a null drop prefab.");
                return null;
            }

            PooledObject pooledPrefab = prefab.GetComponent<PooledObject>();
            if (pooledPrefab == null)
            {
                Debug.LogError($"PoolRegistry: Drop prefab '{prefab.name}' requires a PooledObject component.", prefab);
                return null;
            }

            if (!_prefabPools.TryGetValue(pooledPrefab, out ObjectPool pool) || pool == null)
            {
                GameObject poolObject = new GameObject($"Pool_{prefab.name}");
                poolObject.transform.SetParent(transform, false);
                pool = poolObject.AddComponent<ObjectPool>();
                pool.Initialize(pooledPrefab, _pickupContext);
                _prefabPools[pooledPrefab] = pool;
            }

            return pool.Spawn(position, rotation)?.gameObject;
        }

        
        public GameObject SpawnCurrency(Vector3 position, int amount)
        {
            GameObject instance = Spawn(_currencyPickupPrefab, position, Quaternion.identity);
            if (instance == null) return null;
            ICurrencyPickup pickup = instance.GetComponent(typeof(ICurrencyPickup)) as ICurrencyPickup;
            if (pickup == null)
            {
                Debug.LogError("PoolRegistry currency pickup prefab does not implement ICurrencyPickup.", instance);
                return null;
            }
            pickup.Spawn(position, amount);
            return instance;
        }

        public GameObject SpawnWeapon(Vector3 position, string weaponId)
        {
            GameObject instance = Spawn(_weaponPickupPrefab, position, Quaternion.identity);
            if (instance == null) return null;
            IWeaponPickup pickup = instance.GetComponent(typeof(IWeaponPickup)) as IWeaponPickup;
            if (pickup == null)
            {
                Debug.LogError("PoolRegistry weapon pickup prefab does not implement IWeaponPickup.", instance);
                return null;
            }
            pickup.Spawn(position, weaponId);
            return instance;
        }

public void Despawn(string poolId, PooledObject obj)
        {
            var pool = GetPool(poolId);
            pool?.Despawn(obj);
        }
    }
}
