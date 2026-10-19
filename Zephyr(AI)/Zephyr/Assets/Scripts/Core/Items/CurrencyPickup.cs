using UnityEngine;
using Zephyr.Contracts.Events;
using Zephyr.Contracts.Interfaces;
using Zephyr.Core.ObjectPool;

namespace Zephyr.Core.Items
{
    [RequireComponent(typeof(PooledObject))]
    public sealed class CurrencyPickup : MonoBehaviour, IPickupRuntimeContextConsumer, ICurrencyPickup
    {
        [SerializeField] private int _amount = 1;
        [SerializeField] private float _magnetRange = 1.5f;
        [SerializeField] private float _magnetSpeed = 8f;
        private Transform _player;
        private bool _collected;
        private bool _subscribedToPlayerTarget;
        private PooledObject _pooledObject;
        private IPickupRuntimeContext _runtimeContext;
        private void Awake() => _pooledObject = GetComponent<PooledObject>();

        public void Configure(IPickupRuntimeContext context)
        {
            if (_subscribedToPlayerTarget) _runtimeContext?.UnsubscribePlayerTarget(HandlePlayerTargetChanged);
            _runtimeContext = context;
            _subscribedToPlayerTarget = false;
            _player = null;
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
            if (_subscribedToPlayerTarget) _runtimeContext?.UnsubscribePlayerTarget(HandlePlayerTargetChanged);
            _subscribedToPlayerTarget = false;
            _player = null;
            ResetState();
        }
        private void SubscribeToPlayerTarget()
        {
            if (_runtimeContext == null || _subscribedToPlayerTarget) return;
            _runtimeContext.SubscribePlayerTarget(HandlePlayerTargetChanged);
            _subscribedToPlayerTarget = true;
            if (_runtimeContext.TryGetLatestPlayerTarget(out PlayerTargetChangedEvent current))
                HandlePlayerTargetChanged(current);
        }
        private void HandlePlayerTargetChanged(PlayerTargetChangedEvent change) => _player = change.Target;
        public void ResetState() { _collected = false; }
        public void Spawn(Vector2 position, int amount)
        { transform.position = position; _amount = Mathf.Max(0, amount); ResetState(); }
        void ICurrencyPickup.Spawn(Vector3 position, int amount) => Spawn((Vector2)position, amount);

        private void Update()
        {
            if (_collected) return;
            if (_player == null || Vector2.Distance(transform.position, _player.position) > _magnetRange) return;
            transform.position = Vector2.MoveTowards(transform.position, _player.position, _magnetSpeed * Time.deltaTime);
            if (Vector2.Distance(transform.position, _player.position) < 0.08f) Collect();
        }
        private void Collect()
        {
            if (_runtimeContext == null || !_runtimeContext.TryAddRunCurrency(_amount)) return;
            _collected = true;
            _pooledObject.ReturnToPool();
        }
    }
}
