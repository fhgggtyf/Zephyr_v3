using System;
using UnityEngine;
using Zephyr.Core.Save;
using Zephyr.Core.Events;
using Zephyr.Core.Levels;

namespace Zephyr.Core.Flow.Scenes
{
    public sealed class LoadingRunController : MonoBehaviour
    {
        [SerializeField] private bool _resumeInterruptSave = true;
        [SerializeField] private SceneSO _firstRoomScene;
        [SerializeField] private SceneSO _loadingRunScene;

        private void OnEnable() => EventBus.Instance?.Subscribe<RoomEnteredInfo>(HandleRoomEntered);
        private void OnDisable() => EventBus.Instance?.Unsubscribe<RoomEnteredInfo>(HandleRoomEntered);

        private void Start()
        {
            var save = SaveSystem.Instance;
            if (save == null) return;
            var run = _resumeInterruptSave ? save.LoadRun() : null;
            if (run == null) save.StartNewRun(unchecked((int)DateTime.UtcNow.Ticks));
            GameFlow.Instance?.RequestSceneLoad(_firstRoomScene, _loadingRunScene);
        }

        public void NotifyFirstRoomReady()
        {
            if (GameFlow.Instance?.CurrentState == GameState.LoadingRun)
                GameFlow.Instance.RequestTransition(GameState.InRun);
        }

        private void HandleRoomEntered(RoomEnteredInfo info) => NotifyFirstRoomReady();
    }
}
