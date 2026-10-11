namespace Zephyr.Core.Interfaces
{
    /// <summary>
    /// Optional visual focus contract for interactables selected by the player.
    /// </summary>
    public interface IInteractionFocusReceiver
    {
        void SetInteractionFocused(bool focused);
    }
}
