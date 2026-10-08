using UnityEngine;
using Zephyr.Core.Levels;
using Zephyr.Gameplay.Enemies;

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
        [SerializeField] private EnemyBrain[] _requiredEnemies;
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

            if (_requiredEnemies == null || _requiredEnemies.Length == 0)
                _requiredEnemies = FindObjectsByType<EnemyBrain>(FindObjectsInactive.Include);
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
            if (_requiredEnemies == null || _requiredEnemies.Length == 0) return 0;

            int remaining = 0;
            for (int i = 0; i < _requiredEnemies.Length; i++)
            {
                EnemyBrain enemy = _requiredEnemies[i];
                if (enemy == null || (enemy.isActiveAndEnabled && !enemy.IsDead)) remaining++;
            }

            return remaining;
        }
    }
}
