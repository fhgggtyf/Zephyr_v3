using System;
using UnityEngine;
using Zephyr.Core.Localization;
using Zephyr.Core.Save;
using Zephyr.Core.Flow;
using Zephyr.Core.Interfaces;
using Zephyr.Core.UI.Dialogs;

namespace Zephyr.Core.Flow.Scenes
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private ConfirmationPopupView _confirmationPopupPrefab;
        private MainMenuUseCase _useCase;
        private MainMenuView _view;
        private MainMenuTextPresenter _textPresenter;
        private SettingsPresenter _settingsPresenter;
        private ISaveSlotGateway _saveSlots;
        private ISaveDataGateway _saveData;
        private IGameFlowGateway _gameFlow;
        private MainMenuView.SlotPanelMode _slotMode;

        private void Start()
        {
            SaveSystem save = SaveSystem.Instance;
            _saveSlots = save;
            _saveData = save;
            _gameFlow = GameFlow.Instance;
            _useCase = new MainMenuUseCase(_saveSlots, _saveData);
            _settingsPresenter = new SettingsPresenter(LocalizationManager.Instance);
            _textPresenter = new MainMenuTextPresenter(_settingsPresenter);
            _view = new MainMenuView(ISaveSlotGateway.MaxSaveSlots);
            _view.Build(transform, _confirmationPopupPrefab);
            SubscribeView();
            _settingsPresenter.LocaleChanged += HandleLocaleChanged;
            RefreshAll();
        }

        private void OnDestroy()
        {
            if (_settingsPresenter != null) { _settingsPresenter.LocaleChanged -= HandleLocaleChanged; _settingsPresenter.Dispose(); }
            UnsubscribeView(); _view?.Dispose();
        }

        public void OnStartGamePressed() => OpenSavePanel(MainMenuView.SlotPanelMode.NewGame);
        public void OnContinueGamePressed() { if (_useCase != null && _useCase.HasAnyValidSave) OpenSavePanel(MainMenuView.SlotPanelMode.Continue); }
        public void OnSettingsPressed() => _view?.OpenSettings();
        public void OnExitGamePressed() => ShowConfirmation(T("menu.exit.confirm.title", "Exit Game"), T("menu.exit.confirm.message", "Are you sure you want to leave the game?"), T("common.yes", "Yes"), T("common.no", "No"), ExitApplication);
        public void OnSavePanelClosePressed() => _view?.CloseSavePanel();
        public void OnSettingsClosePressed() => _view?.CloseSettings();
        public void OnEnglishPressed() => _settingsPresenter?.SelectLocale(LocaleId.en_US);
        public void OnChinesePressed() => _settingsPresenter?.SelectLocale(LocaleId.zh_CN);
        public void OnConfirmationAccepted() => _view?.AcceptConfirmation();
        public void OnConfirmationCancelled() => _view?.CancelConfirmation();
        public void ContinueProfile() => OnContinueGamePressed();

        private void SubscribeView()
        {
            _view.StartGamePressed += OnStartGamePressed; _view.ContinueGamePressed += OnContinueGamePressed; _view.SettingsPressed += OnSettingsPressed; _view.ExitGamePressed += OnExitGamePressed;
            _view.SavePanelClosePressed += OnSavePanelClosePressed; _view.SettingsClosePressed += OnSettingsClosePressed; _view.EnglishPressed += OnEnglishPressed; _view.ChinesePressed += OnChinesePressed;
            _view.SlotPrimaryPressed += OnSlotPrimaryPressed; _view.SlotDeletePressed += OnSlotDeletePressed;
        }
        private void UnsubscribeView()
        {
            if (_view == null) return;
            _view.StartGamePressed -= OnStartGamePressed; _view.ContinueGamePressed -= OnContinueGamePressed; _view.SettingsPressed -= OnSettingsPressed; _view.ExitGamePressed -= OnExitGamePressed;
            _view.SavePanelClosePressed -= OnSavePanelClosePressed; _view.SettingsClosePressed -= OnSettingsClosePressed; _view.EnglishPressed -= OnEnglishPressed; _view.ChinesePressed -= OnChinesePressed;
            _view.SlotPrimaryPressed -= OnSlotPrimaryPressed; _view.SlotDeletePressed -= OnSlotDeletePressed;
        }

        private void OpenSavePanel(MainMenuView.SlotPanelMode mode) { _slotMode = mode; _view?.OpenSavePanel(); RefreshAll(); }
        private void OnSlotPrimaryPressed(int slotIndex)
        {
            if (_useCase == null) return;
            if (_slotMode == MainMenuView.SlotPanelMode.Continue) { if (_useCase.TrySelectSlot(slotIndex)) ContinueSelectedProfile(); return; }
            if (_useCase.TryCreateSlot(slotIndex)) StartSelectedProfile();
        }
        private void OnSlotDeletePressed(int slotIndex)
        {
            SaveSlotInfo info = _useCase?.GetSlotInfo(slotIndex) ?? SaveSlotInfo.Empty(slotIndex); if (!info.IsOccupied) return;
            ShowConfirmation(T("menu.save.delete.title", "Delete Save"), F("menu.save.delete.message", "Delete save in Slot {0}? This cannot be undone.", slotIndex + 1), T("common.delete", "Delete"), T("common.cancel", "Cancel"), () => { _useCase.DeleteSlot(slotIndex); RefreshAll(); });
        }
        private void StartSelectedProfile() { _view?.CloseSavePanel(); _gameFlow?.RequestTransition(GameState.Tutorial); }
        private void ContinueSelectedProfile() { _view?.CloseSavePanel(); if (_useCase != null && _gameFlow != null) _gameFlow.RequestTransition(_useCase.ResolveContinueDestination()); }
        private void ShowConfirmation(string title, string message, string confirm, string cancel, Action action) => _view?.ShowConfirmation(title, message, confirm, cancel, action);
        private void RefreshAll() { if (_view == null || _textPresenter == null) return; _view.SetContinueInteractable(_useCase?.HasAnyValidSave ?? false); _view.Refresh(_textPresenter.Build(_slotMode, _useCase?.GetSlotInfos())); }
        private void HandleLocaleChanged(LocaleId _) => RefreshAll();
        private string T(string key, string fallback) => _settingsPresenter?.Translate(key, fallback) ?? fallback;
        private string F(string key, string fallback, params object[] args) => _settingsPresenter?.Format(key, fallback, args) ?? string.Format(fallback, args);
        private static void ExitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
