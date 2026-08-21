using System;
using UnityEngine;
using UnityEngine.UI;

namespace Zephyr.Core.UI.Dialogue
{
    /// <summary>Visual shell for dialogue. All artwork is replaceable in the prefab.</summary>
    public sealed class DialogueView : MonoBehaviour
    {
        [Header("Replaceable artwork")]
        [SerializeField] private Image _backdrop;
        [SerializeField] private Image _dialogueBox;
        [SerializeField] private Image _leftPortrait;
        [SerializeField] private Image _rightPortrait;
        [SerializeField] private Image _continueIndicator;

        [Header("Text")]
        [SerializeField] private Text _leftSpeaker;
        [SerializeField] private Text _rightSpeaker;
        [SerializeField] private Text _body;
        [SerializeField] private Text _counter;
        [SerializeField] private Button _advanceButton;

        public event Action AdvanceRequested;
        public Image LeftPortraitImage => _leftPortrait;
        public Image RightPortraitImage => _rightPortrait;
        public Image DialogueBoxImage => _dialogueBox;
        public Image BackdropImage => _backdrop;

        private void Awake()
        {
            ResolveReferences();
            if (_advanceButton != null) _advanceButton.onClick.AddListener(HandleAdvanceClicked);
        }

        private void OnDestroy()
        {
            if (_advanceButton != null) _advanceButton.onClick.RemoveListener(HandleAdvanceClicked);
        }

        public void SetLine(DialogueLine line, DialogueParticipant participant,
            string speaker, string body, int index, int total)
        {
            ResolveReferences();
            if (_body != null) _body.text = body;
            if (_counter != null) _counter.text = total > 1 ? $"{index + 1}/{total}" : string.Empty;

            bool leftIsSpeaking = line.Speaker == DialogueSpeakerSide.Left;
            SetSpeaker(_leftPortrait, _leftSpeaker, participant, speaker, leftIsSpeaking);
            SetSpeaker(_rightPortrait, _rightSpeaker, participant, speaker, !leftIsSpeaking);
        }

        public void SetContinueVisible(bool visible)
        {
            if (_continueIndicator != null) _continueIndicator.enabled = visible;
        }

        private void HandleAdvanceClicked() => AdvanceRequested?.Invoke();

        private void ResolveReferences()
        {
            _backdrop ??= FindImage("Backdrop");
            _dialogueBox ??= FindImage("DialogueBox");
            _leftPortrait ??= FindImage("LeftPortrait");
            _rightPortrait ??= FindImage("RightPortrait");
            _continueIndicator ??= FindImage("ContinueIndicator");
            _leftSpeaker ??= FindText("LeftSpeaker");
            _rightSpeaker ??= FindText("RightSpeaker");
            _body ??= FindText("Body");
            _counter ??= FindText("Counter");
            _advanceButton ??= GetComponentInChildren<Button>(true);

            Font fallbackFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (fallbackFont != null)
            {
                if (_leftSpeaker != null && _leftSpeaker.font == null) _leftSpeaker.font = fallbackFont;
                if (_rightSpeaker != null && _rightSpeaker.font == null) _rightSpeaker.font = fallbackFont;
                if (_body != null && _body.font == null) _body.font = fallbackFont;
                if (_counter != null && _counter.font == null) _counter.font = fallbackFont;
            }
        }

        private Image FindImage(string childName)
        {
            Transform child = transform.Find(childName);
            return child != null ? child.GetComponent<Image>() : null;
        }

        private Text FindText(string childName)
        {
            Transform child = transform.Find("DialogueBox/" + childName);
            return child != null ? child.GetComponent<Text>() : null;
        }

        private static void SetSpeaker(Image portrait, Text label, DialogueParticipant participant,
            string speaker, bool active)
        {
            if (portrait != null)
            {
                portrait.sprite = active ? participant.Portrait : null;
                portrait.enabled = active && participant.Portrait != null;
            }

            if (label != null)
            {
                label.text = active ? speaker : string.Empty;
                label.enabled = active;
            }
        }
    }
}
