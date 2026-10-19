using System;

namespace Zephyr.Contracts.Interfaces
{
    public interface IEventBusGateway
    {
        void Subscribe<T>(Action<T> handler);
        void Unsubscribe<T>(Action<T> handler);
        void Publish<T>(T payload);
        bool TryGetLatest<T>(out T payload);
    }
}
