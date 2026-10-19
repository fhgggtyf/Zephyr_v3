using Zephyr.Core.Save;
using Zephyr.Contracts.Flow;

namespace Zephyr.Core.Flow.Scenes
{
    public sealed class MainMenuUseCase
    {
        private readonly ISaveSlotGateway _saveSlots;
        private readonly ISaveDataGateway _saveData;

        public MainMenuUseCase(ISaveSlotGateway saveSlots, ISaveDataGateway saveData)
        {
            _saveSlots = saveSlots;
            _saveData = saveData;
        }

        public bool HasAnyValidSave => _saveSlots != null && _saveSlots.HasAnyValidSave;

        public bool TryCreateSlot(int slotIndex) => _saveSlots != null && _saveSlots.TryCreateSlot(slotIndex);

        public bool TrySelectSlot(int slotIndex) => _saveSlots != null && _saveSlots.TrySelectSlot(slotIndex);

        public bool DeleteSlot(int slotIndex) => _saveSlots != null && _saveSlots.DeleteSlot(slotIndex);

        public SaveSlotInfo[] GetSlotInfos() => _saveSlots?.GetSlotInfos();

        public SaveSlotInfo GetSlotInfo(int slotIndex)
            => _saveSlots != null ? _saveSlots.GetSlotInfo(slotIndex) : SaveSlotInfo.Empty(slotIndex);

        public GameState ResolveContinueDestination()
        {
            if (_saveData?.Meta == null) return GameState.MainMenu;
            if (!_saveData.Meta.HasCompletedTutorial) return GameState.Tutorial;
            return _saveData.CurrentRun != null ? GameState.LoadingRun : GameState.MetaHub;
        }
    }
}
