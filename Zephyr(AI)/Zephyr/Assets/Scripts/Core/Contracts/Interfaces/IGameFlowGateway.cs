using System;
using Zephyr.Core.Flow;

namespace Zephyr.Core.Interfaces
{
    public interface IGameFlowGateway
    {
        GameState CurrentState { get; }
        event Action<GameState, GameState> OnStateChanged;
        void RequestTransition(GameState targetState);
    }
}
