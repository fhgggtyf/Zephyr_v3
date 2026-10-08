using System;
using System.Collections;

using UnityEngine;
using Zephyr.Core.Save;
using Zephyr.Core.Meta;
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

private IEnumerator Start()
        {
            // Direct-open scenes start before ColdStartup has loaded Persistent
            // and GameManager. Do not abandon the run-entry path in that window.
            yield return new WaitUntil(() => SaveSystem.Instance != null &&
                GameFlow.Instance != null && GameFlow.Instance.CurrentState != GameState.Boot &&
                GameManagerBootstrap.IsReady);

            EventBus.Instance?.Unsubscribe<RoomEnteredInfo>(HandleRoomEntered);
            EventBus.Instance?.Subscribe<RoomEnteredInfo>(HandleRoomEntered);

            var save = SaveSystem.Instance;
            // A confirmed MetaHub allocation always starts a fresh run. Resume loading
            // is retained only for the interrupt-save path that has no pending entry choice.
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
