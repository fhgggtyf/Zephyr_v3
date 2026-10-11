using UnityEngine;
using Zephyr.Core.Events;
using Zephyr.Core.Interfaces;

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
        private GameObject _player;
        private bool _subscribedToPlayerTarget;

        public Transform Destination => _destination;

        private void OnEnable()
        {
            SubscribeToPlayerTarget();
        }

        private void Start()
        {
            SubscribeToPlayerTarget();
        }

        private void OnDisable()
        {
            if (_subscribedToPlayerTarget && EventBus.Instance != null)
                EventBus.Instance.Unsubscribe<PlayerTargetChangedEvent>(HandlePlayerTargetChanged);
            _subscribedToPlayerTarget = false;
            _player = null;
        }

        private void SubscribeToPlayerTarget()
        {
            if (EventBus.Instance == null) return;
            EventBus.Instance.Subscribe<PlayerTargetChangedEvent>(HandlePlayerTargetChanged);
            _subscribedToPlayerTarget = true;
            if (EventBus.Instance.TryGetLatest(out PlayerTargetChangedEvent current))
                HandlePlayerTargetChanged(current);
        }

        private void HandlePlayerTargetChanged(PlayerTargetChangedEvent change) => _player = change.Player;

        public bool CanInteract()
        {
            if (_destination == null) return false;
            return FindReceiver() != null;
        }

        public void OnInteract()
        {
            if (_destination == null) return;
            IGustJumpReceiver receiver = FindReceiver(_player);
            if (receiver == null || _player == null ||
                Vector2.Distance(_player.transform.position, _destination.position) < _minimumDistance) return;
            receiver.RequestGustJump(_destination);
        }

        private IGustJumpReceiver FindReceiver(GameObject player = null)
        {
            if (player == null) return null;

            MonoBehaviour[] behaviours = player.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IGustJumpReceiver receiver && receiver.CanRequestGustJump)
                    return receiver;
            }

            return null;
        }
    }
}
