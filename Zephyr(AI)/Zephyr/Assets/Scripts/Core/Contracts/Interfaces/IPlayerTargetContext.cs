using System;
using Zephyr.Contracts.Events;

namespace Zephyr.Contracts.Interfaces
{
    public interface IPlayerTargetContext
    {
        void SubscribePlayerTarget(Action<PlayerTargetChangedEvent> handler);
        void UnsubscribePlayerTarget(Action<PlayerTargetChangedEvent> handler);
        bool TryGetLatestPlayerTarget(out PlayerTargetChangedEvent change);
    }

    public interface IPlayerTargetContextConsumer
    {
        void Configure(IPlayerTargetContext context);
    }
}
