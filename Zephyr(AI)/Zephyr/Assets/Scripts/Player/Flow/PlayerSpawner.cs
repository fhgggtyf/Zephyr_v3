using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zephyr.Core.Camera;
using Zephyr.Core.Flow;
using Zephyr.Core.Weapons;

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
        private Coroutine _pendingSpawnRoutine;
        private RespawnLoadout _pendingRespawnLoadout;
        private bool _hasPendingRespawnLoadout;
        private Vector3 _pendingRespawnPosition;
        private bool _hasPendingRespawnPosition;

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

            if (_pendingSpawnRoutine != null)
            {
                StopCoroutine(_pendingSpawnRoutine);
                _pendingSpawnRoutine = null;
            }
        }

        private void OnDestroy()
        {
            if (GameFlow.Instance != null)
                GameFlow.Instance.OnStateChanged -= HandleGameStateChanged;
            if (Instance == this) Instance = null;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (IsPlayerScene(scene)) QueueSpawnAfterSceneActivation(scene);
            else EvaluateLoadedScenes();
        }

        private void QueueSpawnAfterSceneActivation(Scene scene)
        {
            if (_pendingSpawnRoutine != null)
                StopCoroutine(_pendingSpawnRoutine);

            _pendingSpawnRoutine = StartCoroutine(SpawnAfterSceneActivation(scene));
        }

        private IEnumerator SpawnAfterSceneActivation(Scene scene)
        {
            // SceneLoader sets the newly loaded scene active after Unity invokes
            // sceneLoaded. Waiting one frame guarantees that scene modifiers and
            // the active-scene contract are settled before the player Awake runs.
            yield return null;
            _pendingSpawnRoutine = null;

            if (!scene.IsValid() || !scene.isLoaded || !IsPlayerScene(scene))
                yield break;

            SpawnIfNeeded(scene);
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
            Vector3 position = _hasPendingRespawnPosition
                ? _pendingRespawnPosition
                : spawnPoint != null ? spawnPoint.position : Vector3.zero;
            Quaternion rotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;
            CurrentPlayer = Instantiate(_playerPrefab, position, rotation);
            SceneManager.MoveGameObjectToScene(CurrentPlayer, gameplayScene);
            _playerScene = gameplayScene;
            ApplyPendingRespawnLoadout();

            CameraController camera = CameraController.FindForGameplay(gameplayScene);
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
            CaptureCurrentLoadout();
            if (CurrentPlayer != null)
            {
                _pendingRespawnPosition = CurrentPlayer.transform.position;
                _hasPendingRespawnPosition = true;
            }
            DespawnPlayer();
            _respawnPoint = point;
            SpawnIfNeeded(scene);
        }

        private void CaptureCurrentLoadout()
        {
            WeaponController controller = CurrentPlayer != null
                ? CurrentPlayer.GetComponentInChildren<WeaponController>(true)
                : null;
            if (controller == null) return;

            _pendingRespawnLoadout = new RespawnLoadout
            {
                Primary = controller.GetWeapon(WeaponSlot.Primary),
                Secondary = controller.GetWeapon(WeaponSlot.Secondary),
                ActiveSlot = controller.ActiveSlot
            };
            _hasPendingRespawnLoadout = true;
        }

        private void ApplyPendingRespawnLoadout()
        {
            if (_hasPendingRespawnLoadout && CurrentPlayer != null)
            {
                WeaponController controller = CurrentPlayer.GetComponentInChildren<WeaponController>(true);
                if (controller != null)
                {
                    controller.Equip(WeaponSlot.Primary, _pendingRespawnLoadout.Primary);
                    controller.Equip(WeaponSlot.Secondary, _pendingRespawnLoadout.Secondary);
                    controller.SetActiveSlot(_pendingRespawnLoadout.ActiveSlot);
                }
            }

            _hasPendingRespawnLoadout = false;
            _hasPendingRespawnPosition = false;
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

        private struct RespawnLoadout
        {
            public WeaponSO Primary;
            public WeaponSO Secondary;
            public WeaponSlot ActiveSlot;
        }
    }
}
