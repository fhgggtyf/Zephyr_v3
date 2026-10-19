using System;
using System.Collections.Generic;
using UnityEngine;
using Zephyr.Contracts.Interfaces;
using Zephyr.Core.ObjectPool;

namespace Zephyr.Core.Flow
{
    public sealed class RoomDropSpawnGateway : IDropSpawnGateway, IDisposable
    {
        private readonly IDropSpawnGateway _poolGateway;
        private readonly HashSet<PooledObject> _spawned = new HashSet<PooledObject>();
        private bool _disposed;

        public RoomDropSpawnGateway(IDropSpawnGateway poolGateway, int seed, int roomDepth)
        {
            _poolGateway = poolGateway;
            Seed = seed;
            RoomDepth = roomDepth;
        }

        public int Seed { get; }
        public int RoomDepth { get; }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (_disposed || _poolGateway == null) return null;
            GameObject instance = _poolGateway.Spawn(prefab, position, rotation);
            Track(instance);
            return instance;
        }

        public GameObject SpawnCurrency(Vector3 position, int amount)
        {
            if (_disposed || _poolGateway == null) return null;
            GameObject instance = _poolGateway.SpawnCurrency(position, amount);
            Track(instance);
            return instance;
        }

        public GameObject SpawnWeapon(Vector3 position, string weaponId)
        {
            if (_disposed || _poolGateway == null) return null;
            GameObject instance = _poolGateway.SpawnWeapon(position, weaponId);
            Track(instance);
            return instance;
        }

        private void Track(GameObject instance)
        {
            if (instance == null) return;
            PooledObject pooled = instance.GetComponent<PooledObject>();
            if (pooled != null) _spawned.Add(pooled);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (PooledObject instance in _spawned)
                if (instance != null) instance.ReturnToPool();
            _spawned.Clear();
        }
    }
}
