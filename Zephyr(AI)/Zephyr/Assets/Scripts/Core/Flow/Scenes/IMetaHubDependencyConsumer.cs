using Zephyr.Core.Meta;

namespace Zephyr.Core.Flow.Scenes
{
    public interface IMetaHubDependencyConsumer
    {
        void Configure(IMetaGateway metaGateway);
    }
}
