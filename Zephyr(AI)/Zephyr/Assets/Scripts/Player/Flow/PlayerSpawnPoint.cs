using UnityEngine;

namespace Zephyr.Gameplay.Player.Flow
{
    /// <summary>Scene marker whose transform defines the player entry position and facing.</summary>
    public sealed class PlayerSpawnPoint : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.85f, 0.55f, 1f);
            Gizmos.DrawWireSphere(transform.position, 0.35f);
            Gizmos.DrawLine(transform.position, transform.position + transform.right * 0.8f);
        }
    }
}
