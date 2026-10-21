using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using Zephyr.Contracts.Interfaces;
using Zephyr.Core.Events;
using Zephyr.Core.Localization;
using Zephyr.Core.Meta;
using Zephyr.Core.ObjectPool;
using Zephyr.Core.Save;
using Zephyr.Core.Timer;
using Zephyr.Core.Flow.Scenes;

namespace Zephyr.Core.Flow
{
    [DefaultExecutionOrder(-200)]
    public sealed class GameManagerBootstrap : MonoBehaviour
    {
        public static bool IsReady { get; private set; }
        public static event Action Ready;

        private IGameManagerServices _services;
        private GameManagerServices _composition;
        private readonly HashSet<Scene> _configuredScenes = new HashSet<Scene>();

        private void Awake()
        {
            IGameFlowGateway gameFlow = GameFlow.Instance;
            ISaveDataGateway saveData = SaveSystem.Instance;
            ISaveSlotGateway saveSlots = saveData as ISaveSlotGateway;
            IEventBusGateway eventBus = EventBus.Instance;
            IStoryProgressionGateway storyProgression = StoryProgressionService.Instance;
            ILocalizationGateway localization = LocalizationManager.Instance;
            IDropSpawnGateway dropSpawn = PoolRegistry.Instance;
            ITimerGateway timer = TimerService.Instance;
            MonoBehaviour[] components = transform.root.GetComponentsInChildren<MonoBehaviour>(true);
            IPlayerSpawnGateway playerSpawn = null;
            IDialogueInputGateway dialogueInput = null;
            IWeaponReplacementGateway weaponReplacement = null;
            ISceneLoadingGateway sceneLoading = SceneLoader.Instance;
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] is IPlayerSpawnGateway spawnGateway)
                    playerSpawn = spawnGateway;
                if (components[i] is IDialogueInputGateway dialogueGateway)
                    dialogueInput = dialogueGateway;
                if (components[i] is IWeaponReplacementGateway replacementGateway)
                    weaponReplacement = replacementGateway;
            }

            _composition = new GameManagerServices(
                gameFlow,
                gameFlow as IGameFlowSceneGateway,
                playerSpawn,
                saveSlots,
                saveData,
                eventBus,
                dialogueInput,
                sceneLoading,
                storyProgression,
                localization,
                weaponReplacement,
                dropSpawn,
                timer,
                saveData?.CurrentRun?.Seed ?? 0,
                saveData?.CurrentRun?.CurrentRoomDepth ?? 0,
                new GameplayRuntimeContext(saveData, eventBus));
            _services = _composition;

            ConfigureLoadedScene(gameObject.scene, LoadSceneMode.Additive);
            ConfigureAlreadyLoadedScenes();
        }

        private void OnEnable() => SceneManager.sceneLoaded += ConfigureLoadedScene;

        private void OnDisable() => SceneManager.sceneLoaded -= ConfigureLoadedScene;

        private void Start()
        {
            if (_services == null || _services.GameFlow == null || _services.SaveData == null)
                Debug.LogError("GameManagerBootstrap: required Persistent services are missing.", this);

            IsReady = true;
            Ready?.Invoke();
        }

        private void OnDestroy()
        {
            IsReady = false;
            Ready = null;
        }

        private void ConfigureLoadedScene(Scene scene, LoadSceneMode _)
        {
            if (_services == null || !scene.IsValid() || !scene.isLoaded
                || !_configuredScenes.Add(scene)) return;

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
                ConfigureComponents(roots[i].GetComponentsInChildren<MonoBehaviour>(true));
        }

        private void ConfigureAlreadyLoadedScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
                ConfigureLoadedScene(SceneManager.GetSceneAt(i), LoadSceneMode.Additive);
        }

        private void ConfigureComponents(MonoBehaviour[] components)
        {
            for (int i = 0; i < components.Length; i++)
            {
                MonoBehaviour component = components[i];
                if (component is IGameManagerDependencyConsumer consumer)
                    consumer.Configure(_services);
                if (component is IRoomRuntimeDependencyConsumer roomConsumer)
                    roomConsumer.Configure(_composition);
                if (component is ITimerGatewayConsumer timerConsumer)
                    timerConsumer.Configure(_composition.Timer);
                if (component is IStoryProgressionContextConsumer storyConsumer)
                    storyConsumer.Configure(_services.StoryProgression, _services.EventBus);
                if (component is ITutorialCompletionGatewayConsumer tutorialConsumer)
                    tutorialConsumer.Configure(_services as ITutorialCompletionGateway);
                if (component is IPickupRuntimeContextConsumer pickupConsumer)
                    pickupConsumer.Configure(_composition.PickupContext);
                if (component is IPlayerTargetContextConsumer targetConsumer)
                    targetConsumer.Configure(_composition.PickupContext);
            }
        }

        private sealed class GameManagerServices : IGameManagerServices, IRoomRuntimeDependencies,
            ITutorialCompletionGateway
        {
            public GameManagerServices(
                IGameFlowGateway gameFlow,
                IGameFlowSceneGateway sceneFlow,
                IPlayerSpawnGateway playerSpawn,
                ISaveSlotGateway saveSlots,
                ISaveDataGateway saveData,
                IEventBusGateway eventBus,
                IDialogueInputGateway dialogueInput,
                ISceneLoadingGateway sceneLoading,
                IStoryProgressionGateway storyProgression,
                ILocalizationGateway localization,
                IWeaponReplacementGateway weaponReplacement,
                IDropSpawnGateway dropSpawn,
                ITimerGateway timer,
                int seed,
                int roomDepth,
                IPickupRuntimeContext pickupContext)
            {
                GameFlow = gameFlow;
                SceneFlow = sceneFlow;
                PlayerSpawn = playerSpawn;
                SaveSlots = saveSlots;
                SaveData = saveData;
                EventBus = eventBus;
                DialogueInput = dialogueInput;
                SceneLoading = sceneLoading;
                StoryProgression = storyProgression;
                Localization = localization;
                WeaponReplacement = weaponReplacement;
                DropSpawn = dropSpawn;
                Timer = timer;
                Seed = seed;
                RoomDepth = roomDepth;
                PickupContext = pickupContext;
            }

            public IGameFlowGateway GameFlow { get; }
            public IGameFlowSceneGateway SceneFlow { get; }
            public IPlayerSpawnGateway PlayerSpawn { get; }
            public ISaveSlotGateway SaveSlots { get; }
            public ISaveDataGateway SaveData { get; }
            public IEventBusGateway EventBus { get; }
            public IDialogueInputGateway DialogueInput { get; }
            public ISceneLoadingGateway SceneLoading { get; }
            public ILocalizationGateway Localization { get; }
            public IStoryProgressionGateway StoryProgression { get; }
            public IDropSpawnGateway DropSpawn { get; }
            public IWeaponReplacementGateway WeaponReplacement { get; }
            public ITimerGateway Timer { get; }
            public int Seed { get; }
            public int RoomDepth { get; }
            public IPickupRuntimeContext PickupContext { get; }

            public void MarkCompleted()
            {
                if (SaveData?.Meta == null)
                    SaveData?.LoadMeta();

                if (SaveData?.Meta == null) return;
                SaveData.Meta.HasCompletedTutorial = true;
                SaveData.SaveMeta();
            }
        }
    }
}
