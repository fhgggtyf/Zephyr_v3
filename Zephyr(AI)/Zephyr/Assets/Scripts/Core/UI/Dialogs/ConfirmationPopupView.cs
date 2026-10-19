using UnityEngine;
using UnityEngine.UI;

namespace Zephyr.Core.UI.Dialogs
{
    /// <summary>
    /// Content-agnostic confirmation popup. Its owner supplies text and button behavior.
    /// </summary>
    public sealed class ConfirmationPopupView : MonoBehaviour
    {
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private Text _title;
        [SerializeField] private Text _message;
        [SerializeField] private Text _confirmLabel;
        [SerializeField] private Text _cancelLabel;

        public Button ConfirmButton => _confirmButton;
        public Button CancelButton => _cancelButton;

        public void Configure(string title, string message, string confirmLabel, string cancelLabel)
        {
            if (!HasRequiredBindings())
            {
                Debug.LogError("ConfirmationPopupView is missing serialized UI bindings.", this);
                return;
            }

            _title.text = title;
            _message.text = message;
            _confirmLabel.text = confirmLabel;
            _cancelLabel.text = cancelLabel;
        }

        private bool HasRequiredBindings()
        {
            return _confirmButton != null && _cancelButton != null && _title != null &&
                _message != null && _confirmLabel != null && _cancelLabel != null;
        }
    }
}
