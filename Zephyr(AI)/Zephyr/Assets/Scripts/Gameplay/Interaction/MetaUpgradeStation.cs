using UnityEngine;
using Zephyr.Core.Interfaces;
using Zephyr.Core.Meta;
using Zephyr.Core.Save;

namespace Zephyr.Gameplay.Interaction
{
    /// <summary>MetaHub station that opens the permanent Potential upgrade panel.</summary>
    public sealed class MetaUpgradeStation : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameObject _panelPrefab;
        [SerializeField] private MetaUpgradeSO[] _upgrades;
        private MetaUpgradePanel _panel;

        public MetaUpgradeSO[] Upgrades => _upgrades;

        public bool CanInteract() => isActiveAndEnabled && _panel == null && SaveSystem.Instance?.Meta != null;

        public void OnInteract()
        {
            if (!CanInteract() || _panelPrefab == null) return;
            GameObject instance = Instantiate(_panelPrefab);
            _panel = instance.GetComponent<MetaUpgradePanel>();
            if (_panel == null)
            {
                Destroy(instance);
                Debug.LogError("MetaUpgradeStation panel prefab is missing MetaUpgradePanel.", this);
                return;
            }
            _panel.Show(this);
        }

        public void ClosePanel()
        {
            if (_panel == null) return;
            MetaUpgradePanel panel = _panel;
            _panel = null;
            panel.CloseWithoutNotifyingOwner();
        }

        public void NotifyPanelClosed(MetaUpgradePanel panel)
        {
            if (_panel == panel) _panel = null;
        }
    }
}
