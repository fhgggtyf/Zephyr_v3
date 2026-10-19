using UnityEngine;
using Zephyr.Contracts.Interfaces;
using Zephyr.Core.Flow.Scenes;
using Zephyr.Core.Meta;

namespace Zephyr.Gameplay.Interaction
{
    /// <summary>MetaHub station that opens the permanent Potential upgrade panel.</summary>
    public sealed class MetaUpgradeStation : MonoBehaviour, IInteractable, IMetaHubDependencyConsumer
    {
        [SerializeField] private GameObject _panelPrefab;
        [SerializeField] private MetaUpgradeSO[] _upgrades;
        private MetaUpgradePanel _panel;
        private IMetaGateway _metaGateway;

        public MetaUpgradeSO[] Upgrades => _upgrades;
        public IMetaGateway MetaGateway => _metaGateway;

        public void Configure(IMetaGateway metaGateway) => _metaGateway = metaGateway;

        public bool CanInteract() => isActiveAndEnabled && _panel == null && _metaGateway?.HasMeta == true;

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
