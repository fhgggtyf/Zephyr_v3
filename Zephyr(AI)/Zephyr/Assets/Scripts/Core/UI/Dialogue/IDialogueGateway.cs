using System;
using Zephyr.Contracts.Interfaces;

namespace Zephyr.Core.UI.Dialogue
{
    public interface IDialogueGateway : IDialogueInputGateway
    {
        bool TryStart(DialogueSO dialogue, Action completed = null);
    }
}
