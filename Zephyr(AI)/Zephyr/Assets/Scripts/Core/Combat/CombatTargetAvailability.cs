using UnityEngine;

namespace Zephyr.Core.Combat
{
    /// <summary>
    /// Explicitly controls whether an actor can be selected as a combat target.
    /// Enemies treat unavailable actors as if they had left detection range.
    /// </summary>
    public sealed class CombatTargetAvailability : MonoBehaviour
    {
        [SerializeField] private bool _available = true;

        public bool IsAvailable => _available && isActiveAndEnabled;

        public void SetAvailable(bool available) => _available = available;
    }
}
