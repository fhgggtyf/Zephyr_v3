using UnityEngine;

namespace Zephyr.Gameplay.Levels
{
    /// <summary>Generic trigger used by non-objective scenes to advance the authored loop.</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class SceneProgressionExit : MonoBehaviour
    {
        private bool _triggered;

        private void Awake()
        {
            Collider2D trigger = GetComponent<Collider2D>();
            if (trigger != null) trigger.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_triggered || !other.CompareTag("Player")) return;
            if (SceneProgressionController.Instance == null) return;

            _triggered = SceneProgressionController.Instance.AdvanceFromCurrentScene();
        }
    }
}
