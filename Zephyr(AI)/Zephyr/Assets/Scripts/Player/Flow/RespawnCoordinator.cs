using Zephyr.Core;
using Zephyr.Core.Flow;
using Zephyr.Contracts.Flow;
using Zephyr.Contracts.Interfaces;

namespace Zephyr.Gameplay.Player.Flow
{
    public sealed class RespawnCoordinator
    {
        private readonly IPlayerRespawnGateway _playerRespawn;
        private readonly IGameFlowGateway _gameFlow;

        public RespawnCoordinator(IPlayerRespawnGateway playerRespawn, IGameFlowGateway gameFlow)
        {
            _playerRespawn = playerRespawn;
            _gameFlow = gameFlow;
        }

        public void Respawn()
        {
            _playerRespawn?.RespawnCurrentPlayer();
        }

        public void GiveUp()
        {
            if (_gameFlow == null) return;

            if (_gameFlow.CurrentState == GameState.Tutorial || _gameFlow.CurrentState == GameState.LoadingRun ||
                _gameFlow.CurrentState == GameState.InRun || _gameFlow.CurrentState == GameState.Paused)
                _gameFlow.RequestTransition(GameState.MetaHub);
        }
    }
}
