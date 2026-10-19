using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zephyr.Contracts.Interfaces;
using Zephyr.Core.Meta;
using Zephyr.Core.ObjectPool;
using Zephyr.Core.Save;
using Zephyr.Core.Events;
using Zephyr.Core.Timer;
using Zephyr.Core.Flow.Scenes;
using Zephyr.Core.Localization;

namespace Zephyr.Core.Flow
{
    [DefaultExecutionOrder(-100)]
    public sealed class PersistentBootstrap : MonoBehaviour
    {
        public static bool IsReady { get; private set; }
        public static event Action Ready;
        private IMetaGateway _metaGateway;
        private IPickupRuntimeContext _pickupRuntimeContext;
        private PoolRegistry _poolRegistry;
        private IPersistentServices _services;

        private void Awake()
        {
            ISaveDataGateway saveData = SaveSystem.Instance;
            IEventBusGateway eventBus = EventBus.Instance;
            _metaGateway = new MetaGateway(saveData);
            _pickupRuntimeContext = new GameplayRuntimeContext(saveData, eventBus);
            _poolRegistry = PoolRegistry.Instance;
            _poolRegistry?.ConfigurePickupContext(_pickupRuntimeContext);
            _services = new PersistentServices(GameFlow.Instance, saveData as ISaveSlotGateway, saveData,
                LocalizationManager.Instance);
        }

        private void OnEnable() => SceneManager.sceneLoaded += ConfigureLoadedScene;

        private void OnDisable() => SceneManager.sceneLoaded -= ConfigureLoadedScene;

        private void Start()
        {
            IEventBusGateway eventBus = EventBus.Instance;
            StoryProgressionService.Instance?.Configure(_metaGateway, eventBus);
            _poolRegistry?.ConfigurePickupContext(_pickupRuntimeContext);
            _poolRegistry?.ConfigureTimer(TimerService.Instance);
            IsReady = true;
            ConfigureAllLoadedScenes();
            Ready?.Invoke();
        }

        private void OnDestroy()
        {
            IsReady = false;
            Ready = null;
        }

        private void ConfigureLoadedScene(Scene scene, LoadSceneMode _)
        {
            if (!IsReady || !scene.IsValid() || !scene.isLoaded) return;
            ConfigureComponents(scene.GetRootGameObjects());
        }

        private void ConfigureAllLoadedScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
                ConfigureLoadedScene(SceneManager.GetSceneAt(i), LoadSceneMode.Additive);
        }

        private void ConfigureComponents(GameObject[] roots)
        {
            for (int i = 0; i < roots.Length; i++)
            {
                MonoBehaviour[] components = roots[i].GetComponentsInChildren<MonoBehaviour>(true);
                for (int j = 0; j < components.Length; j++)
                    if (components[j] is IPersistentDependencyConsumer consumer)
                        consumer.Configure(_services);
            }
        }

        private sealed class PersistentServices : IPersistentServices
        {
            public PersistentServices(IGameFlowGateway gameFlow, ISaveSlotGateway saveSlots,
                ISaveDataGateway saveData, ILocalizationGateway localization)
            {
                GameFlow = gameFlow;
                SaveSlots = saveSlots;
                SaveData = saveData;
                Localization = localization;
            }

            public IGameFlowGateway GameFlow { get; }
            public ISaveSlotGateway SaveSlots { get; }
            public ISaveDataGateway SaveData { get; }
            public ILocalizationGateway Localization { get; }
        }
    }
}
