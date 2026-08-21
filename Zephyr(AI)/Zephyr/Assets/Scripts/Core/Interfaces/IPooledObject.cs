/*
 * IPooledObject.cs
 * ----------------
 * Module:  Core / Interfaces
 * Purpose: Contract for components that participate in the object pool system. OnSpawn()
 *          is called when the object is taken from the pool (activate and reset state),
 *          OnDespawn() is called when returned to the pool (deactivate and clean up), and
 *          ReturnToPool() is a convenience method for the object to return itself. Implemented
 *          by PooledObject component on pooled prefabs (projectiles, pickups, VFX, audio).
 * Dependencies: None (uses Zephyr.Core.ObjectPool.PooledObject as the default implementation).
 * Scene:    GameManager (pooled entities spawned at runtime).
 * Ch.Ref:   Ch.8 Object Pooling System.
 */
namespace Zephyr.Core.Interfaces
{
    /// <summary>
    /// Entities that can be pooled (projectiles, pickups, VFX, audio sources).
    /// </summary>
    public interface IPooledObject
    {
        void OnSpawn();
        void OnDespawn();
        void ReturnToPool();
    }
}
