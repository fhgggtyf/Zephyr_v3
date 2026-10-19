using System;

namespace Zephyr.Contracts.Interfaces
{
    public interface ITimerGateway
    {
        IDisposable SetTimeout(float duration, Action callback);
        IDisposable SetInterval(float interval, Action callback);
    }

    public interface ITimerGatewayConsumer
    {
        void Configure(ITimerGateway timer);
    }
}
