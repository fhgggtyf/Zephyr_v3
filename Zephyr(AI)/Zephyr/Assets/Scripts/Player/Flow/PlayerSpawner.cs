using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zephyr.Contracts.Events;
using Zephyr.Core.Camera;
using Zephyr.Core.Events;
using Zephyr.Core.Flow;
using Zephyr.Contracts.Flow;
using Zephyr.Contracts.Interfaces;
using Zephyr.Core.Save;
using Zephyr.Core.Weapons;
using Zephyr.Core.Runtime.Lifecycle;
using Zephyr.Gameplay.Player.Core;

namespace Zephyr.Gameplay.Player.Flow
{
    /// <summary>
    /// Spawns one persistent player instance for the active gameplay scene. The
    /// instance is owned by GameManager so run gains and timed modifiers survive
    /// gameplay scene transitions; it is destroyed when leaving gameplay state.
    /// </summary>
    [DefaultExecutionOrder(-150)]
    [SingletonLifetime(SingletonLifetime.GameManager, "GameManager")]
    public sealed class PlayerSpawner : MonoBehaviour, IPlayerSpawnGateway, IGameManagerDependencyConsumer
    {
        private static PlayerSpawner _instance;
        public static PlayerSpawner Instance => _instance;

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
        private IGameFlowGateway _gameFlow;
        private ISaveDataGateway _saveData;
        private IEventBusGateway _eventBus;
        private IGameManagerServices _gameManagerServices;

        private void Awake()
        {
            if (!SingletonLifecycle.TryClaim(this, ref _instance, nameof(PlayerSpawner))
                || _instance != this)
            {
                Destroy(gameObject);
                return;
            }

        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            SceneManager.sceneUnloaded += HandleSceneUnloaded;
        }

        private void Start()
        {
            if (_gameFlow != null)
                _gameFlow.OnStateChanged += HandleGameStateChanged;
            EvaluateLoadedScenes();
        }

        public void Configure(IGameManagerServices services)
        {
            _gameManagerServices = services;
            _gameFlow = services?.GameFlow;
            _saveData = services?.SaveData;
            _eventBus = services?.EventBus;
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
            if (_gameFlow != null)
                _gameFlow.OnStateChanged -= HandleGameStateChanged;
            SingletonLifecycle.Release(this, ref _instance);
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
            }

            if (!HasLoadedPlayerScene() && !ShouldKeepCurrentPlayer())
                DespawnPlayer();
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
            Transform sceneSpawnPoint = _respawnPoint != null &&
                                        _respawnPoint.gameObject.scene == gameplayScene
                ? _respawnPoint
                : FindSceneSpawnPoint(gameplayScene);
            _respawnPoint = sceneSpawnPoint != null ? sceneSpawnPoint : _spawnPoint;

            if (CurrentPlayer != null)
            {
                Vector3 position = _hasPendingRespawnPosition
                    ? _pendingRespawnPosition
                    : _respawnPoint != null ? _respawnPoint.position : CurrentPlayer.transform.position;
                Quaternion rotation = _respawnPoint != null ? _respawnPoint.rotation : CurrentPlayer.transform.rotation;
                CurrentPlayer.transform.SetPositionAndRotation(position, rotation);
                _playerScene = gameplayScene;
                CameraController.FindForGameplay(gameplayScene)?.SetFollow(CurrentPlayer.transform);
                _hasPendingRespawnPosition = false;
                return;
            }

            if (_playerPrefab == null)
            {
                Debug.LogError("PlayerSpawner: Player Prefab is not assigned.", this);
                return;
            }

            Vector3 spawnPosition = _hasPendingRespawnPosition
                ? _pendingRespawnPosition
                : _respawnPoint != null ? _respawnPoint.position : Vector3.zero;
            Quaternion spawnRotation = _respawnPoint != null ? _respawnPoint.rotation : Quaternion.identity;
            CurrentPlayer = Instantiate(_playerPrefab, spawnPosition, spawnRotation);
            SceneManager.MoveGameObjectToScene(CurrentPlayer, gameObject.scene);
            ConfigurePlayerConsumers(CurrentPlayer);
            _playerScene = gameplayScene;
            PlayerStatsInitializer statsInitializer = CurrentPlayer.GetComponent<PlayerStatsInitializer>();
            statsInitializer?.Configure(_saveData);
            statsInitializer?.InitializeNow();
            ApplyPendingRespawnLoadout();

            CameraController camera = CameraController.FindForGameplay(gameplayScene);
            camera?.SetFollow(CurrentPlayer.transform);
            PlayerSpawned?.Invoke(CurrentPlayer);
            PublishPlayerTargetChanged();
        }

        private void ConfigurePlayerConsumers(GameObject player)
        {
            if (player == null || _gameManagerServices == null) return;
            MonoBehaviour[] consumers = player.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < consumers.Length; i++)
            {
                if (consumers[i] is IGameManagerDependencyConsumer consumer)
                    consumer.Configure(_gameManagerServices);
            }
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
            PublishPlayerTargetChanged();
            PlayerDespawned?.Invoke();
        }

        private void PublishPlayerTargetChanged()
        {
            _eventBus?.Publish(new PlayerTargetChangedEvent(CurrentPlayer));
        }

        private bool HasLoadedPlayerScene()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (IsPlayerScene(SceneManager.GetSceneAt(i))) return true;
            }

            return false;
        }

        private bool ShouldKeepCurrentPlayer()
        {
            GameState state = _gameFlow != null ? _gameFlow.CurrentState : GameState.Boot;
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
