using Zephyr.Core.Localization;
using Zephyr.Contracts.Interfaces;
using Zephyr.Core.Save;

namespace Zephyr.Core.Flow
{
    public interface IPersistentDependencyConsumer
    {
        void Configure(IPersistentServices services);
    }

    public interface IPersistentServices
    {
        IGameFlowGateway GameFlow { get; }
        ISaveSlotGateway SaveSlots { get; }
        ISaveDataGateway SaveData { get; }
        ILocalizationGateway Localization { get; }
    }
}
