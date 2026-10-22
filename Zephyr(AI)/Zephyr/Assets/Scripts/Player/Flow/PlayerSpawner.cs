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
using Zephyr.Core.Health;
using Zephyr.Core.Stats;
using Zephyr.Core.Runtime.Lifecycle;
using Zephyr.Gameplay.Player.Core;

namespace Zephyr.Gameplay.Player.Flow
{
    /// <summary>
    /// Coordinates player instances for gameplay scenes. Runtime state is held
    /// temporarily by GameManager while a room transition replaces the instance.
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
        public bool IsCurrentPlayerCameraBound
        {
            get
            {
                if (CurrentPlayer == null || !_playerScene.IsValid()) return false;
                CameraController camera = CameraController.FindForGameplay(_playerScene);
                return camera != null && camera.IsFollowing(CurrentPlayer.transform);
            }
        }
        public bool IsCurrentPlayerCameraStable
        {
            get
            {
                if (CurrentPlayer == null || !_playerScene.IsValid()) return false;
                CameraController camera = CameraController.FindForGameplay(_playerScene);
                return camera != null && camera.IsFollowingStable(CurrentPlayer.transform);
            }
        }
        public event Action<GameObject> PlayerSpawned;
        public event Action PlayerDespawned;
        private Scene _playerScene;
        private Transform _respawnPoint;
        private Coroutine _pendingSpawnRoutine;
        private RespawnLoadout _pendingRespawnLoadout;
        private bool _hasPendingRespawnLoadout;
        private PlayerRuntimeSnapshot _pendingRuntimeState;
        private Vector3 _pendingRespawnPosition;
        private bool _hasPendingRespawnPosition;
        private IGameFlowGateway _gameFlow;
        private ISceneLoadingGateway _sceneLoading;
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
            if (_sceneLoading != null)
                _sceneLoading.SceneUnloading += HandleSceneUnloading;
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
            if (_sceneLoading != null)
                _sceneLoading.SceneUnloading -= HandleSceneUnloading;
            _sceneLoading = services?.SceneLoading;
            if (isActiveAndEnabled && _sceneLoading != null)
                _sceneLoading.SceneUnloading += HandleSceneUnloading;
            _saveData = services?.SaveData;
            _eventBus = services?.EventBus;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneUnloaded -= HandleSceneUnloaded;
            if (_sceneLoading != null)
                _sceneLoading.SceneUnloading -= HandleSceneUnloading;

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

        private void HandleSceneUnloading(SceneSO departingScene, SceneSO targetScene)
        {
            if (!IsPlayerScene(departingScene)) return;

            if (IsPlayerScene(targetScene))
            {
                if (CurrentPlayer != null)
                    CaptureCurrentRuntimeState();
            }
            else
            {
                _pendingRuntimeState = null;
            }

            DespawnPlayer();
        }

        private void HandleGameStateChanged(GameState previous, GameState current)
        {
            if (current == GameState.Tutorial || current == GameState.LoadingRun || current == GameState.InRun)
            {
                EvaluateLoadedScenes();
                return;
            }

            _pendingRuntimeState = null;
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
                if (CurrentPlayer.scene == gameplayScene)
                {
                    CameraController.FindForGameplay(gameplayScene)?.SetFollow(CurrentPlayer.transform);
                    return;
                }

                DespawnPlayer();
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
            SceneManager.MoveGameObjectToScene(CurrentPlayer, gameplayScene);
            ConfigurePlayerConsumers(CurrentPlayer);
            _playerScene = gameplayScene;
            PlayerStatsInitializer statsInitializer = CurrentPlayer.GetComponent<PlayerStatsInitializer>();
            statsInitializer?.Configure(_saveData);
            statsInitializer?.InitializeNow();
            ApplyPendingRuntimeState();
            ApplyPendingRespawnLoadout();

            CameraController camera = CameraController.FindForGameplay(gameplayScene);
            camera?.SetFollow(CurrentPlayer.transform);
            PlayerSpawned?.Invoke(CurrentPlayer);
            PublishPlayerTargetChanged();
        }

        private void CaptureCurrentRuntimeState()
        {
            StatsCore stats = CurrentPlayer.GetComponent<StatsCore>();
            HealthComponent health = CurrentPlayer.GetComponent<HealthComponent>();
            PlayerResourceController resources = CurrentPlayer.GetComponent<PlayerResourceController>();
            WeaponController weapons = CurrentPlayer.GetComponentInChildren<WeaponController>(true);

            _pendingRuntimeState = new PlayerRuntimeSnapshot
            {
                Stats = stats != null ? stats.CaptureRuntimeState() : null,
                CurrentHp = health != null ? health.CurrentHp : 0f,
                CurrentShield = health != null ? health.CurrentShield : 0f,
                Resources = resources != null ? resources.CaptureRuntimeState() : default,
                PrimaryWeapon = weapons != null ? weapons.GetWeapon(WeaponSlot.Primary) : null,
                SecondaryWeapon = weapons != null ? weapons.GetWeapon(WeaponSlot.Secondary) : null,
                ActiveSlot = weapons != null ? weapons.ActiveSlot : WeaponSlot.Primary,
                HasHealth = health != null,
                HasResources = resources != null,
                HasWeapons = weapons != null
            };
        }

        private void ApplyPendingRuntimeState()
        {
            PlayerRuntimeSnapshot state = _pendingRuntimeState;
            _pendingRuntimeState = null;
            if (state == null || CurrentPlayer == null) return;

            CurrentPlayer.GetComponent<StatsCore>()?.RestoreRuntimeState(state.Stats);

            if (state.HasHealth)
                CurrentPlayer.GetComponent<HealthComponent>()?.RestoreRuntimeState(
                    state.CurrentHp, state.CurrentShield);

            if (state.HasResources)
                CurrentPlayer.GetComponent<PlayerResourceController>()?.RestoreRuntimeState(state.Resources);

            if (state.HasWeapons)
            {
                WeaponController weapons = CurrentPlayer.GetComponentInChildren<WeaponController>(true);
                if (weapons != null)
                {
                    weapons.Equip(WeaponSlot.Primary, state.PrimaryWeapon);
                    weapons.Equip(WeaponSlot.Secondary, state.SecondaryWeapon);
                    weapons.SetActiveSlot(state.ActiveSlot);
                }
            }
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

        private bool IsPlayerScene(SceneSO scene)
        {
            if (scene == null || _playerScenes == null) return false;

            foreach (SceneSO playerScene in _playerScenes)
            {
                if (playerScene != null &&
                    (playerScene == scene || playerScene.Id == scene.Id ||
                     playerScene.SceneName == scene.SceneName))
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

        private sealed class PlayerRuntimeSnapshot
        {
            public StatsCore.RuntimeState Stats;
            public float CurrentHp;
            public float CurrentShield;
            public PlayerResourceController.RuntimeState Resources;
            public WeaponSO PrimaryWeapon;
            public WeaponSO SecondaryWeapon;
            public WeaponSlot ActiveSlot;
            public bool HasHealth;
            public bool HasResources;
            public bool HasWeapons;
        }
    }
}
