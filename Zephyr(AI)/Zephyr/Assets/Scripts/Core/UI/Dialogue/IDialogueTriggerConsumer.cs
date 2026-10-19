using Zephyr.Core.Meta;
using Zephyr.Core.Save;

namespace Zephyr.Core.UI.Dialogue
{
    public interface IDialogueTriggerConsumer
    {
        void Configure(ISaveDataGateway saveData, IStoryProgressionGateway storyProgression,
            IDialogueGateway dialogueGateway);
    }
}
