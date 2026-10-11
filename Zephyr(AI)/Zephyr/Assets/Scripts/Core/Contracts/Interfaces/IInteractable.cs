/*
 * IInteractable.cs
 * ----------------
 * Module:  Core / Interfaces
 * Purpose: Contract for any GameObject the player can interact with (doors, ladders,
 *          weapon pickups, NPCs). PlayerInteractor scans for the nearest in-range
 *          IInteractable each frame and shows the interaction prompt. CanInteract()
 *          gates whether the prompt appears (e.g., locked doors return false). OnInteract()
 *          is called when the player presses the interact button — all interaction types
 *          share a single input (no dedicated per-type keys).
 * Dependencies: None.
 * Scene:    GameManager (implemented by Door, Ladder, WeaponPickup, and other interactable prefabs).
 * Ch.Ref:   Ch.12 Pickup and Interaction System, Ch.15 Room Design.
 */
namespace Zephyr.Core.Interfaces
{
    /// <summary>
    /// Implemented by any GameObject the player can interact with (doors, ladders, weapon pickups).
    /// PlayerInteractor queries CanInteract each frame to show the interaction prompt.
    /// </summary>
    public interface IInteractable
    {
        bool CanInteract();
        void OnInteract();
    }
}
