using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zephyr.Core.Events;
using Zephyr.Core.Meta;
using Zephyr.Core.UI;

namespace Zephyr.Core.Flow
{
    [DefaultExecutionOrder(-500)]
    public sealed class GameFlow : MonoBehaviour
    {
        public static GameFlow Instance { get; private set; }

        [Header("Event Channels")]
        [SerializeField] private GameEventSO<GameState> _onGameStateChanged;
        [SerializeField] private GameEventSO<SceneLoadRequest> _onRequestSceneLoad;
        [SerializeField] private GameEventSO<SceneSO> _onRequestSceneUnload;

        [Header("Core Scenes")]
        [SerializeField] private SceneSO _initializerScene;
        [SerializeField] private SceneSO _gameManagerScene;
        [SerializeField] private SceneSO _mainMenuScene;

        [Header("Gameplay Flow Scenes")]
        [SerializeField] private SceneSO _tutorialScene;
        [SerializeField] private SceneSO _metaHubScene;
        [SerializeField] private SceneSO _loadingRunScene;
        [SerializeField] private SceneSO _roomGenericScene;
        [SerializeField] private SceneSO _roomBossScene;

        public GameState CurrentState { get; private set; } = GameState.Boot;
        public event Action<GameState, GameState> OnStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            GameplayTimePause.ResetAll();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void RequestTransition(GameState targetState)
        {
            if (!IsValidTransition(CurrentState, targetState))
            {
                Debug.LogWarning($"GameFlow: Invalid transition from {CurrentState} to {targetState}", this);
                return;
            }

            if ((CurrentState == GameState.InRun || CurrentState == GameState.Paused)
                && targetState == GameState.MetaHub)
                RunEndSettlement.SettleCurrentRun();

            GameState previous = CurrentState;
            CurrentState = targetState;
            GameplayTimePause.SetGameFlowPaused(targetState == GameState.Paused);
            OnStateChanged?.Invoke(previous, targetState);
            _onGameStateChanged?.Raise(targetState);
            ApplySceneTransition(previous, targetState);
        }

        /// <summary>
        /// Initializes the logical flow state when a gameplay scene is opened
        /// directly from the editor. Unlike RequestTransition, this does not
        /// request another scene load because the target scene is already open.
        /// The normal Initializer -> Persistent -> MainMenu flow must continue
        /// to use RequestTransition.
        /// </summary>
        public bool InitializeForDirectOpen(GameState targetState)
        {
            if (CurrentState != GameState.Boot)
            {
                Debug.LogWarning($"GameFlow: direct-open initialization ignored because the current state is {CurrentState}.", this);
                return false;
            }

            if (targetState == GameState.Boot || targetState == GameState.Paused ||
                !IsDirectOpenState(targetState))
            {
                Debug.LogError($"GameFlow: '{targetState}' is not a valid direct-open state.", this);
                return false;
            }

            GameState previous = CurrentState;
            CurrentState = targetState;
            GameplayTimePause.SetGameFlowPaused(false);
            OnStateChanged?.Invoke(previous, targetState);
            _onGameStateChanged?.Raise(targetState);
            return true;
        }

        public void RequestSceneLoad(SceneSO targetScene, SceneSO sceneToReplace = null,
            LoadSceneMode mode = LoadSceneMode.Additive)
        {
            if (targetScene == null)
            {
                Debug.LogError("GameFlow: cannot request a load without a target SceneSO.", this);
                return;
            }

            _onRequestSceneLoad?.Raise(new SceneLoadRequest(targetScene, sceneToReplace, mode));
        }

        public void RequestSceneUnload(SceneSO scene)
        {
            if (scene != null) _onRequestSceneUnload?.Raise(scene);
        }

        /// <summary>
        /// Requests a transition between authored progression scenes while keeping
        /// the SceneLoader as the only scene-loading authority. This is intentionally
        /// separate from RequestTransition because multiple tutorial/run scenes share
        /// one logical GameState.
        /// </summary>
        public void RequestProgressionSceneTransition(SceneSO targetScene, SceneSO sceneToReplace,
            GameState logicalState)
        {
            if (targetScene == null || !targetScene.IsValid)
            {
                Debug.LogError("GameFlow: progression target SceneSO is missing or invalid.", this);
                return;
            }

            if (logicalState != CurrentState)
            {
                if (!IsValidTransition(CurrentState, logicalState))
                {
                    Debug.LogWarning($"GameFlow: Invalid progression transition from {CurrentState} to {logicalState}", this);
                    return;
                }

                if ((CurrentState == GameState.InRun || CurrentState == GameState.Paused) &&
                    logicalState == GameState.MetaHub)
                {
                    RunEndSettlement.SettleCurrentRun();
                }

                GameState previous = CurrentState;
                CurrentState = logicalState;
                GameplayTimePause.SetGameFlowPaused(logicalState == GameState.Paused);
                OnStateChanged?.Invoke(previous, logicalState);
                _onGameStateChanged?.Raise(logicalState);
            }

            RequestSceneLoad(targetScene, sceneToReplace);
        }

        private bool IsValidTransition(GameState from, GameState to)
        {
            if (to == GameState.MainMenu)
                return from != GameState.MainMenu;

            switch (from)
            {
                case GameState.Boot:
                    return to == GameState.MainMenu;
                case GameState.MainMenu:
                    return (to == GameState.Tutorial && !(Save.SaveSystem.Instance?.Meta?.HasCompletedTutorial ?? false)) ||
                           to == GameState.MetaHub || to == GameState.LoadingRun;
                case GameState.Tutorial:
                    return to == GameState.MetaHub;
                case GameState.MetaHub:
                    // Authored placeholder progression may enter the first level directly;
                    // the normal LoadingRun path remains valid for generated runs.
                    return to == GameState.LoadingRun || to == GameState.InRun;
                case GameState.LoadingRun:
                    return to == GameState.InRun || to == GameState.MetaHub;
                case GameState.InRun:
                    return to == GameState.Paused || to == GameState.MetaHub;
                case GameState.Paused:
                    return to == GameState.InRun || to == GameState.MetaHub;
                default:
                    return false;
            }
        }

        private static bool IsDirectOpenState(GameState state)
        {
            return state == GameState.MainMenu || state == GameState.Tutorial ||
                   state == GameState.MetaHub || state == GameState.LoadingRun ||
                   state == GameState.InRun;
        }

        private void ApplySceneTransition(GameState previous, GameState target)
        {
            if (previous == GameState.Boot && target == GameState.MainMenu)
            {
                RequestSceneLoad(_mainMenuScene, _initializerScene);
                return;
            }

            if (target == GameState.MainMenu)
            {
                SceneSO previousScene = SceneForState(previous);
                if (previousScene != null) RequestSceneUnload(previousScene);
                if (previous == GameState.InRun || previous == GameState.Paused) UnloadRoomScenes();
                RequestSceneLoad(_mainMenuScene, _gameManagerScene);
                return;
            }

            if (target == GameState.Paused || (previous == GameState.Paused && target == GameState.InRun))
                return;

            SceneSO targetScene = SceneForState(target);
            if (targetScene == null) return;

            if (previous == GameState.MainMenu)
            {
                RequestSceneLoad(_gameManagerScene);
                RequestSceneLoad(targetScene, _mainMenuScene);
                return;
            }

            if (previous == GameState.InRun || previous == GameState.Paused)
                UnloadRoomScenes();

            RequestSceneLoad(targetScene, SceneForState(previous));
        }

        private void UnloadRoomScenes()
        {
            RequestSceneUnload(_roomGenericScene);
            RequestSceneUnload(_roomBossScene);
        }

        private SceneSO SceneForState(GameState state)
        {
            switch (state)
            {
                case GameState.MainMenu: return _mainMenuScene;
                case GameState.Tutorial: return _tutorialScene;
                case GameState.MetaHub: return _metaHubScene;
                case GameState.LoadingRun: return _loadingRunScene;
                default: return null;
            }
        }
    }
}
