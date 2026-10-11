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

namespace Zephyr.Core.ObjectPool
{
    /// <summary>
    /// Component attached to pooled objects. Handles OnSpawn/OnDespawn lifecycle.
    /// Implements IPooledObject.
    /// </summary>
    public class PooledObject : MonoBehaviour, Interfaces.IPooledObject
    {
        private ObjectPool _pool;

        public void Initialize(ObjectPool pool)
        {
            _pool = pool;
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
            _pool?.Despawn(this);
        }
    }
}
