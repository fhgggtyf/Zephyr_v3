namespace Zephyr.Core.Save
{
    public interface ISaveSlotGateway
    {
        const int MaxSaveSlots = 3;

        bool HasAnyValidSave { get; }
        SaveSlotInfo[] GetSlotInfos();
        SaveSlotInfo GetSlotInfo(int slotIndex);
        bool TrySelectSlot(int slotIndex);
        bool TryCreateSlot(int slotIndex);
        bool DeleteSlot(int slotIndex);
    }

    public interface ISaveDataGateway
    {
        MetaData Meta { get; }
        RunData CurrentRun { get; }
        int SelectedSlotIndex { get; }

        MetaData LoadMeta();
        void SaveMeta();
        RunData LoadRun();
        void SaveRun();
        void ClearRun();
        void StartNewRun(int seed);
    }

    public interface ISaveGateway : ISaveSlotGateway, ISaveDataGateway
    {
    }
}
