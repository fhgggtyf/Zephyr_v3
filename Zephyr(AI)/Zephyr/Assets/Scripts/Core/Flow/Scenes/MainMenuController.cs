using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zephyr.Core.Localization;
using Zephyr.Core.Save;
using Zephyr.Core.UI;
using Zephyr.Core.UI.Dialogs;

namespace Zephyr.Core.Flow.Scenes
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private ConfirmationPopupView _confirmationPopupPrefab;

        private readonly SaveSlotView[] _slotViews = new SaveSlotView[SaveSystem.MaxSaveSlots];

        private Canvas _canvas;
        private Button _startButton;
        private Button _continueButton;
        private Button _settingsButton;
        private Button _exitButton;
        private Text _startLabel;
        private Text _continueLabel;
        private Text _settingsLabel;
        private Text _exitLabel;

        private GameObject _savePanel;
        private Text _savePanelTitle;
        private Text _savePanelStatus;
        private Text _savePanelCloseLabel;
        private GameObject _settingsPanel;
        private Text _settingsTitle;
        private Text _languageLabel;
        private Text _englishLabel;
        private Text _chineseLabel;
        private Text _settingsCloseLabel;
        private Text _brandTitle;
        private Text _brandSubtitle;

        private ConfirmationPopupView _confirmationPopup;
        private Action _confirmationAction;
        private SlotPanelMode _slotMode;
        private bool _built;

        private void Start()
        {
            BuildInterface();
            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.OnLocaleChanged += HandleLocaleChanged;
            RefreshAllText();
            RefreshButtonStates();
        }

        private void OnDestroy()
        {
            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.OnLocaleChanged -= HandleLocaleChanged;
        }

        public void OnStartGamePressed()
        {
            OpenSavePanel(SlotPanelMode.NewGame);
        }

        public void OnContinueGamePressed()
        {
            if (SaveSystem.Instance == null || !SaveSystem.Instance.HasAnyValidSave) return;
            OpenSavePanel(SlotPanelMode.Continue);
        }

        public void OnSettingsPressed()
        {
            _savePanel.SetActive(false);
            _settingsPanel.SetActive(true);
        }

        public void OnExitGamePressed()
        {
            ShowConfirmation(
                L("menu.exit.confirm.title", "Exit Game"),
                L("menu.exit.confirm.message", "Are you sure you want to leave the game?"),
                L("common.yes", "Yes"),
                L("common.no", "No"),
                ExitApplication);
        }

        public void OnSavePanelClosePressed() => _savePanel.SetActive(false);

        public void OnSettingsClosePressed() => _settingsPanel.SetActive(false);

        public void OnEnglishPressed()
            => LocalizationManager.Instance?.SetLocale(LocaleId.en_US);

        public void OnChinesePressed()
            => LocalizationManager.Instance?.SetLocale(LocaleId.zh_CN);

        public void OnConfirmationAccepted()
        {
            Action action = _confirmationAction;
            CloseConfirmation();
            action?.Invoke();
        }

        public void OnConfirmationCancelled() => CloseConfirmation();

        // Kept for existing scene or prefab event bindings.
        public void ContinueProfile() => OnContinueGamePressed();

        private void BuildInterface()
        {
            if (_built) return;
            _built = true;

            EnsureCanvasAndEventSystem();

            Image background = RuntimeUIFactory.CreateImage("Background", _canvas.transform, Color.black);
            RuntimeUIFactory.Stretch((RectTransform)background.transform);
            background.transform.SetAsFirstSibling();

            RectTransform menu = RuntimeUIFactory.CreateRect("Menu", _canvas.transform);
            RuntimeUIFactory.Place(menu, new Vector2(0.5f, 0.5f), new Vector2(380f, 500f), Vector2.zero);

            _brandTitle = RuntimeUIFactory.CreateText("Title", menu, 52, TextAnchor.MiddleCenter, Color.white);
            _brandTitle.text = L("menu.brand.title", "ZEPHYR");
            RuntimeUIFactory.Place((RectTransform)_brandTitle.transform, new Vector2(0.5f, 1f),
                new Vector2(380f, 72f), new Vector2(0f, -50f));

            _brandSubtitle = RuntimeUIFactory.CreateText("Subtitle", menu, 14, TextAnchor.MiddleCenter,
                new Color(0.54f, 0.72f, 0.63f, 1f));
            _brandSubtitle.text = L("menu.brand.subtitle", "A 2D ACTION ROGUELITE");
            RuntimeUIFactory.Place((RectTransform)_brandSubtitle.transform, new Vector2(0.5f, 1f),
                new Vector2(380f, 28f), new Vector2(0f, -94f));

            _startButton = CreateMenuButton("StartGame", menu, 62f, out _startLabel);
            _continueButton = CreateMenuButton("ContinueGame", menu, 4f, out _continueLabel);
            _settingsButton = CreateMenuButton("Settings", menu, -54f, out _settingsLabel);
            _exitButton = CreateMenuButton("ExitGame", menu, -112f, out _exitLabel);

            _startButton.onClick.AddListener(OnStartGamePressed);
            _continueButton.onClick.AddListener(OnContinueGamePressed);
            _settingsButton.onClick.AddListener(OnSettingsPressed);
            _exitButton.onClick.AddListener(OnExitGamePressed);

            BuildSavePanel();
            BuildSettingsPanel();
            BuildConfirmationPopup();

            EventSystem.current?.SetSelectedGameObject(_startButton.gameObject);
        }

        private Button CreateMenuButton(string name, Transform parent, float y, out Text label)
        {
            Button button = RuntimeUIFactory.CreateButton(name, parent,
                new Color(0.12f, 0.13f, 0.13f, 1f), Color.white, out label);
            RuntimeUIFactory.Place((RectTransform)button.transform, new Vector2(0.5f, 0.5f),
                new Vector2(300f, 46f), new Vector2(0f, y));
            return button;
        }

        private void BuildSavePanel()
        {
            Image overlay = RuntimeUIFactory.CreateImage("SaveSlotOverlay", _canvas.transform,
                new Color(0f, 0f, 0f, 0.92f));
            RuntimeUIFactory.Stretch((RectTransform)overlay.transform);
            _savePanel = overlay.gameObject;

            Image panel = RuntimeUIFactory.CreateImage("SaveSlotPanel", overlay.transform,
                new Color(0.07f, 0.075f, 0.075f, 1f));
            RuntimeUIFactory.Place((RectTransform)panel.transform, new Vector2(0.5f, 0.5f),
                new Vector2(900f, 500f), Vector2.zero);

            _savePanelTitle = RuntimeUIFactory.CreateText("Title", panel.transform, 28,
                TextAnchor.MiddleCenter, Color.white);
            RuntimeUIFactory.Place((RectTransform)_savePanelTitle.transform, new Vector2(0.5f, 1f),
                new Vector2(780f, 40f), new Vector2(0f, -34f));

            _savePanelStatus = RuntimeUIFactory.CreateText("Status", panel.transform, 14,
                TextAnchor.MiddleCenter, new Color(0.72f, 0.76f, 0.74f, 1f));
            RuntimeUIFactory.Place((RectTransform)_savePanelStatus.transform, new Vector2(0.5f, 1f),
                new Vector2(780f, 34f), new Vector2(0f, -70f));

            for (int i = 0; i < _slotViews.Length; i++)
                _slotViews[i] = BuildSlot(panel.transform, i, -286f + i * 286f);

            Button close = RuntimeUIFactory.CreateButton("Close", panel.transform,
                new Color(0.22f, 0.23f, 0.23f, 1f), Color.white, out _savePanelCloseLabel);
            RuntimeUIFactory.Place((RectTransform)close.transform, new Vector2(0.5f, 0f),
                new Vector2(190f, 38f), new Vector2(0f, 26f));
            close.onClick.AddListener(OnSavePanelClosePressed);

            _savePanel.SetActive(false);
        }

        private SaveSlotView BuildSlot(Transform parent, int index, float x)
        {
            Image card = RuntimeUIFactory.CreateImage($"SaveSlot{index + 1}", parent,
                new Color(0.12f, 0.125f, 0.125f, 1f));
            RuntimeUIFactory.Place((RectTransform)card.transform, new Vector2(0.5f, 0.5f),
                new Vector2(260f, 300f), new Vector2(x, -8f));

            Text title = RuntimeUIFactory.CreateText("SlotTitle", card.transform, 20,
                TextAnchor.MiddleCenter, Color.white);
            RuntimeUIFactory.Place((RectTransform)title.transform, new Vector2(0.5f, 1f),
                new Vector2(224f, 38f), new Vector2(0f, -32f));

            Text details = RuntimeUIFactory.CreateText("Details", card.transform, 14,
                TextAnchor.MiddleCenter, new Color(0.74f, 0.77f, 0.75f, 1f));
            RuntimeUIFactory.Place((RectTransform)details.transform, new Vector2(0.5f, 0.5f),
                new Vector2(224f, 104f), new Vector2(0f, 22f));

            Button primary = RuntimeUIFactory.CreateButton("Primary", card.transform,
                new Color(0.18f, 0.48f, 0.36f, 1f), Color.white, out Text primaryLabel);
            RuntimeUIFactory.Place((RectTransform)primary.transform, new Vector2(0.5f, 0f),
                new Vector2(214f, 40f), new Vector2(0f, 62f));

            Button delete = RuntimeUIFactory.CreateButton("Delete", card.transform,
                new Color(0.42f, 0.16f, 0.16f, 1f), Color.white, out Text deleteLabel);
            RuntimeUIFactory.Place((RectTransform)delete.transform, new Vector2(0.5f, 0f),
                new Vector2(214f, 34f), new Vector2(0f, 20f));

            int capturedIndex = index;
            primary.onClick.AddListener(() => OnSlotPrimaryPressed(capturedIndex));
            delete.onClick.AddListener(() => OnSlotDeletePressed(capturedIndex));

            return new SaveSlotView(title, details, primary, primaryLabel, delete, deleteLabel);
        }

        private void BuildSettingsPanel()
        {
            Image overlay = RuntimeUIFactory.CreateImage("SettingsOverlay", _canvas.transform,
                new Color(0f, 0f, 0f, 0.92f));
            RuntimeUIFactory.Stretch((RectTransform)overlay.transform);
            _settingsPanel = overlay.gameObject;

            Image panel = RuntimeUIFactory.CreateImage("SettingsPanel", overlay.transform,
                new Color(0.08f, 0.085f, 0.085f, 1f));
            RuntimeUIFactory.Place((RectTransform)panel.transform, new Vector2(0.5f, 0.5f),
                new Vector2(520f, 320f), Vector2.zero);

            _settingsTitle = RuntimeUIFactory.CreateText("Title", panel.transform, 28,
                TextAnchor.MiddleCenter, Color.white);
            RuntimeUIFactory.Place((RectTransform)_settingsTitle.transform, new Vector2(0.5f, 1f),
                new Vector2(440f, 44f), new Vector2(0f, -36f));

            _languageLabel = RuntimeUIFactory.CreateText("LanguageLabel", panel.transform, 19,
                TextAnchor.MiddleCenter, new Color(0.76f, 0.79f, 0.77f, 1f));
            RuntimeUIFactory.Place((RectTransform)_languageLabel.transform, new Vector2(0.5f, 0.5f),
                new Vector2(420f, 36f), new Vector2(0f, 46f));

            Button english = RuntimeUIFactory.CreateButton("English", panel.transform,
                new Color(0.18f, 0.48f, 0.36f, 1f), Color.white, out _englishLabel);
            RuntimeUIFactory.Place((RectTransform)english.transform, new Vector2(0.5f, 0.5f),
                new Vector2(190f, 44f), new Vector2(-102f, -4f));
            english.onClick.AddListener(OnEnglishPressed);

            Button chinese = RuntimeUIFactory.CreateButton("Chinese", panel.transform,
                new Color(0.18f, 0.48f, 0.36f, 1f), Color.white, out _chineseLabel);
            RuntimeUIFactory.Place((RectTransform)chinese.transform, new Vector2(0.5f, 0.5f),
                new Vector2(190f, 44f), new Vector2(102f, -4f));
            chinese.onClick.AddListener(OnChinesePressed);

            Button close = RuntimeUIFactory.CreateButton("Close", panel.transform,
                new Color(0.22f, 0.23f, 0.23f, 1f), Color.white, out _settingsCloseLabel);
            RuntimeUIFactory.Place((RectTransform)close.transform, new Vector2(0.5f, 0f),
                new Vector2(190f, 40f), new Vector2(0f, 32f));
            close.onClick.AddListener(OnSettingsClosePressed);

            _settingsPanel.SetActive(false);
        }

        private void BuildConfirmationPopup()
        {
            if (_confirmationPopupPrefab != null)
            {
                _confirmationPopup = Instantiate(_confirmationPopupPrefab, _canvas.transform, false);
            }
            else
            {
                RectTransform fallback = RuntimeUIFactory.CreateRect("ConfirmationPopup", _canvas.transform);
                _confirmationPopup = fallback.gameObject.AddComponent<ConfirmationPopupView>();
            }

            _confirmationPopup.ConfirmButton.onClick.AddListener(OnConfirmationAccepted);
            _confirmationPopup.CancelButton.onClick.AddListener(OnConfirmationCancelled);
            _confirmationPopup.gameObject.SetActive(false);
        }

        private void EnsureCanvasAndEventSystem()
        {
            _canvas = GetComponentInChildren<Canvas>(true);
            if (_canvas == null)
            {
                RectTransform canvasRect = RuntimeUIFactory.CreateRect("MainMenuUI", transform);
                _canvas = canvasRect.gameObject.AddComponent<Canvas>();
            }

            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.worldCamera = null;
            _canvas.pixelPerfect = false;

            CanvasScaler scaler = _canvas.GetComponent<CanvasScaler>() ?? _canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960f, 540f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (_canvas.GetComponent<GraphicRaycaster>() == null)
                _canvas.gameObject.AddComponent<GraphicRaycaster>();

            if (FindAnyObjectByType<EventSystem>() != null) return;

            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            Type inputModuleType = Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModuleType != null) eventSystemObject.AddComponent(inputModuleType);
            else eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private void OpenSavePanel(SlotPanelMode mode)
        {
            _slotMode = mode;
            _settingsPanel.SetActive(false);
            _savePanel.SetActive(true);
            RefreshSavePanel();
        }

        private void OnSlotPrimaryPressed(int slotIndex)
        {
            SaveSystem save = SaveSystem.Instance;
            if (save == null) return;

            if (_slotMode == SlotPanelMode.Continue)
            {
                if (save.TrySelectSlot(slotIndex)) ContinueSelectedProfile();
                return;
            }

            if (save.TryCreateSlot(slotIndex)) StartSelectedProfile();
        }

        private void OnSlotDeletePressed(int slotIndex)
        {
            SaveSlotInfo info = SaveSystem.Instance?.GetSlotInfo(slotIndex) ?? SaveSlotInfo.Empty(slotIndex);
            if (!info.IsOccupied) return;

            ShowConfirmation(
                L("menu.save.delete.title", "Delete Save"),
                F("menu.save.delete.message", "Delete save in Slot {0}? This cannot be undone.", slotIndex + 1),
                L("common.delete", "Delete"),
                L("common.cancel", "Cancel"),
                () =>
                {
                    SaveSystem.Instance?.DeleteSlot(slotIndex);
                    RefreshButtonStates();
                    RefreshSavePanel();
                });
        }

        private void StartSelectedProfile()
        {
            _savePanel.SetActive(false);
            GameFlow.Instance?.RequestTransition(GameState.Tutorial);
        }

        private void ContinueSelectedProfile()
        {
            _savePanel.SetActive(false);
            SaveSystem save = SaveSystem.Instance;
            if (save?.Meta == null || GameFlow.Instance == null) return;

            GameState destination = !save.Meta.HasCompletedTutorial
                ? GameState.Tutorial
                : save.CurrentRun != null ? GameState.LoadingRun : GameState.MetaHub;
            GameFlow.Instance.RequestTransition(destination);
        }

        private void ShowConfirmation(string title, string message, string confirmLabel,
            string cancelLabel, Action confirmationAction)
        {
            _confirmationAction = confirmationAction;
            _confirmationPopup.Configure(title, message, confirmLabel, cancelLabel);
            _confirmationPopup.gameObject.SetActive(true);
            _confirmationPopup.transform.SetAsLastSibling();
            EventSystem.current?.SetSelectedGameObject(_confirmationPopup.CancelButton.gameObject);
        }

        private void CloseConfirmation()
        {
            _confirmationAction = null;
            _confirmationPopup.gameObject.SetActive(false);
        }

        private void RefreshButtonStates()
        {
            if (_continueButton != null)
                _continueButton.interactable = SaveSystem.Instance?.HasAnyValidSave ?? false;
        }

        private void RefreshSavePanel()
        {
            if (_savePanel == null) return;

            SaveSystem save = SaveSystem.Instance;
            SaveSlotInfo[] slots = save?.GetSlotInfos() ?? new SaveSlotInfo[SaveSystem.MaxSaveSlots];
            bool allOccupied = true;

            for (int i = 0; i < _slotViews.Length; i++)
            {
                SaveSlotInfo info = slots[i];
                SaveSlotView view = _slotViews[i];
                allOccupied &= info.IsOccupied;

                view.Title.text = F("menu.save.slot", "Slot {0}", i + 1);
                if (info.IsOccupied)
                {
                    string profile = info.ProfileId.Length > 8 ? info.ProfileId.Substring(0, 8) : info.ProfileId;
                    string progress = info.HasRun
                        ? L("menu.save.run.in_progress", "Run in progress")
                        : L("menu.save.run.none", "No active run");
                    view.Details.text = F("menu.save.details", "Profile {0}\n{1}\nLast played: {2}",
                        profile, progress, info.LastWriteTimeUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));
                }
                else view.Details.text = L("menu.save.empty", "Empty Slot");

                view.PrimaryLabel.text = _slotMode == SlotPanelMode.Continue
                    ? L("menu.continue", "Continue")
                    : L("menu.save.create", "Create Save");
                view.Primary.interactable = _slotMode == SlotPanelMode.Continue
                    ? info.IsOccupied
                    : !info.IsOccupied;
                view.DeleteLabel.text = L("common.delete", "Delete");
                view.DeleteLabel.color = Color.white;
                view.DeleteLabel.enabled = info.IsOccupied;
                view.DeleteLabel.resizeTextForBestFit = true;
                view.DeleteLabel.resizeTextMinSize = 10;
                view.Delete.gameObject.SetActive(info.IsOccupied);
            }

            _savePanelTitle.text = _slotMode == SlotPanelMode.Continue
                ? L("menu.save.choose", "Choose Save")
                : L("menu.save.new", "New Game");
            _savePanelStatus.text = _slotMode == SlotPanelMode.NewGame && allOccupied
                ? L("menu.save.full", "All save slots are full. Delete a save before creating a new one.")
                : L("menu.save.select", "Select a save slot.");
        }

        private void RefreshAllText()
        {
            if (!_built) return;
            _brandTitle.text = L("menu.brand.title", "ZEPHYR");
            _brandSubtitle.text = L("menu.brand.subtitle", "A 2D ACTION ROGUELITE");
            _startLabel.text = L("menu.start", "Start Game");
            _continueLabel.text = L("menu.continue", "Continue Game");
            _settingsLabel.text = L("menu.settings", "Settings");
            _exitLabel.text = L("menu.exit", "Exit Game");
            _savePanelCloseLabel.text = L("common.close", "Close");
            _settingsTitle.text = L("menu.settings", "Settings");
            _languageLabel.text = L("menu.settings.language", "Language");
            _englishLabel.text = L("language.english", "English");
            _chineseLabel.text = L("language.chinese", "Chinese");
            _settingsCloseLabel.text = L("common.close", "Close");
            RefreshSavePanel();
        }

        private void HandleLocaleChanged(LocaleId _) => RefreshAllText();

        private static string L(string key, string fallback)
        {
            LocalizationManager manager = LocalizationManager.Instance;
            if (manager == null) return fallback;
            string localized = manager.Get(key);
            return localized == key ? fallback : localized;
        }

        private static string F(string key, string fallback, params object[] args)
            => string.Format(L(key, fallback), args);

        private static void ExitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private enum SlotPanelMode
        {
            Continue,
            NewGame
        }

        private sealed class SaveSlotView
        {
            public readonly Text Title;
            public readonly Text Details;
            public readonly Button Primary;
            public readonly Text PrimaryLabel;
            public readonly Button Delete;
            public readonly Text DeleteLabel;

            public SaveSlotView(Text title, Text details, Button primary, Text primaryLabel,
                Button delete, Text deleteLabel)
            {
                Title = title;
                Details = details;
                Primary = primary;
                PrimaryLabel = primaryLabel;
                Delete = delete;
                DeleteLabel = deleteLabel;
            }
        }
    }
}
