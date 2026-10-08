using UnityEngine;
using Zephyr.Core.Interfaces;
using Zephyr.Gameplay.Player.Interaction;

namespace Zephyr.Gameplay.Interaction
{
    /// <summary>
    /// Interactable gust launcher. Destination is authored as a child Transform
    /// in the prefab so level designers can reposition it without code changes.
    /// </summary>
    public sealed class GustInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform _destination;
        [SerializeField] private float _minimumDistance = 0.05f;

        public Transform Destination => _destination;

        public bool CanInteract()
        {
            if (_destination == null) return false;
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            return player != null && player.GetComponentInChildren<PlayerGustJumpController>(true) != null;
        }

        public void OnInteract()
        {
            if (_destination == null) return;
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            PlayerGustJumpController controller = player != null
                ? player.GetComponentInChildren<PlayerGustJumpController>(true)
                : null;
            if (controller == null || Vector2.Distance(player.transform.position, _destination.position) < _minimumDistance) return;
            controller.RequestGustJump(_destination);
        }
    }
}
