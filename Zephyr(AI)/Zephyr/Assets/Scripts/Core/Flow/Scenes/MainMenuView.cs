using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zephyr.Core.UI.Dialogs;

namespace Zephyr.Core.Flow.Scenes
{
    [DisallowMultipleComponent]
    public sealed class MainMenuView : MonoBehaviour, IDisposable
    {
        public enum SlotPanelMode { Continue, NewGame }

        [Serializable]
        public sealed class SlotView
        {
            [SerializeField] public Text Title;
            [SerializeField] public Text Details;
            [SerializeField] public Text PrimaryLabel;
            [SerializeField] public Text DeleteLabel;
            [SerializeField] public Button Primary;
            [SerializeField] public Button Delete;
        }

        [Header("Main menu")]
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _exitButton;
        [SerializeField] private Text _title;
        [SerializeField] private Text _subtitle;
        [SerializeField] private Text _startLabel;
        [SerializeField] private Text _continueLabel;
        [SerializeField] private Text _settingsLabel;
        [SerializeField] private Text _exitLabel;

        [Header("Save panel")]
        [SerializeField] private GameObject _savePanel;
        [SerializeField] private Text _saveTitle;
        [SerializeField] private Text _saveStatus;
        [SerializeField] private Text _saveCloseLabel;
        [SerializeField] private Button _saveCloseButton;
        [SerializeField] private SlotView[] _slots = Array.Empty<SlotView>();

        [Header("Settings panel")]
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private Text _settingsTitle;
        [SerializeField] private Text _languageLabel;
        [SerializeField] private Text _englishLabel;
        [SerializeField] private Text _chineseLabel;
        [SerializeField] private Text _settingsCloseLabel;
        [SerializeField] private Button _englishButton;
        [SerializeField] private Button _chineseButton;
        [SerializeField] private Button _settingsCloseButton;

        [Header("Confirmation")]
        [SerializeField] private ConfirmationPopupView _confirmation;

        private Action _confirmationAction;
        private bool _bound;

        public event Action StartGamePressed;
        public event Action ContinueGamePressed;
        public event Action SettingsPressed;
        public event Action ExitGamePressed;
        public event Action SavePanelClosePressed;
        public event Action SettingsClosePressed;
        public event Action EnglishPressed;
        public event Action ChinesePressed;
        public event Action<int> SlotPrimaryPressed;
        public event Action<int> SlotDeletePressed;

        public void Initialize()
        {
            if (_bound) return;
            if (!HasRequiredBindings())
            {
                Debug.LogError("MainMenuView is missing serialized UI bindings.", this);
                return;
            }

            _bound = true;
            _startButton.onClick.AddListener(HandleStartGamePressed);
            _continueButton.onClick.AddListener(HandleContinueGamePressed);
            _settingsButton.onClick.AddListener(HandleSettingsPressed);
            _exitButton.onClick.AddListener(HandleExitGamePressed);
            _saveCloseButton.onClick.AddListener(HandleSavePanelClosePressed);
            _settingsCloseButton.onClick.AddListener(HandleSettingsClosePressed);
            _englishButton.onClick.AddListener(HandleEnglishPressed);
            _chineseButton.onClick.AddListener(HandleChinesePressed);

            for (int i = 0; i < _slots.Length; i++)
            {
                int slotIndex = i;
                if (_slots[i]?.Primary != null)
                    _slots[i].Primary.onClick.AddListener(() => SlotPrimaryPressed?.Invoke(slotIndex));
                if (_slots[i]?.Delete != null)
                    _slots[i].Delete.onClick.AddListener(() => SlotDeletePressed?.Invoke(slotIndex));
            }

            if (_confirmation != null)
            {
                _confirmation.ConfirmButton.onClick.AddListener(AcceptConfirmation);
                _confirmation.CancelButton.onClick.AddListener(CancelConfirmation);
                _confirmation.gameObject.SetActive(false);
            }

        }

        public void SetContinueInteractable(bool value)
        {
            if (_continueButton != null) _continueButton.interactable = value;
        }

        public void OpenSavePanel()
        {
            if (_settingsPanel != null) _settingsPanel.SetActive(false);
            if (_savePanel != null) _savePanel.SetActive(true);
        }

        public void CloseSavePanel()
        {
            if (_savePanel != null) _savePanel.SetActive(false);
        }

        public void OpenSettings()
        {
            if (_savePanel != null) _savePanel.SetActive(false);
            if (_settingsPanel != null) _settingsPanel.SetActive(true);
        }

        public void CloseSettings()
        {
            if (_settingsPanel != null) _settingsPanel.SetActive(false);
        }

        public void ShowConfirmation(string title, string message, string confirm,
            string cancel, Action action)
        {
            if (_confirmation == null) return;
            _confirmationAction = action;
            _confirmation.Configure(title, message, confirm, cancel);
            _confirmation.gameObject.SetActive(true);
            _confirmation.transform.SetAsLastSibling();
            EventSystem.current?.SetSelectedGameObject(_confirmation.CancelButton.gameObject);
        }

        public void AcceptConfirmation()
        {
            Action action = _confirmationAction;
            CloseConfirmation();
            action?.Invoke();
        }

        public void CancelConfirmation() => CloseConfirmation();

        public void Refresh(MainMenuPresentation presentation)
        {
            if (presentation == null) return;
            SetText(_title, presentation.Title);
            SetText(_subtitle, presentation.Subtitle);
            SetText(_startLabel, presentation.StartLabel);
            SetText(_continueLabel, presentation.ContinueLabel);
            SetText(_settingsLabel, presentation.SettingsLabel);
            SetText(_exitLabel, presentation.ExitLabel);
            SetText(_saveCloseLabel, presentation.SaveCloseLabel);
            SetText(_settingsTitle, presentation.SettingsTitle);
            SetText(_languageLabel, presentation.LanguageLabel);
            SetText(_englishLabel, presentation.EnglishLabel);
            SetText(_chineseLabel, presentation.ChineseLabel);
            SetText(_settingsCloseLabel, presentation.SettingsCloseLabel);

            for (int i = 0; i < _slots.Length; i++)
            {
                if (presentation.Slots == null || i >= presentation.Slots.Length || _slots[i] == null)
                    continue;

                MainMenuSlotPresentation slotText = presentation.Slots[i];
                SlotView slot = _slots[i];
                SetText(slot.Title, slotText.Title);
                SetText(slot.Details, slotText.Details);
                SetText(slot.PrimaryLabel, slotText.PrimaryLabel);
                SetText(slot.DeleteLabel, slotText.DeleteLabel);
                if (slot.Primary != null) slot.Primary.interactable = slotText.PrimaryInteractable;
                if (slot.Delete != null) slot.Delete.gameObject.SetActive(slotText.DeleteVisible);
            }

            SetText(_saveTitle, presentation.SaveTitle);
            SetText(_saveStatus, presentation.SaveStatus);
        }

        public void Dispose()
        {
            if (!_bound) return;
            _bound = false;
            _startButton.onClick.RemoveListener(HandleStartGamePressed);
            _continueButton.onClick.RemoveListener(HandleContinueGamePressed);
            _settingsButton.onClick.RemoveListener(HandleSettingsPressed);
            _exitButton.onClick.RemoveListener(HandleExitGamePressed);
            _saveCloseButton.onClick.RemoveListener(HandleSavePanelClosePressed);
            _settingsCloseButton.onClick.RemoveListener(HandleSettingsClosePressed);
            _englishButton.onClick.RemoveListener(HandleEnglishPressed);
            _chineseButton.onClick.RemoveListener(HandleChinesePressed);
            _confirmationAction = null;
            StartGamePressed = null;
            ContinueGamePressed = null;
            SettingsPressed = null;
            ExitGamePressed = null;
            SavePanelClosePressed = null;
            SettingsClosePressed = null;
            EnglishPressed = null;
            ChinesePressed = null;
            SlotPrimaryPressed = null;
            SlotDeletePressed = null;
        }

        private void CloseConfirmation()
        {
            _confirmationAction = null;
            if (_confirmation != null) _confirmation.gameObject.SetActive(false);
        }

        private bool HasRequiredBindings()
        {
            return _startButton != null && _continueButton != null && _settingsButton != null &&
                _exitButton != null && _saveCloseButton != null && _settingsCloseButton != null &&
                _englishButton != null && _chineseButton != null && _slots != null && _slots.Length == 3;
        }

        private static void SetText(Text target, string value)
        {
            if (target != null) target.text = value ?? string.Empty;
        }

        private void HandleStartGamePressed() => StartGamePressed?.Invoke();
        private void HandleContinueGamePressed() => ContinueGamePressed?.Invoke();
        private void HandleSettingsPressed() => SettingsPressed?.Invoke();
        private void HandleExitGamePressed() => ExitGamePressed?.Invoke();
        private void HandleSavePanelClosePressed() => SavePanelClosePressed?.Invoke();
        private void HandleSettingsClosePressed() => SettingsClosePressed?.Invoke();
        private void HandleEnglishPressed() => EnglishPressed?.Invoke();
        private void HandleChinesePressed() => ChinesePressed?.Invoke();
    }
}
