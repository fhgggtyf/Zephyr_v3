using UnityEngine;
using Zephyr.Contracts.Flow;
using Zephyr.Contracts.Interfaces;
using Zephyr.Core.Meta;
using Zephyr.Core.Save;

namespace Zephyr.Core.Flow.Scenes
{
public sealed class MetaHubController : MonoBehaviour, IRunEntryGateway, IGameManagerDependencyConsumer
    {
        [SerializeField] private MonoBehaviour[] _dependencyConsumers;

        private IMetaGateway _metaGateway;
        private IGameFlowGateway _gameFlow;

        public void Configure(IGameManagerServices services)
        {
            _gameFlow = services?.GameFlow;
            if (services?.SaveData == null) return;
            _metaGateway = new MetaGateway(services.SaveData);
            ConfigureConsumers();
        }

        private void Awake() => ConfigureConsumers();

        private void ConfigureConsumers()
        {
            if (_metaGateway == null || _dependencyConsumers == null) return;

            for (int i = 0; i < _dependencyConsumers.Length; i++)
            {
                MonoBehaviour source = _dependencyConsumers[i];
                if (source == null) continue;

                if (source is IMetaHubDependencyConsumer consumer)
                    consumer.Configure(_metaGateway);
                else
                    Debug.LogError($"MetaHubController dependency source '{source.name}' does not implement IMetaHubDependencyConsumer.", source);
            }
        }

        public void StartRun() => _gameFlow?.RequestTransition(GameState.LoadingRun);
    }
}
