using UnityEngine;
using Zephyr.Core.Meta;
using Zephyr.Core.Save;

namespace Zephyr.Core.UI.Dialogue
{
    /// <summary>Starts a dialogue when the tagged player crosses this scene-local trigger.</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class DialogueTrigger : MonoBehaviour
    {
        [SerializeField] private DialogueSO _dialogue;
        [SerializeField] private bool _triggerOnce = true;
        [SerializeField] private string _requiredObjectiveId;
        [SerializeField] private string _requiredStoryBeatId;
        [SerializeField] private int _minimumRunStage = -1;
        [SerializeField] private bool _requireCompletedTutorial;
        [SerializeField] private string _playerTag = "Player";
        [SerializeField] private LayerMask _playerLayers = -1;

        private bool _triggered;

        public DialogueSO Dialogue => _dialogue;

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
            if (_triggered || _dialogue == null || DialogueManager.Instance == null) return false;
            if (_triggerOnce && HasBeenRecorded()) return false;
            if (!IsProgressReached()) return false;
            if (!DialogueManager.Instance.TryStart(_dialogue, MarkTriggered)) return false;
            _triggered = true;
            return true;
        }

        private void MarkTriggered()
        {
            if (!_triggerOnce || _dialogue == null) return;
            SaveSystem save = SaveSystem.Instance;
            if (save?.Meta == null || string.IsNullOrWhiteSpace(_dialogue.DialogueId)) return;
            StoryProgressionSystem.MarkBeatTriggered(save.Meta, _dialogue.DialogueId);
        }

        private bool HasBeenRecorded()
        {
            SaveSystem save = SaveSystem.Instance;
            return save?.Meta != null && !string.IsNullOrWhiteSpace(_dialogue?.DialogueId)
                && save.Meta.TriggeredStoryBeats.Contains(_dialogue.DialogueId);
        }

        private bool IsProgressReached()
        {
            bool hasRequirement = _requireCompletedTutorial || _minimumRunStage >= 0
                || !string.IsNullOrWhiteSpace(_requiredObjectiveId)
                || !string.IsNullOrWhiteSpace(_requiredStoryBeatId);
            if (!hasRequirement) return true;

            SaveSystem save = SaveSystem.Instance;
            if (save?.Meta == null) return false;
            if (_requireCompletedTutorial && !save.Meta.HasCompletedTutorial) return false;
            if (!string.IsNullOrWhiteSpace(_requiredObjectiveId)
                && !save.Meta.FulfilledObjectives.Contains(_requiredObjectiveId)) return false;
            if (!string.IsNullOrWhiteSpace(_requiredStoryBeatId)
                && !save.Meta.TriggeredStoryBeats.Contains(_requiredStoryBeatId)) return false;
            if (_minimumRunStage >= 0 && (save.CurrentRun == null || save.CurrentRun.CurrentStage < _minimumRunStage)) return false;
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
