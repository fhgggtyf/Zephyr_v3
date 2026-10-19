using UnityEngine;
using UnityEngine.Events;
using Zephyr.Core.Events;
using Zephyr.Contracts.Interfaces;

namespace Zephyr.Core.Meta
{
    public sealed class StoryProgressionGate : MonoBehaviour, IStoryProgressionContextConsumer
    {
        [SerializeField] private MilestoneConditionSO _condition;
        [SerializeField] private bool _disableGameObjectWhenUnlocked;
        [SerializeField] private UnityEvent _onUnlocked;

        public bool IsUnlocked { get; private set; }
        public MilestoneConditionSO Condition => _condition;
        private IStoryProgressionGateway _storyProgression;
        private IEventBusGateway _eventBus;

        public void Configure(IStoryProgressionGateway storyProgression, IEventBusGateway eventBus)
        {
            if (_eventBus != null)
                _eventBus.Unsubscribe<MilestoneCompletedEvent>(HandleMilestoneCompleted);
            _storyProgression = storyProgression;
            _eventBus = eventBus;
            if (isActiveAndEnabled)
                _eventBus?.Subscribe<MilestoneCompletedEvent>(HandleMilestoneCompleted);
            Refresh();
        }

        private void Start()
        {
            Refresh();
        }

        private void OnEnable()
        {
            _eventBus?.Subscribe<MilestoneCompletedEvent>(HandleMilestoneCompleted);
        }

        private void OnDisable()
        {
            _eventBus?.Unsubscribe<MilestoneCompletedEvent>(HandleMilestoneCompleted);
        }

        public void Refresh()
        {
            bool unlocked = _storyProgression != null && _condition != null && _condition.Evaluate(_storyProgression);
            if (unlocked == IsUnlocked) return;

            IsUnlocked = unlocked;
            if (IsUnlocked)
            {
                _onUnlocked?.Invoke();
                if (_disableGameObjectWhenUnlocked)
                    gameObject.SetActive(false);
            }
        }

        private void HandleMilestoneCompleted(MilestoneCompletedEvent _) => Refresh();
    }
}
