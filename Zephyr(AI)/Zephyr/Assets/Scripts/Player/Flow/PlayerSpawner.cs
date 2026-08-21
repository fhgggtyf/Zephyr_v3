using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zephyr.Core.Camera;
using Zephyr.Core.Flow;

namespace Zephyr.Gameplay.Player.Flow
{
    /// <summary>
    /// Spawns one player into the active scene listed in Player Scenes. The
    /// player is scene-owned so unloading a tutorial or room also unloads it.
    /// </summary>
    [DefaultExecutionOrder(-150)]
    public sealed class PlayerSpawner : MonoBehaviour
    {
        public static PlayerSpawner Instance { get; private set; }

        [SerializeField] private GameObject _playerPrefab;
        [SerializeField] private SceneSO[] _playerScenes;
        [SerializeField] private Transform _spawnPoint;

        public GameObject CurrentPlayer { get; private set; }
        public event Action<GameObject> PlayerSpawned;
        public event Action PlayerDespawned;
        private Scene _playerScene;
        private Transform _respawnPoint;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            SceneManager.sceneUnloaded += HandleSceneUnloaded;
        }

        private void Start()
        {
            if (GameFlow.Instance != null)
                GameFlow.Instance.OnStateChanged += HandleGameStateChanged;
            EvaluateLoadedScenes();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneUnloaded -= HandleSceneUnloaded;
        }

        private void OnDestroy()
        {
            if (GameFlow.Instance != null)
                GameFlow.Instance.OnStateChanged -= HandleGameStateChanged;
            if (Instance == this) Instance = null;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (IsPlayerScene(scene)) SpawnIfNeeded(scene);
            else EvaluateLoadedScenes();
        }

        private void HandleSceneUnloaded(Scene scene)
        {
            if (_playerScene == scene)
            {
                _playerScene = default;
                _respawnPoint = null;
                CurrentPlayer = null;
                PlayerDespawned?.Invoke();
            }

            if (!HasLoadedPlayerScene() && !ShouldKeepCurrentPlayer()) DespawnPlayer();
        }

        private void HandleGameStateChanged(GameState previous, GameState current)
        {
            if (current == GameState.Tutorial || current == GameState.LoadingRun || current == GameState.InRun)
            {
                EvaluateLoadedScenes();
                return;
            }

            DespawnPlayer();
        }

        private void EvaluateLoadedScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (IsPlayerScene(scene))
                {
                    SpawnIfNeeded(scene);
                    return;
                }
            }

            DespawnPlayer();
        }

        private void SpawnIfNeeded(Scene gameplayScene)
        {
            // A player belongs to the scene that requested the spawn. If a new
            // gameplay scene is loaded, discard an old scene-owned instance so
            // it cannot survive as a hidden object in GameManager.
            if (CurrentPlayer != null)
            {
                if (CurrentPlayer.scene == gameplayScene) return;
                DespawnPlayer();
            }

            if (_playerPrefab == null)
            {
                Debug.LogError("PlayerSpawner: Player Prefab is not assigned.", this);
                return;
            }

            Transform sceneSpawnPoint = _respawnPoint != null &&
                                        _respawnPoint.gameObject.scene == gameplayScene
                ? _respawnPoint
                : FindSceneSpawnPoint(gameplayScene);
            _respawnPoint = sceneSpawnPoint != null ? sceneSpawnPoint : _spawnPoint;
            Transform spawnPoint = _respawnPoint;
            Vector3 position = spawnPoint != null ? spawnPoint.position : Vector3.zero;
            Quaternion rotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;
            CurrentPlayer = Instantiate(_playerPrefab, position, rotation);
            SceneManager.MoveGameObjectToScene(CurrentPlayer, gameplayScene);
            _playerScene = gameplayScene;

            CameraController camera = FindAnyObjectByType<CameraController>();
            camera?.SetFollow(CurrentPlayer.transform);
            PlayerSpawned?.Invoke(CurrentPlayer);
        }

        /// <summary>Updates the checkpoint used by the next down respawn.</summary>
        public void SetRespawnPoint(Transform checkpoint)
        {
            if (checkpoint != null && checkpoint.gameObject.scene == _playerScene)
                _respawnPoint = checkpoint;
        }

        /// <summary>Replaces the downed instance with a clean player at the active checkpoint.</summary>
        public void RespawnCurrentPlayer()
        {
            Scene scene = _playerScene;
            if (!scene.IsValid() || !scene.isLoaded) return;

            Transform point = _respawnPoint;
            DespawnPlayer();
            _respawnPoint = point;
            SpawnIfNeeded(scene);
        }

        private static Transform FindSceneSpawnPoint(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                PlayerSpawnPoint marker = root.GetComponentInChildren<PlayerSpawnPoint>(true);
                if (marker != null) return marker.transform;
            }

            return null;
        }

        private void DespawnPlayer()
        {
            if (CurrentPlayer == null)
            {
                _playerScene = default;
                return;
            }

            GameObject player = CurrentPlayer;
            CurrentPlayer = null;
            _playerScene = default;
            _respawnPoint = null;
            if (player != null) Destroy(player);
            PlayerDespawned?.Invoke();
        }

        private bool HasLoadedPlayerScene()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (IsPlayerScene(SceneManager.GetSceneAt(i))) return true;
            }

            return false;
        }

        private static bool ShouldKeepCurrentPlayer()
        {
            GameState state = GameFlow.Instance != null ? GameFlow.Instance.CurrentState : GameState.Boot;
            return state == GameState.Tutorial || state == GameState.LoadingRun || state == GameState.InRun;
        }

        private bool IsPlayerScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || _playerScenes == null) return false;

            foreach (SceneSO sceneSO in _playerScenes)
            {
                if (sceneSO != null && sceneSO.IsValid && scene.name == sceneSO.SceneName)
                    return true;
            }

            return false;
        }
    }
}
