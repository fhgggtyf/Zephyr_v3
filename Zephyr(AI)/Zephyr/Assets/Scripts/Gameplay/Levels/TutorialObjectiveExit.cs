using UnityEngine;
using Zephyr.Contracts.Interfaces;
using Zephyr.Core.Flow.Scenes;
using Zephyr.Core.Flow;


using Zephyr.Core.Levels;

namespace Zephyr.Gameplay.Levels
{
    /// <summary>
    /// Tutorial exit gate. Room and objective references are authored by the scene.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class TutorialObjectiveExit : MonoBehaviour, ISceneProgressionGatewayConsumer,
        ITutorialCompletionGatewayConsumer
    {
        [SerializeField] private RoomController _roomController;
        [SerializeField] private MonoBehaviour[] _requiredObjectives;
        [SerializeField] private bool _logBlockedAttempt = true;
        private bool _hasLoggedBlockedAttempt;
        [SerializeField] private MonoBehaviour _progressionGatewaySource;
        private ISceneProgressionGateway _progressionGateway;
        private ITutorialCompletionGateway _tutorialCompletion;

        public void Configure(ISceneProgressionGateway gateway)
        {
            _progressionGateway = gateway;
        }

        public void Configure(ITutorialCompletionGateway gateway)
        {
            _tutorialCompletion = gateway;
        }

        public int RemainingTargets => CountRemainingTargets();
        public bool IsUnlocked => RemainingTargets == 0;

        private void Awake()
        {
            Collider2D trigger = GetComponent<Collider2D>();
            if (trigger != null)
                trigger.isTrigger = true;

            if (_roomController == null)
                Debug.LogError("TutorialObjectiveExit requires an explicit RoomController reference.", this);

            if (_requiredObjectives == null)
                Debug.LogError("TutorialObjectiveExit requires an authored objective list. Use an empty list for an objective-free tutorial phase.", this);
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
            if (_progressionGateway == null && _progressionGatewaySource is ISceneProgressionGateway source)
                _progressionGateway = source;

            if (_progressionGateway != null)
            {
                if (_progressionGateway.IsFinalTutorialScene())
                    _tutorialCompletion?.MarkCompleted();
                _progressionGateway.AdvanceFromCurrentScene();
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
