using System;

namespace Zephyr.Contracts.Interfaces
{
    public interface IDialogueInputGateway
    {
        bool IsOpen { get; }
        event Action InputSuspended;
        event Action InputRestored;
    }
}
