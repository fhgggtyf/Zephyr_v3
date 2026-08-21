using System;
using UnityEngine;
using UnityEngine.Localization;

namespace Zephyr.Core.UI.Dialogue
{
    public enum DialogueSpeakerSide
    {
        Left,
        Right
    }

    /// <summary>Authored content for one two-person dialogue sequence.</summary>
    [CreateAssetMenu(fileName = "Dialogue", menuName = "Zephyr/Dialogue/Dialogue")]
    public sealed class DialogueSO : ScriptableObject
    {
        [SerializeField] private string _dialogueId;
        [SerializeField] private DialogueParticipant _leftParticipant;
        [SerializeField] private DialogueParticipant _rightParticipant;
        [SerializeField] private DialogueLine[] _lines = Array.Empty<DialogueLine>();

        public string DialogueId => string.IsNullOrWhiteSpace(_dialogueId) ? name : _dialogueId;
        public DialogueParticipant LeftParticipant => _leftParticipant;
        public DialogueParticipant RightParticipant => _rightParticipant;
        public int LineCount => _lines?.Length ?? 0;

        public DialogueLine GetLine(int index)
        {
            if (_lines == null || _lines.Length == 0) return default;
            return _lines[Mathf.Clamp(index, 0, _lines.Length - 1)];
        }

        public DialogueParticipant GetParticipant(DialogueSpeakerSide side)
            => side == DialogueSpeakerSide.Left ? _leftParticipant : _rightParticipant;
    }

    [Serializable]
    public struct DialogueParticipant
    {
        [Header("Localization Package")]
        [SerializeField] private LocalizedString _name;
        [SerializeField] private string _fallbackName;

        [Header("Portrait")]
        [Tooltip("A frameless 160 x 160 portrait shown on this participant's side of the screen.")]
        [SerializeField] private Sprite _portrait;

        public LocalizedString NameReference => _name;
        public string FallbackName => _fallbackName;
        public Sprite Portrait => _portrait;

        public string ResolveName()
        {
            if (_name != null && !_name.IsEmpty)
            {
                try
                {
                    string localized = _name.GetLocalizedString();
                    if (!string.IsNullOrWhiteSpace(localized)) return localized;
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"Dialogue participant localization failed: {exception.Message}");
                }
            }

            return _fallbackName ?? string.Empty;
        }
    }

    [Serializable]
    public struct DialogueLine
    {
        [Header("Speaker")]
        [SerializeField] private DialogueSpeakerSide _speaker;

        [Header("Localization Package")]
        [SerializeField] private LocalizedString _text;
        [SerializeField, TextArea(2, 5)] private string _fallbackText;

        public DialogueSpeakerSide Speaker => _speaker;
        public LocalizedString TextReference => _text;
        public string FallbackText => _fallbackText;

        public string ResolveText()
        {
            if (_text != null && !_text.IsEmpty)
            {
                try
                {
                    string localized = _text.GetLocalizedString();
                    if (!string.IsNullOrWhiteSpace(localized)) return localized;
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"Dialogue text localization failed: {exception.Message}");
                }
            }

            return _fallbackText ?? string.Empty;
        }
    }
}
