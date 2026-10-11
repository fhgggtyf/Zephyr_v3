/*
 * ObjectPool.cs
 * -------------
 * Module:  Core / ObjectPool
 * Purpose: Generic object pool MonoBehaviour for a specific prefab type. Manages a queue
 *          of available objects and a set of active objects. Provides Spawn(position, rotation)
 *          and Despawn(obj) methods. On Awake, pre-initializes _initialSize objects. The pool
 *          can optionally grow when exhausted (_canGrow = true) or return null. Objects are
 *          parented to the pool's transform for organizational cleanliness.
 * Dependencies: PooledObject (component on pooled prefabs), IPooledObject (interface).
 * Scene:    GameManager (one ObjectPool per pooled prefab type in the game).
 * Ch.Ref:   Ch.8 Object Pooling System.
 */
using System.Collections.Generic;
using UnityEngine;

namespace Zephyr.Core.ObjectPool
{
    /// <summary>
    /// Generic object pool. Spawns/despawns components of type T.
    /// Pooled objects are reused to avoid frequent Instantiate/Destroy calls.
    /// </summary>
    public class ObjectPool : MonoBehaviour
    {
        [SerializeField] private PooledObject _prefab;
        [SerializeField] private int _initialSize = 10;
        [SerializeField] private bool _canGrow = true;

        private readonly Queue<PooledObject> _available = new Queue<PooledObject>();
        private readonly HashSet<PooledObject> _active = new HashSet<PooledObject>();

        private void Awake()
        {
            for (int i = 0; i < _initialSize; i++)
            {
                var obj = CreateNew();
                obj.OnDespawn();
                _available.Enqueue(obj);
            }
        }

        public PooledObject Spawn(Vector3 position, Quaternion rotation)
        {
            PooledObject obj;

            if (_available.Count > 0)
            {
                obj = _available.Dequeue();
            }
            else if (_canGrow)
            {
                obj = CreateNew();
            }
            else
            {
                Debug.LogWarning($"Pool {_prefab.name} exhausted and cannot grow.");
                return null;
            }

            obj.transform.SetPositionAndRotation(position, rotation);
            obj.OnSpawn();
            _active.Add(obj);
            return obj;
        }

        public void Despawn(PooledObject obj)
        {
            if (!_active.Remove(obj)) return;
            obj.OnDespawn();
            _available.Enqueue(obj);
        }

        private PooledObject CreateNew()
        {
            var obj = Instantiate(_prefab, transform);
            obj.Initialize(this);
            return obj;
        }

        public int ActiveCount => _active.Count;
        public int AvailableCount => _available.Count;
    }
}
