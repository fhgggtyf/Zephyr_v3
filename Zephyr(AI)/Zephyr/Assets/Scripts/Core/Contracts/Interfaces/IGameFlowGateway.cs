using System;
using Zephyr.Contracts.Flow;

namespace Zephyr.Contracts.Interfaces
{
    public interface IGameFlowGateway
    {
        GameState CurrentState { get; }
        event Action<GameState, GameState> OnStateChanged;
        void RequestTransition(GameState targetState);
    }
}
