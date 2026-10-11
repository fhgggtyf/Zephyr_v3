using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zephyr.Core;
using Zephyr.Core.DamageSystem;
using Zephyr.Core.Flow;
using Zephyr.Core.Health;
using Zephyr.Core.Interfaces;
using Zephyr.Core.Save;
using Zephyr.Core.Stats;
using Zephyr.Gameplay.Player.Core;
using Zephyr.Gameplay.Player.UI;
using Zephyr.Gameplay.Player.Visual;

namespace Zephyr.Gameplay.Player.Flow
{
    /// <summary>Unity facade for player down, death presentation, and respawn flow.</summary>
    public sealed class PlayerDeathController : MonoBehaviour
    {
        [Header("Presentation")]
        [SerializeField] private DeathView _viewPrefab;
        [SerializeField] private Transform _viewParent;
        [SerializeField] private string _deathAnimationName = "Death";
        [Min(0.1f)] [SerializeField] private float _deathAnimationFallbackDuration = 1.2f;

        private readonly Dictionary<int, float> _sessionPotentials = new Dictionary<int, float>();
        private readonly PotentialLossService _potentialLoss = new PotentialLossService();
        private RespawnCoordinator _respawnCoordinator;

        private PlayerSpawner _playerSpawner;
        private GameFlow _gameFlow;
        private ISaveDataGateway _saveGateway;
        private DeathPresentationController _presentation;
        private PlayerDownUseCase _downUseCase;
        private GameObject _player;
        private HealthComponent _health;
        private StatsCore _stats;
        private HurtReceiver _hurtReceiver;
        private DamageInfo _lastDamage;
        private bool _hasLastDamage;
        private bool _processingDown;
        private int _sessionDownCount;

        private void Start()
        {
            ResolveDependencies();
            if (_viewPrefab != null)
                _presentation = new DeathPresentationController(_viewPrefab, _viewParent != null ? _viewParent : transform,
                    Respawn, GiveUp);
            else
                Debug.LogError("PlayerDeathController: Death View Prefab is not assigned.", this);

            if (_playerSpawner != null)
            {
                _playerSpawner.PlayerSpawned += BindPlayer;
                _playerSpawner.PlayerDespawned += UnbindPlayer;
                BindPlayer(_playerSpawner.CurrentPlayer);
            }

            if (_gameFlow != null)
                _gameFlow.OnStateChanged += HandleGameStateChanged;
        }

        private void OnDestroy()
        {
            if (_playerSpawner != null)
            {
                _playerSpawner.PlayerSpawned -= BindPlayer;
                _playerSpawner.PlayerDespawned -= UnbindPlayer;
            }

            if (_gameFlow != null)
                _gameFlow.OnStateChanged -= HandleGameStateChanged;

            _presentation?.Dispose();
            UnbindPlayer();
        }

        private void BindPlayer(GameObject player)
        {
            UnbindPlayer();
            _player = player;
            if (_player == null) return;

            _health = _player.GetComponent<HealthComponent>();
            _stats = _player.GetComponent<StatsCore>();
            _hurtReceiver = _player.GetComponentInChildren<HurtReceiver>(true);
            _hurtReceiver?.SetInvincible(_gameFlow?.CurrentState == GameState.Tutorial);
            _downUseCase = new PlayerDownUseCase(_player);
            RestorePotentials();
            _player.GetComponent<Zephyr.Core.Combat.CombatTargetAvailability>()?.SetAvailable(true);

            if (_health != null)
            {
                _health.DamageTaken += HandleDamageTaken;
                _health.Died += HandleDowned;
            }
        }

        private void UnbindPlayer()
        {
            if (_health != null)
            {
                _health.DamageTaken -= HandleDamageTaken;
                _health.Died -= HandleDowned;
            }

            _player = null;
            _health = null;
            _stats = null;
            _hurtReceiver = null;
            _downUseCase = null;
            _hasLastDamage = false;
        }

        private void HandleDamageTaken(DamageInfo info, DamageResult result)
        {
            _lastDamage = info;
            _hasLastDamage = true;
            if (_saveGateway?.CurrentRun != null)
                _saveGateway.CurrentRun.TotalDamageTaken += result.TotalDamage;
        }

        private void HandleDowned()
        {
            if (_processingDown || _player == null || _stats == null) return;
            StartCoroutine(ProcessDown());
        }

        private IEnumerator ProcessDown()
        {
            _processingDown = true;
            _downUseCase?.SetDown(true);

            PotentialLossResult result = ResolvePotentialLoss();
            SavePotentials();

            SpriteAnimator animator = _player.GetComponentInChildren<SpriteAnimator>(true);
            animator?.PlayAnimation(_deathAnimationName, 1f, true);

            float elapsed = 0f;
            while (elapsed < _deathAnimationFallbackDuration)
            {
                if (animator != null && animator.IsCurrentAnimationComplete()) break;
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            _presentation?.Show(result.Target, result.Before, result.Loss, result.After > 0f);
        }

        private PotentialLossResult ResolvePotentialLoss()
        {
            StatType target = StatType.MaxHp;
            float potentialAttack = 0f;
            if (_hasLastDamage && TryFindPotentialSource(_lastDamage.Source, out IPotentialAttackSource source))
            {
                target = source.TargetPotential;
                potentialAttack = source.PotentialAttack;
            }

            PotentialLossResult result = _potentialLoss.Calculate(_stats, target, potentialAttack, GetDownCount());
            _stats.SetRunPotential(result.Target, result.After);
            SetDownCount(GetDownCount() + 1);
            return result;
        }

        private void Respawn()
        {
            if (!_processingDown || _stats == null) return;
            SavePotentials();
            _presentation?.Hide();
            _processingDown = false;
            _respawnCoordinator.Respawn();
        }

        private void GiveUp()
        {
            _presentation?.Hide();
            _respawnCoordinator.GiveUp();
        }

        private void HandleGameStateChanged(GameState previous, GameState current)
        {
            _hurtReceiver?.SetInvincible(current == GameState.Tutorial);
            if (current != GameState.MetaHub && current != GameState.MainMenu) return;
            StopAllCoroutines();
            _processingDown = false;
            _sessionDownCount = 0;
            _sessionPotentials.Clear();
            _presentation?.Hide();
        }

        private void RestorePotentials()
        {
            if (_stats == null) return;
            Dictionary<int, float> saved = _saveGateway?.CurrentRun?.Potentials;
            Dictionary<int, float> source = saved != null && saved.Count > 0 ? saved : _sessionPotentials;
            _potentialLoss.Restore(_stats, source, _stats.SetRunPotential);
        }

        private void SavePotentials()
        {
            if (_stats == null) return;
            _sessionPotentials.Clear();
            foreach (KeyValuePair<int, float> pair in _potentialLoss.Capture(_stats))
                _sessionPotentials[pair.Key] = pair.Value;

            RunData run = _saveGateway?.CurrentRun;
            if (run == null) return;
            _potentialLoss.SaveToRun(run, _sessionPotentials);
            _saveGateway.SaveRun();
        }

        private int GetDownCount() => _saveGateway?.CurrentRun?.DownCount ?? _sessionDownCount;

        private void SetDownCount(int value)
        {
            _sessionDownCount = value;
            if (_saveGateway?.CurrentRun != null)
                _saveGateway.CurrentRun.DownCount = value;
        }

        private void ResolveDependencies()
        {
            _playerSpawner = PlayerSpawner.Instance;
            _gameFlow = GameFlow.Instance;
            _saveGateway = SaveSystem.Instance;
            _respawnCoordinator = new RespawnCoordinator(_playerSpawner, _gameFlow);
        }

        private static bool TryFindPotentialSource(GameObject sourceObject, out IPotentialAttackSource source)
        {
            source = null;
            if (sourceObject == null) return false;
            foreach (MonoBehaviour behaviour in sourceObject.GetComponentsInParent<MonoBehaviour>(true))
            {
                if (behaviour is IPotentialAttackSource potentialSource)
                {
                    source = potentialSource;
                    return true;
                }
            }

            return false;
        }
    }
}
