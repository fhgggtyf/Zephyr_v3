/*
 * PooledObject.cs
 * ---------------
 * Module:  Core / ObjectPool
 * Purpose: Component attached to every pooled prefab. Implements IPooledObject and handles
 *          the spawn/despawn lifecycle (SetActive(true)/SetActive(false)). The Initialize()
 *          method stores a back-reference to the owning ObjectPool so that ReturnToPool()
 *          can be called by the pooled object itself to auto-despawn. Used on projectiles,
 *          currency pickups, VFX, and audio sources that are frequently created/destroyed.
 * Dependencies: ObjectPool (back-reference), IPooledObject (interface implementation).
 * Scene:    GameManager (attached to pooled prefabs instantiated at runtime).
 * Ch.Ref:   Ch.8 Object Pooling System.
 */
using UnityEngine;
using Zephyr.Contracts.Interfaces;

namespace Zephyr.Core.ObjectPool
{
    /// <summary>
    /// Component attached to pooled objects. Handles OnSpawn/OnDespawn lifecycle.
    /// Implements IPooledObject.
    /// </summary>
    public class PooledObject : MonoBehaviour, IPooledObject
    {
        private ObjectPool _pool;
        private IPickupRuntimeContext _pickupContext;
        private ITimerGateway _timerGateway;

        public void Initialize(ObjectPool pool)
        {
            _pool = pool;
        }

        public void Configure(IPickupRuntimeContext pickupContext)
        {
            _pickupContext = pickupContext;

            MonoBehaviour[] consumers = GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < consumers.Length; i++)
            {
                if (consumers[i] is IPickupRuntimeContextConsumer pickupConsumer)
                    pickupConsumer.Configure(_pickupContext);
                if (consumers[i] is IPlayerTargetContextConsumer targetConsumer)
                    targetConsumer.Configure(_pickupContext);
            }
        }

        public void ConfigureTimer(ITimerGateway timerGateway)
        {
            _timerGateway = timerGateway;
            MonoBehaviour[] consumers = GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < consumers.Length; i++)
                if (consumers[i] is ITimerGatewayConsumer timerConsumer)
                    timerConsumer.Configure(_timerGateway);
        }

        public void OnSpawn()
        {
            gameObject.SetActive(true);
        }

        public void OnDespawn()
        {
            gameObject.SetActive(false);
        }

        public void ReturnToPool()
        {
            if (_pool != null) _pool.Despawn(this);
            else OnDespawn();
        }
    }
}
