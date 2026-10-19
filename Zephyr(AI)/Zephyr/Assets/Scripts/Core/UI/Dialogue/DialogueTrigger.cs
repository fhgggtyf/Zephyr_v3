using UnityEngine;
using Zephyr.Core.Meta;
using Zephyr.Core.Save;
using Zephyr.Contracts.Interfaces;

namespace Zephyr.Core.UI.Dialogue
{
    /// <summary>Starts a dialogue when the tagged player crosses this scene-local trigger.</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class DialogueTrigger : MonoBehaviour, IDialogueTriggerConsumer
    {
        [SerializeField] private DialogueSO _dialogue;
        [SerializeField] private bool _triggerOnce = true;
        [SerializeField] private StoryFactSO _requiredFact;
        [SerializeField] private MilestoneSO _requiredMilestone;
        [SerializeField] private MilestoneSO _completionMilestone;
        [SerializeField] private int _minimumRunStage = -1;
        [SerializeField] private bool _requireCompletedTutorial;
        [SerializeField] private string _playerTag = "Player";
        [SerializeField] private LayerMask _playerLayers = -1;

        private bool _triggered;
        private ISaveDataGateway _saveData;
        private IStoryProgressionGateway _storyProgression;
        private IDialogueGateway _dialogueGateway;

        public DialogueSO Dialogue => _dialogue;

        public void Configure(ISaveDataGateway saveData, IStoryProgressionGateway storyProgression,
            IDialogueGateway dialogueGateway)
        {
            _saveData = saveData;
            _storyProgression = storyProgression;
            _dialogueGateway = dialogueGateway;
        }

        private void Reset()
        {
            Collider2D trigger = GetComponent<Collider2D>();
            trigger.isTrigger = true;
            _playerLayers = -1;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_triggered || !IsPlayer(other) || !IsProgressReached()) return;
            TryStart();
        }

        public bool TryStart()
        {
            if (_triggered || _dialogue == null || _dialogueGateway == null) return false;
            if (_triggerOnce && HasBeenRecorded()) return false;
            if (!IsProgressReached()) return false;
            if (!_dialogueGateway.TryStart(_dialogue, MarkTriggered)) return false;
            _triggered = true;
            return true;
        }

        private void MarkTriggered()
        {
            if (!_triggerOnce || _dialogue == null) return;
            if (_completionMilestone != null)
                _storyProgression?.Complete(_completionMilestone);
        }

        private bool HasBeenRecorded()
        {
            return _completionMilestone != null
                && _storyProgression != null
                && _storyProgression.IsCompleted(_completionMilestone);
        }

        private bool IsProgressReached()
        {
            bool hasRequirement = _requireCompletedTutorial || _minimumRunStage >= 0
                || _requiredFact != null || _requiredMilestone != null;
            if (!hasRequirement) return true;

            if (_saveData?.Meta == null) return false;
            if (_requireCompletedTutorial && !_saveData.Meta.HasCompletedTutorial) return false;
            if (_requiredFact != null
                && (_storyProgression == null || !_requiredFact.ValueType.Equals(StoryFactValueType.Bool)
                    || !_storyProgression.GetBool(_requiredFact))) return false;
            if (_requiredMilestone != null
                && (_storyProgression == null || !_storyProgression.IsCompleted(_requiredMilestone))) return false;
            if (_minimumRunStage >= 0 && (_saveData.CurrentRun == null || _saveData.CurrentRun.CurrentStage < _minimumRunStage)) return false;
            return true;
        }

        private bool IsPlayer(Collider2D other)
        {
            if (other == null || (_playerLayers.value & (1 << other.gameObject.layer)) == 0) return false;
            if (string.IsNullOrWhiteSpace(_playerTag)) return true;
            try { return other.transform.root.CompareTag(_playerTag); }
            catch (UnityException) { return false; }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
            Collider2D collider = GetComponent<Collider2D>();
            if (collider is BoxCollider2D box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.offset, box.size);
            }
        }
    }
}
