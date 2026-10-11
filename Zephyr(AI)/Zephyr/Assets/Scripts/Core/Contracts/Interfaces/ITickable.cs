/*
 * ITickable.cs
 * ------------
 * Module:  Core / Interfaces
 * Purpose: Marker interface for MonoBehaviours that opt into the centralized Timer/Tick
 *          service instead of using private Update/FixedUpdate loops. The Tick(float) method
 *          receives deltaTime and is called by a central dispatcher, enabling batch
 *          scheduling and reducing per-frame MonoBehaviour overhead. Entities implementing
 *          this are registered with the TickService in the Timer module.
 * Dependencies: None.
 * Scene:    Persistent (TickService) + GameManager (implementors like ProjectileSystem).
 * Ch.Ref:   Ch.24.1 Centralized Update Loop.
 */
namespace Zephyr.Core.Interfaces
{
    /// <summary>
    /// Marker interface for MonoBehaviours that tick through the centralized update loop
    /// (Ch.24.1) instead of using private Update/FixedUpdate.
    /// </summary>
    public interface ITickable
    {
        void Tick(float deltaTime);
    }
}
