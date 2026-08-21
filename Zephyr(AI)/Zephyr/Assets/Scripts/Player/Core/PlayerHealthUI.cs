using UnityEngine;
using Zephyr.Core.Health;
using Zephyr.Core.Interfaces;
using Zephyr.Core;
using Zephyr.UI;

namespace Zephyr.Gameplay.Player.Core
{
    /// <summary>
    /// Bridges HealthComponent and PlayerResourceController to UI stat bars.
    /// Updates HP bar, stamina bar, and energy bar every frame.
    /// Also provides a stat panel reference for the full stats display.
    /// </summary>
    [RequireComponent(typeof(HealthComponent))]
    public class PlayerHealthUI : MonoBehaviour
    {
        [Header("Bars")]
        [SerializeField] private StatBarUI _hpBar;
        [SerializeField] private StatBarUI _staminaBar;
        [SerializeField] private StatBarUI _energyBar;

        [Header("Panel (optional)")]
        [SerializeField] private StatsPanelUI _statsPanel;

        private HealthComponent _health;
        private PlayerResourceController _resources;

        private void Awake()
        {
            _health = GetComponent<HealthComponent>();
            _resources = GetComponent<PlayerResourceController>();
        }

        private void LateUpdate()
        {
            UpdateBars();
        }

        private void UpdateBars()
        {
            if (_hpBar != null && _health != null)
            {
                _hpBar.UpdateBar(_health.CurrentHp, _health.MaxHp);
            }

            if (_staminaBar != null && _resources != null)
            {
                _staminaBar.UpdateBar(_resources.CurrentStamina, _resources.MaxStamina);
            }

            if (_energyBar != null && _resources != null)
            {
                _energyBar.UpdateBar(_resources.CurrentEnergy, _resources.MaxEnergy);
            }

            if (_statsPanel != null)
            {
                _statsPanel.Refresh();
            }
        }
    }
}
