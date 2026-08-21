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

namespace Zephyr.Core.ObjectPool
{
    /// <summary>
    /// Central registry for all object pools. Lives in the Persistent scene (Ch.14.2).
    /// Subsystems register their pools here; spawning goes through GetPool.
    /// </summary>
    [DefaultExecutionOrder(-700)]
    public sealed class PoolRegistry : MonoBehaviour
    {
        public static PoolRegistry Instance { get; private set; }

        private readonly Dictionary<string, ObjectPool> _pools = new Dictionary<string, ObjectPool>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void RegisterPool(string id, ObjectPool pool)
        {
            _pools[id] = pool;
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

        public void Despawn(string poolId, PooledObject obj)
        {
            var pool = GetPool(poolId);
            pool?.Despawn(obj);
        }
    }
}
