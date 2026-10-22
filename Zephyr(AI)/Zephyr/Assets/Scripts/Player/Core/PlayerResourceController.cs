using UnityEngine;
using Zephyr.Core;
using Zephyr.Core.Stats;
using Zephyr.Core.Weapons;
using Zephyr.Gameplay.Player.Input;

namespace Zephyr.Gameplay.Player.Core
{
    /// <summary>
    /// Manages current stamina and energy values on the player.
    /// Max values come from StatsCore. Provides Consume methods for
    /// weapon attacks, skills, and other actions that cost resources.
    /// </summary>
    [RequireComponent(typeof(StatsCore))]
    public class PlayerResourceController : MonoBehaviour, IAttackResourceConsumer
    {
        [Header("Regeneration")]
        [SerializeField] private float _staminaRegenPerSecond = 15f;
        [SerializeField] private float _energyRegenPerSecond = 5f;
        [SerializeField, Min(0f)] private float _regenDelayAfterUse = 1f;

        [Header("Movement Costs")]
        [SerializeField, Min(0f)] private float _sprintStaminaPerSecond = 10f;
        [SerializeField, Min(0f)] private float _sprintRestartStaminaThreshold = 5f;
        [SerializeField, Min(0f)] private float _jumpStaminaCost = 15f;

        [Header("Debug")]
        [SerializeField] private bool _logConsumption;

        private StatsCore _statsCore;
        [SerializeField] private PlayerInputReader _inputReader;
        private float _currentStamina;
        private float _currentEnergy;
        private float _staminaRegenDelayRemaining;
        private float _energyRegenDelayRemaining;
        private bool _sprintConsumptionActive;
        private bool _sprintRecoveryLockout;
        private bool _runtimeStateRestored;

        public float CurrentStamina => _currentStamina;
        public float CurrentEnergy => _currentEnergy;
        public float MaxStamina => _statsCore != null ? _statsCore.GetStatValue(StatType.Stamina) : 100f;
        public float MaxEnergy => _statsCore != null ? _statsCore.GetStatValue(StatType.Energy) : 50f;
        public bool CanSprint => _sprintStaminaPerSecond <= 0f || _currentStamina > 0f;
        public bool CanJump => CanAfford(AttackResourceType.Stamina, _jumpStaminaCost);

        /// <summary>
        /// A fresh Shift press may start with any positive stamina. A held press
        /// that already entered Sprint must recover to the restart threshold.
        /// </summary>
        public bool CanStartSprint(bool isFreshInput)
        {
            if (_sprintStaminaPerSecond <= 0f) return true;
            if (_currentStamina <= 0f) return false;
            return isFreshInput || _currentStamina >= _sprintRestartStaminaThreshold;
        }

        private void Awake()
        {
            _statsCore = GetComponent<StatsCore>();
            ResetToMax();
        }

        private void Start()
        {
            // All stat initializers have completed Awake by this point.
            if (!_runtimeStateRestored)
                ResetToMax();
        }

        private void Update()
        {
            UpdateSprintConsumptionState();
            if (_sprintConsumptionActive)
                ConsumeSprintStamina(Time.deltaTime);
            Regenerate();
        }

        private bool IsSprintInputActive => _inputReader != null &&
            _inputReader.HasSprintInput && _inputReader.HasHorizontalInput;

        private void UpdateSprintConsumptionState()
        {
            if (!IsSprintInputActive)
            {
                _sprintConsumptionActive = false;
                _sprintRecoveryLockout = false;
                return;
            }

            if (_sprintConsumptionActive || _currentStamina <= 0f)
                return;

            if (!_sprintRecoveryLockout || _currentStamina >= _sprintRestartStaminaThreshold)
                _sprintConsumptionActive = true;
        }

        private void Regenerate()
        {
            _staminaRegenDelayRemaining = Mathf.Max(0f,
                _staminaRegenDelayRemaining - Time.deltaTime);
            _energyRegenDelayRemaining = Mathf.Max(0f,
                _energyRegenDelayRemaining - Time.deltaTime);

            if (_staminaRegenDelayRemaining <= 0f && _currentStamina < MaxStamina)
            {
                _currentStamina = Mathf.Min(MaxStamina, _currentStamina + _staminaRegenPerSecond * Time.deltaTime);
            }

            if (_energyRegenDelayRemaining <= 0f && _currentEnergy < MaxEnergy)
            {
                _currentEnergy = Mathf.Min(MaxEnergy, _currentEnergy + _energyRegenPerSecond * Time.deltaTime);
            }
        }

        public bool TryConsumeStamina(float amount)
        {
            if (amount <= 0f) return true;
            if (_currentStamina < amount) return false;

            _currentStamina -= amount;
            _staminaRegenDelayRemaining = _regenDelayAfterUse;

#if UNITY_EDITOR
            if (_logConsumption)
                Debug.Log($"[Resource] Consumed {amount:F1} stamina. Remaining: {_currentStamina:F1}", this);
#endif
            return true;
        }

        public bool TryConsumeEnergy(float amount)
        {
            if (amount <= 0f) return true;
            if (_currentEnergy < amount) return false;

            _currentEnergy -= amount;
            _energyRegenDelayRemaining = _regenDelayAfterUse;

#if UNITY_EDITOR
            if (_logConsumption)
                Debug.Log($"[Resource] Consumed {amount:F1} energy. Remaining: {_currentEnergy:F1}", this);
#endif
            return true;
        }

        public void ConsumeSprintStamina(float deltaTime)
        {
            if (deltaTime <= 0f || _sprintStaminaPerSecond <= 0f) return;
            _staminaRegenDelayRemaining = _regenDelayAfterUse;
            if (_currentStamina <= 0f) return;
            _currentStamina = Mathf.Max(0f,
                _currentStamina - _sprintStaminaPerSecond * deltaTime);

            if (_currentStamina <= 0f)
            {
                _sprintConsumptionActive = false;
                _sprintRecoveryLockout = true;
            }
        }

        public bool TryConsumeJumpStamina()
        {
            return TryConsumeStamina(_jumpStaminaCost);
        }

        public bool CanAfford(AttackResourceType type, float amount)
        {
            return type switch
            {
                AttackResourceType.Stamina => _currentStamina >= amount,
                AttackResourceType.Energy => _currentEnergy >= amount,
                _ => true
            };
        }

        public bool TryConsume(AttackResourceType type, float amount)
        {
            return type switch
            {
                AttackResourceType.Stamina => TryConsumeStamina(amount),
                AttackResourceType.Energy => TryConsumeEnergy(amount),
                _ => true
            };
        }

        public bool CanAffordAttackResource(AttackResourceCost cost)
        {
            if (cost.ResourceType == AttackResourceType.None || cost.Amount <= 0f) return true;
            return CanAfford(cost.ResourceType, cost.Amount);
        }

        public bool TryConsumeAttackResource(AttackResourceCost cost)
        {
            if (!CanAffordAttackResource(cost)) return false;
            return TryConsume(cost.ResourceType, cost.Amount);
        }

        public void ResetToMax()
        {
            _currentStamina = MaxStamina;
            _currentEnergy = MaxEnergy;
            _staminaRegenDelayRemaining = 0f;
            _energyRegenDelayRemaining = 0f;
            _sprintConsumptionActive = false;
            _sprintRecoveryLockout = false;
            _runtimeStateRestored = false;
        }

        public RuntimeState CaptureRuntimeState()
        {
            return new RuntimeState(_currentStamina, _currentEnergy,
                _staminaRegenDelayRemaining, _energyRegenDelayRemaining);
        }

        public void RestoreRuntimeState(RuntimeState state)
        {
            _currentStamina = Mathf.Clamp(state.CurrentStamina, 0f, MaxStamina);
            _currentEnergy = Mathf.Clamp(state.CurrentEnergy, 0f, MaxEnergy);
            _staminaRegenDelayRemaining = Mathf.Max(0f, state.StaminaRegenDelayRemaining);
            _energyRegenDelayRemaining = Mathf.Max(0f, state.EnergyRegenDelayRemaining);
            _sprintConsumptionActive = false;
            _sprintRecoveryLockout = false;
            _runtimeStateRestored = true;
        }

        public readonly struct RuntimeState
        {
            public float CurrentStamina { get; }
            public float CurrentEnergy { get; }
            public float StaminaRegenDelayRemaining { get; }
            public float EnergyRegenDelayRemaining { get; }

            public RuntimeState(float currentStamina, float currentEnergy,
                float staminaRegenDelayRemaining, float energyRegenDelayRemaining)
            {
                CurrentStamina = currentStamina;
                CurrentEnergy = currentEnergy;
                StaminaRegenDelayRemaining = staminaRegenDelayRemaining;
                EnergyRegenDelayRemaining = energyRegenDelayRemaining;
            }
        }

        private void OnValidate()
        {
            _staminaRegenPerSecond = Mathf.Max(0f, _staminaRegenPerSecond);
            _energyRegenPerSecond = Mathf.Max(0f, _energyRegenPerSecond);
            _regenDelayAfterUse = Mathf.Max(0f, _regenDelayAfterUse);
            _sprintStaminaPerSecond = Mathf.Max(0f, _sprintStaminaPerSecond);
            _sprintRestartStaminaThreshold = Mathf.Max(0f, _sprintRestartStaminaThreshold);
            _jumpStaminaCost = Mathf.Max(0f, _jumpStaminaCost);
        }
    }
}
