namespace Zephyr.Core.Flow.Scenes
{
    public interface ITutorialCompletionGateway
    {
        void MarkCompleted();
    }

    public interface ITutorialCompletionGatewayConsumer
    {
        void Configure(ITutorialCompletionGateway gateway);
    }
}
