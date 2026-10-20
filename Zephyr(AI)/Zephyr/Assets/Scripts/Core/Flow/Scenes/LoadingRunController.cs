using System;
using System.Collections;

using UnityEngine;
using Zephyr.Contracts.Flow;
using Zephyr.Core.Save;
using Zephyr.Core.Meta;
using Zephyr.Core.Events;
using Zephyr.Contracts.Interfaces;
using Zephyr.Core.Levels;

namespace Zephyr.Core.Flow.Scenes
{
public sealed class LoadingRunController : MonoBehaviour, IGameManagerDependencyConsumer
    {
        [SerializeField] private bool _resumeInterruptSave = true;
        [SerializeField] private SceneSO _firstRoomScene;
        [SerializeField] private SceneSO _loadingRunScene;

        private ISaveDataGateway _saveData;
        private IGameFlowGateway _gameFlow;
        private IGameFlowSceneGateway _sceneFlow;
        private ISceneLoadingGateway _sceneLoading;
        private IPlayerSpawnGateway _playerSpawn;
        private IEventBusGateway _eventBus;
        private bool _roomReady;
        private bool _playerReady;
        private bool _hudReady;
        private bool _readySignaled;

        public void Configure(IGameManagerServices services)
        {
            _saveData = services?.SaveData;
            _gameFlow = services?.GameFlow;
            _sceneFlow = services?.SceneFlow;
            _sceneLoading = services?.SceneLoading;
            _playerSpawn = services?.PlayerSpawn;
            _eventBus = services?.EventBus;
            if (_playerSpawn != null)
                _playerSpawn.PlayerSpawned += HandlePlayerSpawned;
            if (isActiveAndEnabled) SubscribeEvents();
        }

        private void OnEnable()
        {
            SubscribeEvents();
            if (_playerSpawn != null)
                _playerSpawn.PlayerSpawned += HandlePlayerSpawned;
        }
        private void OnDisable()
        {
            UnsubscribeEvents();
            if (_playerSpawn != null)
                _playerSpawn.PlayerSpawned -= HandlePlayerSpawned;
        }

        private void SubscribeEvents()
        {
            _eventBus?.Subscribe<RoomEnteredInfo>(HandleRoomEntered);
            _eventBus?.Subscribe<GameplayHudReady>(HandleHudReady);
            if (_eventBus != null && _eventBus.TryGetLatest(out RoomEnteredInfo room))
                HandleRoomEntered(room);
            if (_eventBus != null && _eventBus.TryGetLatest(out GameplayHudReady _))
                _hudReady = true;
        }

        private void UnsubscribeEvents()
        {
            _eventBus?.Unsubscribe<RoomEnteredInfo>(HandleRoomEntered);
            _eventBus?.Unsubscribe<GameplayHudReady>(HandleHudReady);
        }

        private IEnumerator Start()
        {
            yield return new WaitUntil(() => _saveData != null && _gameFlow != null &&
                _gameFlow.CurrentState != GameState.Boot && GameManagerBootstrap.IsReady);

            ISaveDataGateway save = _saveData;
            var run = _resumeInterruptSave && !RunEntryPotentialService.HasPending ? save.LoadRun() : null;
            if (run == null)
            {
                save.StartNewRun(unchecked((int)DateTime.UtcNow.Ticks));
                if (RunEntryPotentialService.TryConsume(out var potentials))
                {
                    foreach (var pair in potentials)
                        save.CurrentRun.Potentials[(int)pair.Key] = pair.Value;
                }
                save.SaveRun();
            }

            _sceneLoading?.RequireReadySignal();
            _sceneFlow?.RequestSceneLoad(_firstRoomScene, _loadingRunScene);
        }

        private void HandleRoomEntered(RoomEnteredInfo info)
        {
            if (_firstRoomScene == null || info.Scene == _firstRoomScene)
                _roomReady = true;
            TryComplete();
        }

        private void HandleHudReady(GameplayHudReady _)
        {
            _hudReady = true;
            TryComplete();
        }

        private void HandlePlayerSpawned(GameObject player)
        {
            if (player != null) StartCoroutine(WaitForPlayerAndCameraReady(player));
        }

        private IEnumerator WaitForPlayerAndCameraReady(GameObject player)
        {
            const int maxFrames = 120;
            for (int frame = 0; frame < maxFrames; frame++)
            {
                yield return null;
                if (_playerSpawn != null && _playerSpawn.CurrentPlayer == player &&
                    _playerSpawn.IsCurrentPlayerCameraStable)
                {
                    _playerReady = true;
                    TryComplete();
                    yield break;
                }
            }

            Debug.LogError("LoadingRunController: player camera did not become stable.", this);
        }

        private void TryComplete()
        {
            if (_readySignaled || _gameFlow?.CurrentState != GameState.LoadingRun) return;
            if (!_roomReady || !_playerReady || !_hudReady) return;

            _readySignaled = true;
            _sceneLoading?.SignalReady();
            _gameFlow.RequestTransition(GameState.InRun);
        }
    }
}
