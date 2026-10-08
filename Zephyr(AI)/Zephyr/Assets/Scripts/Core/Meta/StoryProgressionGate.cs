using UnityEngine;
using UnityEngine.Events;
using Zephyr.Core.Events;

namespace Zephyr.Core.Meta
{
    public sealed class StoryProgressionGate : MonoBehaviour
    {
        [SerializeField] private MilestoneConditionSO _condition;
        [SerializeField] private bool _disableGameObjectWhenUnlocked;
        [SerializeField] private UnityEvent _onUnlocked;

        public bool IsUnlocked { get; private set; }
        public MilestoneConditionSO Condition => _condition;

        private void Start()
        {
            Refresh();
        }

        private void OnEnable()
        {
            if (EventBus.Instance != null)
                EventBus.Instance.Subscribe<MilestoneCompletedEvent>(HandleMilestoneCompleted);
        }

        private void OnDisable()
        {
            if (EventBus.Instance != null)
                EventBus.Instance.Unsubscribe<MilestoneCompletedEvent>(HandleMilestoneCompleted);
        }

        public void Refresh()
        {
            StoryProgressionService service = StoryProgressionService.Instance;
            bool unlocked = service != null && _condition != null && _condition.Evaluate(service);
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
