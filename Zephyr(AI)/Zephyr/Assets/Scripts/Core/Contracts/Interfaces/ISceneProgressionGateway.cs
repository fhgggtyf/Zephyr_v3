namespace Zephyr.Contracts.Interfaces
{
    public interface ISceneProgressionGateway
    {
        bool AdvanceFromCurrentScene();
        bool IsFinalTutorialScene();
    }

    public interface ISceneProgressionGatewayConsumer
    {
        void Configure(ISceneProgressionGateway gateway);
    }
}
