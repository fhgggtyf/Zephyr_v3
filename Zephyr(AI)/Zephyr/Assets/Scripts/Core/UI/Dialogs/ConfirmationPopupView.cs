using UnityEngine;
using UnityEngine.UI;

namespace Zephyr.Core.UI.Dialogs
{
    /// <summary>
    /// Content-agnostic confirmation popup. Its owner supplies text and button behavior.
    /// </summary>
    public sealed class ConfirmationPopupView : MonoBehaviour
    {
        public Button ConfirmButton { get; private set; }
        public Button CancelButton { get; private set; }

        private Text _title;
        private Text _message;
        private Text _confirmLabel;
        private Text _cancelLabel;
        private bool _built;

        private void Awake() => BuildIfNeeded();

        public void Configure(string title, string message, string confirmLabel, string cancelLabel)
        {
            BuildIfNeeded();
            _title.text = title;
            _message.text = message;
            _confirmLabel.text = confirmLabel;
            _cancelLabel.text = cancelLabel;
        }

        private void BuildIfNeeded()
        {
            if (_built) return;
            _built = true;

            var root = (RectTransform)transform;
            RuntimeUIFactory.Stretch(root);

            Image backdrop = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, 0.82f);

            Image dialog = RuntimeUIFactory.CreateImage("Dialog", transform,
                new Color(0.08f, 0.09f, 0.09f, 1f));
            RuntimeUIFactory.Place((RectTransform)dialog.transform, new Vector2(0.5f, 0.5f),
                new Vector2(480f, 250f), Vector2.zero);

            _title = RuntimeUIFactory.CreateText("Title", dialog.transform, 24,
                TextAnchor.MiddleCenter, Color.white);
            RuntimeUIFactory.Place((RectTransform)_title.transform, new Vector2(0.5f, 1f),
                new Vector2(410f, 44f), new Vector2(0f, -40f));

            _message = RuntimeUIFactory.CreateText("Message", dialog.transform, 16,
                TextAnchor.MiddleCenter, new Color(0.82f, 0.84f, 0.83f, 1f));
            RuntimeUIFactory.Place((RectTransform)_message.transform, new Vector2(0.5f, 0.5f),
                new Vector2(400f, 78f), new Vector2(0f, 10f));

            ConfirmButton = RuntimeUIFactory.CreateButton("Confirm", dialog.transform,
                new Color(0.18f, 0.48f, 0.36f, 1f), Color.white, out _confirmLabel);
            RuntimeUIFactory.Place((RectTransform)ConfirmButton.transform, new Vector2(0.5f, 0f),
                new Vector2(170f, 42f), new Vector2(-94f, 36f));

            CancelButton = RuntimeUIFactory.CreateButton("Cancel", dialog.transform,
                new Color(0.25f, 0.26f, 0.26f, 1f), Color.white, out _cancelLabel);
            RuntimeUIFactory.Place((RectTransform)CancelButton.transform, new Vector2(0.5f, 0f),
                new Vector2(170f, 42f), new Vector2(94f, 36f));
        }
    }
}
