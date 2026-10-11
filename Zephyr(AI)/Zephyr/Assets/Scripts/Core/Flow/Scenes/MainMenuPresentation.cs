using Zephyr.Core.Save;

namespace Zephyr.Core.Flow.Scenes
{
    public sealed class MainMenuPresentation
    {
        public string Title;
        public string Subtitle;
        public string StartLabel;
        public string ContinueLabel;
        public string SettingsLabel;
        public string ExitLabel;
        public string SaveTitle;
        public string SaveStatus;
        public string SaveCloseLabel;
        public string SettingsTitle;
        public string LanguageLabel;
        public string EnglishLabel;
        public string ChineseLabel;
        public string SettingsCloseLabel;
        public MainMenuSlotPresentation[] Slots;
    }

    public sealed class MainMenuSlotPresentation
    {
        public string Title;
        public string Details;
        public string PrimaryLabel;
        public string DeleteLabel;
        public bool PrimaryInteractable;
        public bool DeleteVisible;
    }

    public sealed class MainMenuTextPresenter
    {
        private readonly SettingsPresenter _settings;

        public MainMenuTextPresenter(SettingsPresenter settings)
        {
            _settings = settings;
        }

        public MainMenuPresentation Build(MainMenuView.SlotPanelMode mode, SaveSlotInfo[] infos)
        {
            int slotCount = ISaveSlotGateway.MaxSaveSlots;
            var presentation = new MainMenuPresentation
            {
                Title = T("menu.brand.title", "ZEPHYR"),
                Subtitle = T("menu.brand.subtitle", "A 2D ACTION ROGUELITE"),
                StartLabel = T("menu.start", "Start Game"),
                ContinueLabel = T("menu.continue", "Continue Game"),
                SettingsLabel = T("menu.settings", "Settings"),
                ExitLabel = T("menu.exit", "Exit Game"),
                SaveCloseLabel = T("common.close", "Close"),
                SettingsTitle = T("menu.settings", "Settings"),
                LanguageLabel = T("menu.settings.language", "Language"),
                EnglishLabel = T("language.english", "English"),
                ChineseLabel = T("language.chinese", "Chinese"),
                SettingsCloseLabel = T("common.close", "Close"),
                Slots = new MainMenuSlotPresentation[slotCount]
            };

            bool allOccupied = true;
            for (int i = 0; i < slotCount; i++)
            {
                SaveSlotInfo info = infos != null && i < infos.Length ? infos[i] : SaveSlotInfo.Empty(i);
                allOccupied &= info.IsOccupied;
                string profile = info.ProfileId ?? string.Empty;
                if (profile.Length > 8) profile = profile.Substring(0, 8);

                presentation.Slots[i] = new MainMenuSlotPresentation
                {
                    Title = F("menu.save.slot", "Slot {0}", i + 1),
                    Details = info.IsOccupied
                        ? F("menu.save.details", "Profile {0}\n{1}\nLast played: {2}", profile,
                            info.HasRun ? T("menu.save.run.in_progress", "Run in progress") : T("menu.save.run.none", "No active run"),
                            info.LastWriteTimeUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm"))
                        : T("menu.save.empty", "Empty Slot"),
                    PrimaryLabel = mode == MainMenuView.SlotPanelMode.Continue
                        ? T("menu.continue", "Continue")
                        : T("menu.save.create", "Create Save"),
                    DeleteLabel = T("common.delete", "Delete"),
                    PrimaryInteractable = mode == MainMenuView.SlotPanelMode.Continue ? info.IsOccupied : !info.IsOccupied,
                    DeleteVisible = info.IsOccupied
                };
            }

            presentation.SaveTitle = mode == MainMenuView.SlotPanelMode.Continue
                ? T("menu.save.choose", "Choose Save")
                : T("menu.save.new", "New Game");
            presentation.SaveStatus = mode == MainMenuView.SlotPanelMode.NewGame && allOccupied
                ? T("menu.save.full", "All save slots are full. Delete a save before creating a new one.")
                : T("menu.save.select", "Select a save slot.");
            return presentation;
        }

        private string T(string key, string fallback) => _settings?.Translate(key, fallback) ?? fallback;

        private string F(string key, string fallback, params object[] args)
            => _settings?.Format(key, fallback, args) ?? string.Format(fallback, args);
    }
}
