using UnityEngine;
using System.Collections.Generic;
using Zephyr.Contracts.Events;
using Zephyr.Contracts.Interfaces;

namespace Zephyr.Gameplay.Interaction
{
    /// <summary>
    /// Interactable gust launcher. Destination is authored as a child Transform
    /// in the prefab so level designers can reposition it without code changes.
    /// </summary>
    public sealed class GustInteractable : MonoBehaviour, IInteractable, IPlayerTargetContextConsumer
    {
        [SerializeField] private Transform _destination;
        [SerializeField] private float _minimumDistance = 0.05f;
        private GameObject _player;
        private bool _subscribedToPlayerTarget;
        private IPlayerTargetContext _playerTargetContext;
        private readonly List<IGustJumpReceiver> _receivers = new List<IGustJumpReceiver>();

        public Transform Destination => _destination;

        public void Configure(IPlayerTargetContext context)
        {
            if (_subscribedToPlayerTarget) _playerTargetContext?.UnsubscribePlayerTarget(HandlePlayerTargetChanged);
            _playerTargetContext = context;
            _subscribedToPlayerTarget = false;
            _player = null;
            _receivers.Clear();
            if (isActiveAndEnabled) SubscribeToPlayerTarget();
        }

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
            if (_subscribedToPlayerTarget) _playerTargetContext?.UnsubscribePlayerTarget(HandlePlayerTargetChanged);
            _subscribedToPlayerTarget = false;
            _player = null;
            _receivers.Clear();
        }

        private void SubscribeToPlayerTarget()
        {
            if (_playerTargetContext == null || _subscribedToPlayerTarget) return;
            _playerTargetContext.SubscribePlayerTarget(HandlePlayerTargetChanged);
            _subscribedToPlayerTarget = true;
            if (_playerTargetContext.TryGetLatestPlayerTarget(out PlayerTargetChangedEvent current))
                HandlePlayerTargetChanged(current);
        }

        private void HandlePlayerTargetChanged(PlayerTargetChangedEvent change)
        {
            _player = change.Player;
            _receivers.Clear();
            if (_player == null) return;
            foreach (MonoBehaviour behaviour in _player.GetComponentsInChildren<MonoBehaviour>(true))
                if (behaviour is IGustJumpReceiver receiver) _receivers.Add(receiver);
        }

        public bool CanInteract()
        {
            if (_destination == null) return false;
            return FindReceiver() != null;
        }

        public void OnInteract()
        {
            if (_destination == null) return;
            IGustJumpReceiver receiver = FindReceiver();
            if (receiver == null || _player == null ||
                Vector2.Distance(_player.transform.position, _destination.position) < _minimumDistance) return;
            receiver.RequestGustJump(_destination);
        }

        private IGustJumpReceiver FindReceiver()
        {
            if (_player == null) return null;
            foreach (IGustJumpReceiver receiver in _receivers)
            {
                if (receiver is MonoBehaviour behaviour && behaviour != null && receiver.CanRequestGustJump)
                    return receiver;
            }

            return null;
        }
    }
}
