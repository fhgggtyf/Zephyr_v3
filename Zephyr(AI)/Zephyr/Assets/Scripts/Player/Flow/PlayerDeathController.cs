using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zephyr.Core;
using Zephyr.Core.Combat;
using Zephyr.Core.DamageSystem;
using Zephyr.Core.Flow;
using Zephyr.Core.Health;
using Zephyr.Core.Interfaces;
using Zephyr.Core.Save;
using Zephyr.Core.Stats;
using Zephyr.Gameplay.Player.Core;
using Zephyr.Gameplay.Player.Input;
using Zephyr.Gameplay.Player.UI;
using Zephyr.Gameplay.Player.Visual;

namespace Zephyr.Gameplay.Player.Flow
{
    /// <summary>Owns player down, Potential loss, death presentation, and checkpoint respawn.</summary>
    public sealed class PlayerDeathController : MonoBehaviour
    {
        [Header("Presentation")]
        [SerializeField] private DeathView _viewPrefab;
        [SerializeField] private Transform _viewParent;
        [SerializeField] private string _deathAnimationName = "Death";
        [Min(0.1f)] [SerializeField] private float _deathAnimationFallbackDuration = 1.2f;

        private readonly Dictionary<int, float> _sessionPotentials = new Dictionary<int, float>();
        private DeathView _view;
        private GameObject _player;
        private HealthComponent _health;
        private StatsCore _stats;
        private DamageInfo _lastDamage;
        private bool _hasLastDamage;
        private bool _processingDown;
        private int _sessionDownCount;

        private void Start()
        {
            if (_viewPrefab != null)
            {
                _view = Instantiate(_viewPrefab, _viewParent != null ? _viewParent : transform);
                _view.RespawnRequested += Respawn;
                _view.GiveUpRequested += GiveUp;
                _view.Hide();
            }
            else
            {
                Debug.LogError("PlayerDeathController: Death View Prefab is not assigned.", this);
            }

            if (PlayerSpawner.Instance != null)
            {
                PlayerSpawner.Instance.PlayerSpawned += BindPlayer;
                PlayerSpawner.Instance.PlayerDespawned += UnbindPlayer;
                BindPlayer(PlayerSpawner.Instance.CurrentPlayer);
            }

            if (GameFlow.Instance != null)
                GameFlow.Instance.OnStateChanged += HandleGameStateChanged;
        }

        private void OnDestroy()
        {
            if (PlayerSpawner.Instance != null)
            {
                PlayerSpawner.Instance.PlayerSpawned -= BindPlayer;
                PlayerSpawner.Instance.PlayerDespawned -= UnbindPlayer;
            }

            if (GameFlow.Instance != null)
                GameFlow.Instance.OnStateChanged -= HandleGameStateChanged;

            if (_view != null)
            {
                _view.RespawnRequested -= Respawn;
                _view.GiveUpRequested -= GiveUp;
            }

            UnbindPlayer();
        }

        private void BindPlayer(GameObject player)
        {
            UnbindPlayer();
            _player = player;
            if (_player == null) return;

            _health = _player.GetComponent<HealthComponent>();
            _stats = _player.GetComponent<StatsCore>();
            RestorePotentials();

            CombatTargetAvailability availability = _player.GetComponent<CombatTargetAvailability>();
            availability?.SetAvailable(true);

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
            _hasLastDamage = false;
        }

        private void HandleDamageTaken(DamageInfo info, DamageResult result)
        {
            _lastDamage = info;
            _hasLastDamage = true;
            if (SaveSystem.Instance?.CurrentRun != null)
                SaveSystem.Instance.CurrentRun.TotalDamageTaken += result.TotalDamage;
        }

        private void HandleDowned()
        {
            if (_processingDown || _player == null || _stats == null) return;
            StartCoroutine(ProcessDown());
        }

        private IEnumerator ProcessDown()
        {
            _processingDown = true;
            SetPlayerDowned(true);

            ResolvePotentialLoss(out StatType target, out float before, out float loss, out float after);
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

            _view?.Show(target, before, loss, after > 0f);
        }

        private void ResolvePotentialLoss(out StatType target, out float before, out float loss, out float after)
        {
            target = StatType.MaxHp;
            float potentialAttack = 0f;
            if (_hasLastDamage && TryFindPotentialSource(_lastDamage.Source, out IPotentialAttackSource source))
            {
                target = source.TargetPotential;
                potentialAttack = Mathf.Max(0f, source.PotentialAttack);
            }

            int downCount = GetDownCount();
            loss = potentialAttack * Mathf.Pow(2f, Mathf.Min(downCount, 30));
            before = _stats.GetPotential(target);
            after = Mathf.Max(0f, before - loss);
            _stats.SetRunPotential(target, after);
            SetDownCount(downCount + 1);
        }

        private void Respawn()
        {
            if (!_processingDown || _stats == null) return;
            SavePotentials();
            _view?.Hide();
            _processingDown = false;
            PlayerSpawner.Instance?.RespawnCurrentPlayer();
        }

        private void GiveUp()
        {
            _view?.Hide();
            GameFlow flow = GameFlow.Instance;
            if (flow == null) return;

            if (flow.CurrentState == GameState.Tutorial || flow.CurrentState == GameState.LoadingRun ||
                flow.CurrentState == GameState.InRun || flow.CurrentState == GameState.Paused)
                flow.RequestTransition(GameState.MetaHub);
        }

        private void HandleGameStateChanged(GameState previous, GameState current)
        {
            if (current != GameState.MetaHub && current != GameState.MainMenu) return;
            StopAllCoroutines();
            _processingDown = false;
            _sessionDownCount = 0;
            _sessionPotentials.Clear();
            _view?.Hide();
        }

        private void SetPlayerDowned(bool downed)
        {
            if (_player == null) return;
            _player.GetComponent<CombatTargetAvailability>()?.SetAvailable(!downed);

            PlayerInputReader input = _player.GetComponentInChildren<PlayerInputReader>(true);
            if (input != null) input.enabled = !downed;

            Zephyr.Core.StateMachine.StateMachine stateMachine =
                _player.GetComponent<Zephyr.Core.StateMachine.StateMachine>();
            if (stateMachine != null) stateMachine.enabled = !downed;

            Rigidbody2D body = _player.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                if (downed) body.constraints = RigidbodyConstraints2D.FreezeAll;
            }
        }

        private void RestorePotentials()
        {
            if (_stats == null) return;
            Dictionary<int, float> saved = SaveSystem.Instance?.CurrentRun?.Potentials;
            Dictionary<int, float> source = saved != null && saved.Count > 0 ? saved : _sessionPotentials;
            if (source.Count == 0) return;

            foreach (KeyValuePair<int, float> pair in source)
                if (System.Enum.IsDefined(typeof(StatType), pair.Key))
                    _stats.SetRunPotential((StatType)pair.Key, pair.Value);
        }

        private void SavePotentials()
        {
            if (_stats == null) return;
            _sessionPotentials.Clear();
            foreach (StatType stat in System.Enum.GetValues(typeof(StatType)))
                _sessionPotentials[(int)stat] = _stats.GetPotential(stat);

            RunData run = SaveSystem.Instance?.CurrentRun;
            if (run == null) return;
            run.Potentials = new Dictionary<int, float>(_sessionPotentials);
            SaveSystem.Instance.SaveRun();
        }

        private int GetDownCount() => SaveSystem.Instance?.CurrentRun?.DownCount ?? _sessionDownCount;

        private void SetDownCount(int value)
        {
            _sessionDownCount = value;
            if (SaveSystem.Instance?.CurrentRun != null)
                SaveSystem.Instance.CurrentRun.DownCount = value;
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
