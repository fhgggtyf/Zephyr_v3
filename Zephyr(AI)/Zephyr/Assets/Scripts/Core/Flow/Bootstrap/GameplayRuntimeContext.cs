using System;
using Zephyr.Contracts.Events;
using Zephyr.Contracts.Interfaces;
using Zephyr.Core.Save;

namespace Zephyr.Core.Flow
{
    public sealed class GameplayRuntimeContext : IPickupRuntimeContext
    {
        private readonly ISaveDataGateway _saveData;
        private readonly IEventBusGateway _eventBus;

        public GameplayRuntimeContext(ISaveDataGateway saveData, IEventBusGateway eventBus)
        {
            _saveData = saveData;
            _eventBus = eventBus;
        }

        public void SubscribePlayerTarget(Action<PlayerTargetChangedEvent> handler)
            => _eventBus?.Subscribe(handler);

        public void UnsubscribePlayerTarget(Action<PlayerTargetChangedEvent> handler)
            => _eventBus?.Unsubscribe(handler);

        public bool TryGetLatestPlayerTarget(out PlayerTargetChangedEvent change)
        {
            if (_eventBus != null)
                return _eventBus.TryGetLatest(out change);

            change = default;
            return false;
        }

        public bool TryAddRunCurrency(int amount)
        {
            if (_saveData?.CurrentRun == null || amount < 0) return false;
            _saveData.CurrentRun.RunCurrency += amount;
            return true;
        }

        public bool TryRecordAcquiredWeapon(string weaponId)
        {
            if (_saveData?.CurrentRun == null || !int.TryParse(weaponId, out int id)) return false;
            return _saveData.CurrentRun.AcquiredWeaponIds.Add(id);
        }
    }
}
