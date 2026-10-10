using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Zephyr.Core.Flow
{
    /// <summary>
    /// Optional scene-local bootstrap for opening a scene directly from the editor.
    /// It fills missing additive dependencies, prepares direct-open save state, and
    /// destroys itself after the normal flow services are ready.
    /// </summary>
    [DefaultExecutionOrder(-950)]
    public sealed class ColdStartupBootstrap : MonoBehaviour
    {
        private static ColdStartupBootstrap s_owner;

        [SerializeField] private SceneSO _persistentScene;
        [SerializeField] private SceneSO _gameManagerScene;
        [SerializeField] private bool _loadGameManager = true;

        private Scene _directOpenScene;
        private GameState? _directOpenState;
        private bool _shouldLoadGameManager;
        private bool _waitingForPersistentServices;
        private bool _waitingForGameManager;
        private float _gameManagerTimeoutAt;

        private IEnumerator Start()
        {
            // ColdStartup is an editor-only dependency bridge. Once the normal
            // Initializer -> Persistent -> MainMenu flow is already alive, the
            // scene-local instance must be inert and remove itself.
            if (s_owner != null || SceneLoader.Instance != null || GameFlow.Instance != null)
            {
                Destroy(gameObject);
                yield break;
            }

            s_owner = this;
            _directOpenScene = gameObject.scene;
            _directOpenState = ResolveDirectOpenState(_directOpenScene);
            _shouldLoadGameManager = _loadGameManager && RequiresGameManager(_directOpenState);

            // This is the same one-time Persistent bootstrap exception used by
            // InitializerBootstrap. All later scene operations go through the
            // Persistent SceneLoader.
            if (!EnsureSceneLoaded(_persistentScene, "Persistent"))
            {
                s_owner = null;
                Destroy(gameObject);
                yield break;
            }

            _waitingForPersistentServices = true;
            yield return null;
        }

        private void Update()
        {
            if (_waitingForPersistentServices)
            {
                if (!PersistentBootstrap.IsReady || GameFlow.Instance == null || Save.SaveSystem.Instance == null)
                    return;

                _waitingForPersistentServices = false;
                PrepareDirectOpenState(_directOpenState);

                if (_shouldLoadGameManager)
                {
                    if (SceneLoader.Instance == null)
                    {
                        Debug.LogError("ColdStartupBootstrap: SceneLoader is missing after Persistent loaded.", this);
                        CompleteDirectOpen();
                    }
                    else
                    {
                        if (!SceneLoader.Instance.RequestSceneLoad(_gameManagerScene))
                        {
                            Destroy(gameObject);
                            return;
                        }
                        _waitingForGameManager = true;
                        _gameManagerTimeoutAt = Time.realtimeSinceStartup + 30f;
                    }
                }
                else
                {
                    CompleteDirectOpen();
                }

                return;
            }

            if (!_waitingForGameManager) return;

            Scene loaded = SceneManager.GetSceneByName(_gameManagerScene.SceneName);
            if (loaded.IsValid() && loaded.isLoaded && GameManagerBootstrap.IsReady)
            {
                _waitingForGameManager = false;
                CompleteDirectOpen();
                return;
            }

            if (Time.realtimeSinceStartup >= _gameManagerTimeoutAt)
            {
                _waitingForGameManager = false;
                Debug.LogError("ColdStartupBootstrap: timed out waiting for GameManager scene.", this);
                Destroy(gameObject);
            }
        }

        private void CompleteDirectOpen()
        {
            _waitingForPersistentServices = false;
            _waitingForGameManager = false;

            if (_directOpenScene.IsValid() && _directOpenScene.isLoaded)
                SceneManager.SetActiveScene(_directOpenScene);

            if (_directOpenState.HasValue && GameFlow.Instance != null)
                GameFlow.Instance.InitializeForDirectOpen(_directOpenState.Value);
            else if (!_directOpenState.HasValue)
                Debug.LogWarning($"ColdStartupBootstrap: no direct-open GameState mapping for scene '{_directOpenScene.name}'.", this);

            s_owner = null;
            Destroy(gameObject);
        }

        private static GameState? ResolveDirectOpenState(Scene scene)
        {
            if (!scene.IsValid() || string.IsNullOrWhiteSpace(scene.name)) return null;

            if (string.Equals(scene.name, "MainMenu", System.StringComparison.OrdinalIgnoreCase))
                return GameState.MainMenu;
            if (string.Equals(scene.name, "MetaHub", System.StringComparison.OrdinalIgnoreCase))
                return GameState.MetaHub;
            if (string.Equals(scene.name, "LoadingRun", System.StringComparison.OrdinalIgnoreCase))
                return GameState.LoadingRun;
            if (scene.name.StartsWith("Tutorial", System.StringComparison.OrdinalIgnoreCase))
                return GameState.Tutorial;
            if (scene.name.StartsWith("LV", System.StringComparison.OrdinalIgnoreCase) ||
                scene.name.StartsWith("Room_", System.StringComparison.OrdinalIgnoreCase))
                return GameState.InRun;

            return null;
        }

        private static bool RequiresGameManager(GameState? state)
        {
            return state.HasValue && state.Value != GameState.MainMenu;
        }

        private static bool EnsureSceneLoaded(SceneSO scene, string role)
        {
            if (scene == null || !scene.IsValid)
            {
                Debug.LogError($"ColdStartupBootstrap: {role} SceneSO is missing or invalid.");
                return false;
            }

            Scene loaded = SceneManager.GetSceneByName(scene.SceneName);
            if (loaded.IsValid() && loaded.isLoaded)
                return true;

            try
            {
                // Persistent is the only dependency loaded directly here. Once
                // Persistent is ready, GameManager and all gameplay scenes use
                // SceneLoader just like the normal runtime flow.
                AsyncOperation operation = SceneManager.LoadSceneAsync(scene.LoadKey, LoadSceneMode.Additive);
                if (operation == null)
                {
                    Debug.LogError($"ColdStartupBootstrap: Unity could not start loading {role} scene.");
                    return false;
                }

                return true;
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"ColdStartupBootstrap: Unity could not load {role} scene: {exception.Message}");
                return false;
            }
        }

        private void OnDestroy()
        {
            if (s_owner == this) s_owner = null;
        }

        private static void PrepareDirectOpenState(GameState? directOpenState)
        {
            Save.SaveSystem save = Save.SaveSystem.Instance;
            if (save == null)
            {
                Debug.LogError("ColdStartupBootstrap: SaveSystem is missing after Persistent loaded.");
                return;
            }

            if (directOpenState == GameState.MetaHub)
            {
                if (save.Meta != null) return;

                for (int slotIndex = 0; slotIndex < Save.SaveSystem.MaxSaveSlots; slotIndex++)
                {
                    if (save.GetSlotInfo(slotIndex).IsOccupied && save.TrySelectSlot(slotIndex))
                        return;
                }

                if (save.TryCreateSlot(0)) return;

                Debug.LogError("ColdStartupBootstrap: could not select or create a MetaHub save slot.");
                return;
            }

            if (directOpenState == GameState.LoadingRun || directOpenState == GameState.InRun)
            {
                // Direct-open gameplay is a clean debug run: do not reuse a persisted
                // interrupt save or a pending MetaHub allocation. PlayerStatsInitializer
                // then uses its authored full-Potential fallback.
                Zephyr.Core.Meta.RunEntryPotentialService.Clear();
                save.ClearRun();
            }
        }
    }
}
