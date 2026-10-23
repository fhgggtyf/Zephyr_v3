using Zephyr.Core.Localization;
using Zephyr.Contracts.Interfaces;
using Zephyr.Core.Meta;
using Zephyr.Core.Save;

namespace Zephyr.Core.Flow
{
    public interface IGameManagerDependencyConsumer
    {
        void Configure(IGameManagerServices services);
    }

    public interface IGameManagerServices
    {
        IGameFlowGateway GameFlow { get; }
        IGameFlowSceneGateway SceneFlow { get; }
        IPlayerSpawnGateway PlayerSpawn { get; }
        ISaveSlotGateway SaveSlots { get; }
        ISaveDataGateway SaveData { get; }
        IEventBusGateway EventBus { get; }
        IDialogueInputGateway DialogueInput { get; }
        ISceneLoadingGateway SceneLoading { get; }
        ILocalizationGateway Localization { get; }
        IWeaponReplacementGateway WeaponReplacement { get; }
        IDropSpawnGateway DropSpawn { get; }
        IStoryProgressionGateway StoryProgression { get; }
        ICombatFeedbackGateway CombatFeedback { get; }
        ICameraShakeGateway CameraShake { get; }
    }
}
