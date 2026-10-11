using System.Collections.Generic;
using UnityEngine;
using Zephyr.Core.Flow.Scenes;
using Zephyr.Core.Interfaces;
using Zephyr.Core.Levels;

namespace Zephyr.Gameplay.Levels
{
    /// <summary>
    /// Tutorial exit gate. The scene discovers its room and enemy objectives at runtime,
    /// while serialized fields remain available for an explicitly authored override.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class TutorialObjectiveExit : MonoBehaviour
    {
        [SerializeField] private RoomController _roomController;
        [SerializeField] private MonoBehaviour[] _requiredObjectives;
        [SerializeField] private bool _logBlockedAttempt = true;
        private bool _hasLoggedBlockedAttempt;

        public int RemainingTargets => CountRemainingTargets();
        public bool IsUnlocked => RemainingTargets == 0;

        private void Awake()
        {
            Collider2D trigger = GetComponent<Collider2D>();
            if (trigger != null)
                trigger.isTrigger = true;

            if (_roomController == null)
                _roomController = FindAnyObjectByType<RoomController>();

            if (_requiredObjectives == null || _requiredObjectives.Length == 0)
            {
                List<MonoBehaviour> objectives = new List<MonoBehaviour>();
                MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
                foreach (MonoBehaviour behaviour in behaviours)
                {
                    if (behaviour is ILevelObjective) objectives.Add(behaviour);
                }
                _requiredObjectives = objectives.ToArray();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            int remaining = CountRemainingTargets();
            if (remaining > 0)
            {
                if (_logBlockedAttempt || !_hasLoggedBlockedAttempt)
                    Debug.Log($"还需击杀 {remaining} 个目标", this);
                _hasLoggedBlockedAttempt = true;
                return;
            }

            _roomController?.BeginExit();
            if (SceneProgressionController.Instance != null &&
                SceneProgressionController.Instance.IsFinalTutorialScene())
                TutorialController.MarkCompleted();

            if (SceneProgressionController.Instance != null)
            {
                SceneProgressionController.Instance.AdvanceFromCurrentScene();
            }
            else
            {
                Debug.Log("Tutorial exit unlocked: all objectives complete.", this);
            }
        }

        private int CountRemainingTargets()
        {
            if (_requiredObjectives == null || _requiredObjectives.Length == 0) return 0;

            int remaining = 0;
            for (int i = 0; i < _requiredObjectives.Length; i++)
            {
                MonoBehaviour objectiveBehaviour = _requiredObjectives[i];
                if (objectiveBehaviour != null && objectiveBehaviour.isActiveAndEnabled &&
                    objectiveBehaviour is ILevelObjective objective && !objective.IsComplete)
                    remaining++;
            }

            return remaining;
        }
    }
}
