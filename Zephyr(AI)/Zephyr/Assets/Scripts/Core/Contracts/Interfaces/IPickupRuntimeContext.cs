namespace Zephyr.Contracts.Interfaces
{
    public interface IPickupRuntimeContext : IPlayerTargetContext
    {
        bool TryAddRunCurrency(int amount);
        bool TryRecordAcquiredWeapon(string weaponId);
    }

    public interface IPickupRuntimeContextConsumer
    {
        void Configure(IPickupRuntimeContext context);
    }
}
