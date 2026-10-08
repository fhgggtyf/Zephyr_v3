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

        private void Start()
        {
            // A scene reached through SceneLoader already has a session. Never
            // reselect its profile or clear its run/allocation on a normal load.
            if (s_owner != null || SceneLoader.Instance != null || GameFlow.Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            s_owner = this;
            _directOpenScene = gameObject.scene;
            _directOpenState = ResolveDirectOpenState(_directOpenScene);
            _shouldLoadGameManager = _loadGameManager && RequiresGameManager(_directOpenState);

            EnsureSceneLoaded(_persistentScene, "Persistent");
            _waitingForPersistentServices = true;
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

        private static void EnsureSceneLoaded(SceneSO scene, string role)
        {
            if (scene == null || !scene.IsValid)
            {
                Debug.LogError($"ColdStartupBootstrap: {role} SceneSO is missing or invalid.");
                return;
            }

            Scene loaded = SceneManager.GetSceneByName(scene.SceneName);
            if (loaded.IsValid() && loaded.isLoaded)
                return;

            try
            {
                // Persistent is the only dependency loaded without SceneLoader:
                // it does not exist yet during a direct scene open.
                SceneManager.LoadSceneAsync(scene.LoadKey, LoadSceneMode.Additive);
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"ColdStartupBootstrap: Unity could not load {role} scene: {exception.Message}");
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
