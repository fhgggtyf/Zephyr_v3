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
        private IEventBusGateway _eventBus;

        public void Configure(IGameManagerServices services)
        {
            _saveData = services?.SaveData;
            _gameFlow = services?.GameFlow;
            _sceneFlow = services?.SceneFlow;
            _eventBus = services?.EventBus;
            if (isActiveAndEnabled) SubscribeEvents();
        }

        private void OnEnable() => SubscribeEvents();
        private void OnDisable() => _eventBus?.Unsubscribe<RoomEnteredInfo>(HandleRoomEntered);
        private void SubscribeEvents() => _eventBus?.Subscribe<RoomEnteredInfo>(HandleRoomEntered);

        private IEnumerator Start()
        {
            yield return new WaitUntil(() => _saveData != null && _gameFlow != null &&
                _gameFlow.CurrentState != GameState.Boot && GameManagerBootstrap.IsReady);

            _eventBus?.Unsubscribe<RoomEnteredInfo>(HandleRoomEntered);
            SubscribeEvents();

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

            _sceneFlow?.RequestSceneLoad(_firstRoomScene, _loadingRunScene);
        }

        public void NotifyFirstRoomReady()
        {
            if (_gameFlow?.CurrentState == GameState.LoadingRun)
                _gameFlow.RequestTransition(GameState.InRun);
        }

        private void HandleRoomEntered(RoomEnteredInfo info) => NotifyFirstRoomReady();
    }
}
