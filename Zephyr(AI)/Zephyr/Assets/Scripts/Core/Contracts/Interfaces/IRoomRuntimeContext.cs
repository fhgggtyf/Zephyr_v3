using System;

namespace Zephyr.Contracts.Interfaces
{
    public interface IRoomRuntimeDependencies
    {
        IPickupRuntimeContext PickupContext { get; }
        IEventBusGateway EventBus { get; }
        IDropSpawnGateway DropSpawn { get; }
        ITimerGateway Timer { get; }
        int Seed { get; }
        int RoomDepth { get; }
    }

    public interface IRoomRuntimeDependencyConsumer
    {
        void Configure(IRoomRuntimeDependencies dependencies);
    }
}
