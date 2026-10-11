using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zephyr.Core.UI;
using Zephyr.Core.UI.Dialogs;

namespace Zephyr.Core.Flow.Scenes
{
    public sealed class MainMenuView : IDisposable
    {
        public enum SlotPanelMode { Continue, NewGame }
        public event Action StartGamePressed, ContinueGamePressed, SettingsPressed, ExitGamePressed;
        public event Action SavePanelClosePressed, SettingsClosePressed, EnglishPressed, ChinesePressed;
        public event Action<int> SlotPrimaryPressed, SlotDeletePressed;
        private readonly SlotView[] _slots;
        private Canvas _canvas;
        private Button _continueButton;
        private Text _title, _subtitle, _start, _continue, _settings, _exit;
        private GameObject _savePanel, _settingsPanel;
        private Text _saveTitle, _saveStatus, _saveClose, _settingsTitle, _language, _english, _chinese, _settingsClose;
        private ConfirmationPopupView _confirmation;
        private Action _confirmationAction;
        public MainMenuView(int slotCount) { _slots = new SlotView[slotCount]; }

        public void Build(Transform owner, ConfirmationPopupView popupPrefab)
        {
            EnsureCanvas(owner);
            Image background = RuntimeUIFactory.CreateImage("Background", _canvas.transform, Color.black); RuntimeUIFactory.Stretch((RectTransform)background.transform);
            RectTransform menu = RuntimeUIFactory.CreateRect("Menu", _canvas.transform); RuntimeUIFactory.Place(menu, new Vector2(.5f, .5f), new Vector2(380, 500), Vector2.zero);
            _title = CreateText("Title", menu, 52, Color.white); Place(_title, new Vector2(.5f, 1), new Vector2(380, 72), new Vector2(0, -50));
            _subtitle = CreateText("Subtitle", menu, 14, new Color(.54f, .72f, .63f)); Place(_subtitle, new Vector2(.5f, 1), new Vector2(380, 28), new Vector2(0, -94));
            Button start = CreateMenuButton("StartGame", menu, 62, out _start); _continueButton = CreateMenuButton("ContinueGame", menu, 4, out _continue); Button settings = CreateMenuButton("Settings", menu, -54, out _settings); Button exit = CreateMenuButton("ExitGame", menu, -112, out _exit);
            start.onClick.AddListener(() => StartGamePressed?.Invoke()); _continueButton.onClick.AddListener(() => ContinueGamePressed?.Invoke()); settings.onClick.AddListener(() => SettingsPressed?.Invoke()); exit.onClick.AddListener(() => ExitGamePressed?.Invoke());
            BuildSavePanel(); BuildSettingsPanel(); BuildConfirmation(popupPrefab); EventSystem.current?.SetSelectedGameObject(start.gameObject);
        }
        public void SetContinueInteractable(bool value) => _continueButton.interactable = value;
        public void OpenSavePanel() { _settingsPanel.SetActive(false); _savePanel.SetActive(true); }
        public void CloseSavePanel() => _savePanel.SetActive(false);
        public void OpenSettings() { _savePanel.SetActive(false); _settingsPanel.SetActive(true); }
        public void CloseSettings() => _settingsPanel.SetActive(false);
        public void ShowConfirmation(string title, string message, string confirm, string cancel, Action action) { _confirmationAction = action; _confirmation.Configure(title, message, confirm, cancel); _confirmation.gameObject.SetActive(true); _confirmation.transform.SetAsLastSibling(); EventSystem.current?.SetSelectedGameObject(_confirmation.CancelButton.gameObject); }
        public void AcceptConfirmation() { Action action = _confirmationAction; CloseConfirmation(); action?.Invoke(); }
        public void CancelConfirmation() => CloseConfirmation();
        public void Dispose() { _confirmationAction = null; StartGamePressed = null; ContinueGamePressed = null; SettingsPressed = null; ExitGamePressed = null; SavePanelClosePressed = null; SettingsClosePressed = null; EnglishPressed = null; ChinesePressed = null; SlotPrimaryPressed = null; SlotDeletePressed = null; }
        private void CloseConfirmation() { _confirmationAction = null; _confirmation.gameObject.SetActive(false); }

        public void Refresh(MainMenuPresentation presentation)
        {
            if (presentation == null) return;
            _title.text = presentation.Title; _subtitle.text = presentation.Subtitle; _start.text = presentation.StartLabel; _continue.text = presentation.ContinueLabel; _settings.text = presentation.SettingsLabel; _exit.text = presentation.ExitLabel;
            _saveClose.text = presentation.SaveCloseLabel; _settingsTitle.text = presentation.SettingsTitle; _language.text = presentation.LanguageLabel; _english.text = presentation.EnglishLabel; _chinese.text = presentation.ChineseLabel; _settingsClose.text = presentation.SettingsCloseLabel;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (presentation.Slots == null || i >= presentation.Slots.Length) continue;
                MainMenuSlotPresentation slotText = presentation.Slots[i]; SlotView slot = _slots[i]; slot.Title.text = slotText.Title; slot.Details.text = slotText.Details; slot.PrimaryLabel.text = slotText.PrimaryLabel; slot.Primary.interactable = slotText.PrimaryInteractable; slot.DeleteLabel.text = slotText.DeleteLabel; slot.Delete.gameObject.SetActive(slotText.DeleteVisible);
            }
            _saveTitle.text = presentation.SaveTitle; _saveStatus.text = presentation.SaveStatus;
        }

        private Text CreateText(string name, Transform parent, int size, Color color) => RuntimeUIFactory.CreateText(name, parent, size, TextAnchor.MiddleCenter, color);
        private static void Place(Text text, Vector2 anchor, Vector2 size, Vector2 position) => RuntimeUIFactory.Place((RectTransform)text.transform, anchor, size, position);
        private Button CreateMenuButton(string name, Transform parent, float y, out Text label) { Button button = RuntimeUIFactory.CreateButton(name, parent, new Color(.12f, .13f, .13f), Color.white, out label); RuntimeUIFactory.Place((RectTransform)button.transform, new Vector2(.5f, .5f), new Vector2(300, 46), new Vector2(0, y)); return button; }

        private void BuildSavePanel()
        {
            Image overlay = RuntimeUIFactory.CreateImage("SaveSlotOverlay", _canvas.transform, new Color(0, 0, 0, .92f)); RuntimeUIFactory.Stretch((RectTransform)overlay.transform); _savePanel = overlay.gameObject;
            Image panel = RuntimeUIFactory.CreateImage("SaveSlotPanel", overlay.transform, new Color(.07f, .075f, .075f)); RuntimeUIFactory.Place((RectTransform)panel.transform, new Vector2(.5f, .5f), new Vector2(900, 500), Vector2.zero);
            _saveTitle = CreateText("Title", panel.transform, 28, Color.white); Place(_saveTitle, new Vector2(.5f, 1), new Vector2(780, 40), new Vector2(0, -34));
            _saveStatus = CreateText("Status", panel.transform, 14, new Color(.72f, .76f, .74f)); Place(_saveStatus, new Vector2(.5f, 1), new Vector2(780, 34), new Vector2(0, -70));
            for (int i = 0; i < _slots.Length; i++) _slots[i] = BuildSlot(panel.transform, i, -286 + i * 286);
            Button close = RuntimeUIFactory.CreateButton("Close", panel.transform, new Color(.22f, .23f, .23f), Color.white, out _saveClose); RuntimeUIFactory.Place((RectTransform)close.transform, new Vector2(.5f, 0), new Vector2(190, 38), new Vector2(0, 26)); close.onClick.AddListener(() => SavePanelClosePressed?.Invoke()); _savePanel.SetActive(false);
        }

        private SlotView BuildSlot(Transform parent, int index, float x)
        {
            Image card = RuntimeUIFactory.CreateImage("SaveSlot" + (index + 1), parent, new Color(.12f, .125f, .125f)); RuntimeUIFactory.Place((RectTransform)card.transform, new Vector2(.5f, .5f), new Vector2(260, 300), new Vector2(x, -8));
            Text title = CreateText("SlotTitle", card.transform, 20, Color.white); Place(title, new Vector2(.5f, 1), new Vector2(224, 38), new Vector2(0, -32));
            Text details = CreateText("Details", card.transform, 14, new Color(.74f, .77f, .75f)); Place(details, new Vector2(.5f, .5f), new Vector2(224, 104), new Vector2(0, 22));
            Button primary = RuntimeUIFactory.CreateButton("Primary", card.transform, new Color(.18f, .48f, .36f), Color.white, out Text primaryLabel); RuntimeUIFactory.Place((RectTransform)primary.transform, new Vector2(.5f, 0), new Vector2(214, 40), new Vector2(0, 62));
            Button delete = RuntimeUIFactory.CreateButton("Delete", card.transform, new Color(.42f, .16f, .16f), Color.white, out Text deleteLabel); RuntimeUIFactory.Place((RectTransform)delete.transform, new Vector2(.5f, 0), new Vector2(214, 34), new Vector2(0, 20));
            int captured = index; primary.onClick.AddListener(() => SlotPrimaryPressed?.Invoke(captured)); delete.onClick.AddListener(() => SlotDeletePressed?.Invoke(captured)); return new SlotView(title, details, primary, primaryLabel, delete, deleteLabel);
        }

        private void BuildSettingsPanel()
        {
            Image overlay = RuntimeUIFactory.CreateImage("SettingsOverlay", _canvas.transform, new Color(0, 0, 0, .92f)); RuntimeUIFactory.Stretch((RectTransform)overlay.transform); _settingsPanel = overlay.gameObject;
            Image panel = RuntimeUIFactory.CreateImage("SettingsPanel", overlay.transform, new Color(.08f, .085f, .085f)); RuntimeUIFactory.Place((RectTransform)panel.transform, new Vector2(.5f, .5f), new Vector2(520, 320), Vector2.zero);
            _settingsTitle = CreateText("Title", panel.transform, 28, Color.white); Place(_settingsTitle, new Vector2(.5f, 1), new Vector2(440, 44), new Vector2(0, -36));
            _language = CreateText("LanguageLabel", panel.transform, 19, new Color(.76f, .79f, .77f)); Place(_language, new Vector2(.5f, .5f), new Vector2(420, 36), new Vector2(0, 46));
            Button english = RuntimeUIFactory.CreateButton("English", panel.transform, new Color(.18f, .48f, .36f), Color.white, out _english); RuntimeUIFactory.Place((RectTransform)english.transform, new Vector2(.5f, .5f), new Vector2(190, 44), new Vector2(-102, -4)); english.onClick.AddListener(() => EnglishPressed?.Invoke());
            Button chinese = RuntimeUIFactory.CreateButton("Chinese", panel.transform, new Color(.18f, .48f, .36f), Color.white, out _chinese); RuntimeUIFactory.Place((RectTransform)chinese.transform, new Vector2(.5f, .5f), new Vector2(190, 44), new Vector2(102, -4)); chinese.onClick.AddListener(() => ChinesePressed?.Invoke());
            Button close = RuntimeUIFactory.CreateButton("Close", panel.transform, new Color(.22f, .23f, .23f), Color.white, out _settingsClose); RuntimeUIFactory.Place((RectTransform)close.transform, new Vector2(.5f, 0), new Vector2(190, 40), new Vector2(0, 32)); close.onClick.AddListener(() => SettingsClosePressed?.Invoke()); _settingsPanel.SetActive(false);
        }

        private sealed class SlotView { public readonly Text Title, Details, PrimaryLabel, DeleteLabel; public readonly Button Primary, Delete; public SlotView(Text title, Text details, Button primary, Text primaryLabel, Button delete, Text deleteLabel) { Title = title; Details = details; Primary = primary; PrimaryLabel = primaryLabel; Delete = delete; DeleteLabel = deleteLabel; } }

        private void BuildConfirmation(ConfirmationPopupView prefab)
        {
            _confirmation = prefab != null ? UnityEngine.Object.Instantiate(prefab, _canvas.transform, false) : RuntimeUIFactory.CreateRect("ConfirmationPopup", _canvas.transform).gameObject.AddComponent<ConfirmationPopupView>();
            _confirmation.ConfirmButton.onClick.AddListener(AcceptConfirmation); _confirmation.CancelButton.onClick.AddListener(CancelConfirmation); _confirmation.gameObject.SetActive(false);
        }

        private void EnsureCanvas(Transform owner)
        {
            _canvas = owner.GetComponentInChildren<Canvas>(true);
            if (_canvas == null) { RectTransform rect = RuntimeUIFactory.CreateRect("MainMenuUI", owner); _canvas = rect.gameObject.AddComponent<Canvas>(); }
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; _canvas.worldCamera = null; _canvas.pixelPerfect = false;
            CanvasScaler scaler = _canvas.GetComponent<CanvasScaler>() ?? _canvas.gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(960, 540); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; scaler.matchWidthOrHeight = .5f;
            if (_canvas.GetComponent<GraphicRaycaster>() == null) _canvas.gameObject.AddComponent<GraphicRaycaster>();
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null) return;
            GameObject es = new GameObject("EventSystem", typeof(EventSystem)); Type input = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem"); if (input != null) es.AddComponent(input); else es.AddComponent<StandaloneInputModule>();
        }
    }
}
