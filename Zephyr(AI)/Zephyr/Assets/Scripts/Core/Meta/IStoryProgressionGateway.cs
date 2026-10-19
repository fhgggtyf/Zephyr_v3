using Zephyr.Contracts.Interfaces;

namespace Zephyr.Core.Meta
{
    public interface IStoryProgressionGateway
    {
        bool GetBool(StoryFactSO fact);
        bool GetBool(string factId);
        int GetInt(StoryFactSO fact);
        int GetInt(string factId);
        bool IsCompleted(MilestoneSO milestone);
        bool IsCompleted(string milestoneId);
        bool Complete(MilestoneSO milestone);
        bool Complete(string milestoneId, MilestoneSO definition = null);
    }

    public interface IStoryProgressionContextConsumer
    {
        void Configure(IStoryProgressionGateway storyProgression, IEventBusGateway eventBus);
    }
}
