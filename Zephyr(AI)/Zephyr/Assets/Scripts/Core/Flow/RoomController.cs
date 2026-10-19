using System;
using System.Collections.Generic;
using UnityEngine;
using Zephyr.Contracts.Interfaces;
using Zephyr.Core.Flow;
using Zephyr.Core.Runtime.Stats;
using Zephyr.Core.Stats;

namespace Zephyr.Core.Levels
{
    public enum RoomPhase { Loading, Entering, Active, InterWave, Cleared, Exiting }

    /// <summary>Owns one active room's lifecycle. Wave composition is supplied by a deterministic spawner.</summary>
    public sealed class RoomController : MonoBehaviour, IRoomRuntimeDependencyConsumer, IStatModifierSource
    {
        [SerializeField] private bool _bossRoom;
        [SerializeField] private SceneSO _scene;
        [Header("Room Stat Modifiers")]
        [SerializeField] private List<StatModifierDefinition> _statModifiers = new List<StatModifierDefinition>();
        [SerializeField] private bool _locksUntilCleared = true;
        [SerializeField, Min(1)] private int _totalWaves = 1;
        [SerializeField, Min(0f)] private float _interWaveDelay = 1f;
        [SerializeField] private MonoBehaviour[] _dependencyConsumers;
        private readonly HashSet<MonoBehaviour> _configuredConsumers = new HashSet<MonoBehaviour>();
        private int _livingEnemies;
        private int _waveIndex = -1;
        private IDisposable _interWaveTimer;
        private IEventBusGateway _eventBus;
        private ITimerGateway _timer;
        private IPickupRuntimeContext _runtimeContext;
        private RoomDropSpawnGateway _dropSpawnGateway;

        public bool IsBossRoom => _bossRoom;
        public RoomPhase Phase { get; private set; } = RoomPhase.Loading;
        public int WaveIndex => _waveIndex;
        public bool ExitsLocked => _locksUntilCleared && Phase != RoomPhase.Cleared;
        public event Action<int> OnWaveRequested;
        public event Action OnRoomCleared;
        public IReadOnlyList<StatModifierDefinition> Modifiers => _statModifiers;

        private void Awake()
        {
        }

        private void OnEnable() => StatModifierRegistry.Register(this);

        private void OnDisable()
        {
            StatModifierRegistry.Unregister(this);
            _interWaveTimer?.Dispose();
        }

        public void Configure(IRoomRuntimeDependencies dependencies)
        {
            if (_runtimeContext != null) return;
            if (dependencies == null) return;
            _eventBus = dependencies.EventBus;
            _timer = dependencies.Timer;
            _runtimeContext = dependencies.PickupContext;
            if (_runtimeContext == null) return;
            _dropSpawnGateway = new RoomDropSpawnGateway(dependencies.DropSpawn,
                dependencies.Seed, dependencies.RoomDepth);

            ConfigureConsumers(_dependencyConsumers);
            ConfigureSceneConsumers();
        }

        private void ConfigureConsumers(MonoBehaviour[] consumers)
        {
            if (consumers == null) return;
            for (int i = 0; i < consumers.Length; i++) ConfigureConsumer(consumers[i]);
        }

        private void ConfigureSceneConsumers()
        {
            GameObject[] roots = gameObject.scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                MonoBehaviour[] consumers = roots[i].GetComponentsInChildren<MonoBehaviour>(true);
                for (int j = 0; j < consumers.Length; j++) ConfigureConsumer(consumers[j]);
            }
        }

        private void ConfigureConsumer(MonoBehaviour source)
        {
            if (source == null || !_configuredConsumers.Add(source)) return;
            if (source is IDropSpawnGatewayConsumer dropConsumer)
                dropConsumer.Configure(_dropSpawnGateway);
            else if (source is IPickupRuntimeContextConsumer pickupConsumer)
                pickupConsumer.Configure(_runtimeContext);
            else if (source is IPlayerTargetContextConsumer targetConsumer)
                targetConsumer.Configure(_runtimeContext);
            else
                _configuredConsumers.Remove(source);
        }

        private void Start()
        {
            EnterRoom();
            _eventBus?.Publish(new RoomEnteredInfo(_scene));
        }

        public void EnterRoom()
        {
            if (Phase != RoomPhase.Loading) return;
            Phase = RoomPhase.Entering;
            StartNextWave();
        }

        public void RegisterEnemy() => _livingEnemies++;

        public void NotifyEnemyDefeated()
        {
            _livingEnemies = Mathf.Max(0, _livingEnemies - 1);
            if (Phase != RoomPhase.Active || _livingEnemies > 0) return;
            CompleteWave();
        }

        public void BeginExit() { if (Phase == RoomPhase.Cleared) Phase = RoomPhase.Exiting; }

        private void StartNextWave()
        {
            _waveIndex++;
            _livingEnemies = 0;
            Phase = RoomPhase.Active;
            OnWaveRequested?.Invoke(_waveIndex);
            ConfigureSceneConsumers();
            if (_livingEnemies == 0) CompleteWave();
        }

        private void CompleteWave()
        {
            if (_waveIndex + 1 >= _totalWaves)
            {
                ClearRoom();
                return;
            }

            Phase = RoomPhase.InterWave;
            _interWaveTimer?.Dispose();
            _interWaveTimer = _timer?.SetTimeout(_interWaveDelay, StartNextWave);
        }

        private void ClearRoom()
        {
            Phase = RoomPhase.Cleared;
            OnRoomCleared?.Invoke();
        }

        private void OnDestroy()
        {
            _dropSpawnGateway?.Dispose();
        }

        public bool AppliesTo(IStatModifierTarget target) => target != null;
    }

    public readonly struct RoomEnteredInfo
    {
        public readonly SceneSO Scene;
        public RoomEnteredInfo(SceneSO scene) => Scene = scene;
    }
}
