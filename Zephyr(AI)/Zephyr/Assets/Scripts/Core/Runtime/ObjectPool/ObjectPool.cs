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
using Zephyr.Contracts.Interfaces;

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
        private IPickupRuntimeContext _pickupContext;
        private ITimerGateway _timerGateway;
        private bool _initialized;
        private Transform _creationRoot;

        private void Awake()
        {
            if (_prefab != null) InitializePool();
        }

        public void Initialize(PooledObject prefab, IPickupRuntimeContext pickupContext,
            int initialSize = 4, bool canGrow = true)
        {
            if (_initialized) return;
            _prefab = prefab;
            _pickupContext = pickupContext;
            _initialSize = Mathf.Max(0, initialSize);
            _canGrow = canGrow;
            InitializePool();
        }

        public void Configure(IPickupRuntimeContext pickupContext)
        {
            _pickupContext = pickupContext;

            foreach (PooledObject obj in _available)
                obj.Configure(_pickupContext);
            foreach (PooledObject obj in _active)
                obj.Configure(_pickupContext);
        }

        public void ConfigureTimer(ITimerGateway timerGateway)
        {
            _timerGateway = timerGateway;
            foreach (PooledObject obj in _available) obj.ConfigureTimer(_timerGateway);
            foreach (PooledObject obj in _active) obj.ConfigureTimer(_timerGateway);
        }

        private void InitializePool()
        {
            if (_initialized || _prefab == null) return;
            _initialized = true;
            for (int i = 0; i < _initialSize; i++)
            {
                PooledObject obj = CreateNew();
                obj.OnDespawn();
                _available.Enqueue(obj);
            }
        }

        public PooledObject Spawn(Vector3 position, Quaternion rotation)
        {
            if (!_initialized) InitializePool();
            if (!_initialized) return null;

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
            _active.Add(obj);
            obj.OnSpawn();
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
            if (_creationRoot == null)
            {
                GameObject creationObject = new GameObject("InactiveCreationRoot");
                creationObject.SetActive(false);
                creationObject.transform.SetParent(transform, false);
                _creationRoot = creationObject.transform;
            }
            PooledObject obj = Instantiate(_prefab, _creationRoot);
            obj.gameObject.SetActive(false);
            obj.Initialize(this);
            obj.Configure(_pickupContext);
            obj.ConfigureTimer(_timerGateway);
            obj.transform.SetParent(transform, false);
            return obj;
        }

        public int ActiveCount => _active.Count;
        public int AvailableCount => _available.Count;
        public PooledObject Prefab => _prefab;
    }
}
