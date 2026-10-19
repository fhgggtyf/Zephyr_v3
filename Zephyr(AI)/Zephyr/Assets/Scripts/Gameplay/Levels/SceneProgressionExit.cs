using UnityEngine;
using Zephyr.Contracts.Interfaces;

namespace Zephyr.Gameplay.Levels
{
    /// <summary>Generic trigger used by non-objective scenes to advance the authored loop.</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class SceneProgressionExit : MonoBehaviour, ISceneProgressionGatewayConsumer
    {
        [SerializeField] private MonoBehaviour _progressionGatewaySource;
        private ISceneProgressionGateway _progressionGateway;
        private bool _triggered;

        public void Configure(ISceneProgressionGateway gateway)
        {
            _progressionGateway = gateway;
        }

        private void Awake()
        {
            Collider2D trigger = GetComponent<Collider2D>();
            if (trigger != null) trigger.isTrigger = true;
        }

private void OnTriggerEnter2D(Collider2D other)
        {
            if (_triggered || !other.CompareTag("Player")) return;
            if (_progressionGateway == null && _progressionGatewaySource is ISceneProgressionGateway source)
                _progressionGateway = source;
            if (_progressionGateway == null) return;

            _triggered = _progressionGateway.AdvanceFromCurrentScene();
        }
    }
}
