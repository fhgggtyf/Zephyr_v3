using System;
using UnityEngine;
using Zephyr.Core.Flow;
using Zephyr.Core.Events;
using Zephyr.Core.Timer;

namespace Zephyr.Core.Levels
{
    public enum RoomPhase { Loading, Entering, Active, InterWave, Cleared, Exiting }

    /// <summary>Owns one active room's lifecycle. Wave composition is supplied by a deterministic spawner.</summary>
    public sealed class RoomController : MonoBehaviour
    {
        [SerializeField] private bool _bossRoom;
        [SerializeField] private SceneSO _scene;
        [SerializeField] private bool _locksUntilCleared = true;
        [SerializeField, Min(1)] private int _totalWaves = 1;
        [SerializeField, Min(0f)] private float _interWaveDelay = 1f;
        private int _livingEnemies;
        private int _waveIndex = -1;
        private TimerHandle _interWaveTimer;

        public bool IsBossRoom => _bossRoom;
        public RoomPhase Phase { get; private set; } = RoomPhase.Loading;
        public int WaveIndex => _waveIndex;
        public bool ExitsLocked => _locksUntilCleared && Phase != RoomPhase.Cleared;
        public event Action<int> OnWaveRequested;
        public event Action OnRoomCleared;

        private void Start()
        {
            EnterRoom();
            EventBus.Instance?.Publish(new RoomEnteredInfo(_scene));
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
            _interWaveTimer?.Cancel();
            _interWaveTimer = TimerService.Instance?.SetTimeout(_interWaveDelay, StartNextWave);
        }

        private void ClearRoom()
        {
            Phase = RoomPhase.Cleared;
            OnRoomCleared?.Invoke();
        }

        private void OnDisable() => _interWaveTimer?.Cancel();
    }

    public readonly struct RoomEnteredInfo
    {
        public readonly SceneSO Scene;
        public RoomEnteredInfo(SceneSO scene) => Scene = scene;
    }
}
