using NUnit.Framework;
using Zephyr.Contracts.Flow;
using Zephyr.Core.Flow;
using Zephyr.Core.Flow.Scenes;
using Zephyr.Core.Save;

namespace Zephyr.Tests.EditMode
{
    public sealed class MainMenuUseCaseTests
    {
        [Test]
        public void ContinueGoesToTutorialBeforeTutorialCompletion()
        {
            var gateway = new FakeGateway { Meta = new MetaData { HasCompletedTutorial = false } };
            Assert.That(new MainMenuUseCase(gateway, gateway).ResolveContinueDestination(), Is.EqualTo(GameState.Tutorial));
        }

        [Test]
        public void ContinueResumesRunAfterTutorialCompletion()
        {
            var gateway = new FakeGateway { Meta = new MetaData { HasCompletedTutorial = true }, CurrentRun = new RunData() };
            Assert.That(new MainMenuUseCase(gateway, gateway).ResolveContinueDestination(), Is.EqualTo(GameState.LoadingRun));
        }

        [Test]
        public void MissingGatewayReturnsMainMenu()
        {
            Assert.That(new MainMenuUseCase(null, null).ResolveContinueDestination(), Is.EqualTo(GameState.MainMenu));
        }

        private sealed class FakeGateway : ISaveSlotGateway, ISaveDataGateway
        {
            public MetaData Meta { get; set; }
            public RunData CurrentRun { get; set; }
            public int SelectedSlotIndex { get; private set; } = -1;
            public bool HasAnyValidSave => Meta != null;
            public SaveSlotInfo[] GetSlotInfos() => new SaveSlotInfo[ISaveSlotGateway.MaxSaveSlots];
            public SaveSlotInfo GetSlotInfo(int slotIndex) => SaveSlotInfo.Empty(slotIndex);
            public bool TrySelectSlot(int slotIndex) { SelectedSlotIndex = slotIndex; return true; }
            public bool TryCreateSlot(int slotIndex) { SelectedSlotIndex = slotIndex; return true; }
            public bool DeleteSlot(int slotIndex) => true;
            public MetaData LoadMeta() => Meta;
            public void SaveMeta() { }
            public RunData LoadRun() => CurrentRun;
            public void SaveRun() { }
            public void ClearRun() { CurrentRun = null; }
            public void StartNewRun(int seed) { CurrentRun = new RunData { Seed = seed }; }
        }
    }
}
