using UnityEngine;
using Zephyr.Core.Flow.Scenes;
using Zephyr.Core.Interfaces;
using Zephyr.Core.Save;

namespace Zephyr.Gameplay.Interaction
{
    /// <summary>Explicit MetaHub run-entry interaction replacing the old far-right trigger.</summary>
    public sealed class RunStartInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameObject _panelPrefab;
        private PotentialAllocationPanel _panel;

        public bool CanInteract() => isActiveAndEnabled && _panel == null && SaveSystem.Instance?.Meta != null;

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
            FindAnyObjectByType<MetaHubController>()?.StartRun();
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
