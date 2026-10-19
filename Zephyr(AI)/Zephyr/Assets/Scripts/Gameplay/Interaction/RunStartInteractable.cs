using UnityEngine;
using Zephyr.Contracts.Interfaces;
using Zephyr.Core.Flow.Scenes;
using Zephyr.Core.Meta;

namespace Zephyr.Gameplay.Interaction
{
    /// <summary>Explicit MetaHub run-entry interaction replacing the old far-right trigger.</summary>
    public sealed class RunStartInteractable : MonoBehaviour, IInteractable, IMetaHubDependencyConsumer
    {
        [SerializeField] private GameObject _panelPrefab;
        [SerializeField] private MonoBehaviour _runEntryGatewaySource;
        private PotentialAllocationPanel _panel;
        private IMetaGateway _metaGateway;
        private IRunEntryGateway _runEntryGateway;

        public IMetaGateway MetaGateway => _metaGateway;

        private void Awake()
        {
            _runEntryGateway = _runEntryGatewaySource as IRunEntryGateway;
            if (_runEntryGatewaySource == null)
                Debug.LogError("RunStartInteractable requires an explicit IRunEntryGateway reference.", this);
            else if (_runEntryGateway == null)
                Debug.LogError("RunStartInteractable gateway source does not implement IRunEntryGateway.", this);
        }

        public void Configure(IMetaGateway metaGateway) => _metaGateway = metaGateway;

        public bool CanInteract() => isActiveAndEnabled && _panel == null && _metaGateway?.HasMeta == true;

        public void OnInteract()
        {
            if (!CanInteract() || _panelPrefab == null) return;
            GameObject instance = Instantiate(_panelPrefab);
            _panel = instance.GetComponent<PotentialAllocationPanel>();
            if (_panel == null)
            {
                Destroy(instance);
                Debug.LogError("RunStartInteractable panel prefab is missing PotentialAllocationPanel.", this);
                return;
            }
            _panel.Show(this);
        }

        public void BeginRun()
        {
            if (_panel != null)
            {
                PotentialAllocationPanel panel = _panel;
                _panel = null;
                panel.CloseWithoutDestroyingOwner();
            }
            _runEntryGateway?.StartRun();
        }

        public void ClosePanel()
        {
            if (_panel == null) return;
            PotentialAllocationPanel panel = _panel;
            _panel = null;
            panel.CloseWithoutDestroyingOwner();
        }
    }
}
