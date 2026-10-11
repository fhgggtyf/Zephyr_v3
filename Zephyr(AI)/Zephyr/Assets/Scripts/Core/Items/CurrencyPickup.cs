using UnityEngine;
using Zephyr.Core.ObjectPool;
using Zephyr.Core.Save;
using Zephyr.Core.Events;

namespace Zephyr.Core.Items
{
    [RequireComponent(typeof(PooledObject))]
    public sealed class CurrencyPickup : MonoBehaviour
    {
        [SerializeField] private int _amount = 1;
        [SerializeField] private float _magnetRange = 1.5f;
        [SerializeField] private float _magnetSpeed = 8f;
        private Transform _player;
        private bool _collected;
        private bool _subscribedToPlayerTarget;
        private PooledObject _pooledObject;
        private ISaveDataGateway _saveData;
        private void Awake()
        {
            _pooledObject = GetComponent<PooledObject>();
            _saveData = SaveSystem.Instance;
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
            if (_subscribedToPlayerTarget && EventBus.Instance != null)
                EventBus.Instance.Unsubscribe<PlayerTargetChangedEvent>(HandlePlayerTargetChanged);
            _subscribedToPlayerTarget = false;
            _player = null;
            ResetState();
        }
        private void SubscribeToPlayerTarget()
        {
            if (EventBus.Instance == null) return;
            EventBus.Instance.Subscribe<PlayerTargetChangedEvent>(HandlePlayerTargetChanged);
            _subscribedToPlayerTarget = true;
            if (EventBus.Instance.TryGetLatest(out PlayerTargetChangedEvent current))
                HandlePlayerTargetChanged(current);
        }
        private void HandlePlayerTargetChanged(PlayerTargetChangedEvent change) => _player = change.Target;
        public void ResetState() { _player = null; _collected = false; }
        public void Spawn(Vector2 position, int amount)
        { transform.position = position; _amount = Mathf.Max(0, amount); ResetState(); }
        private void Update()
        {
            if (_collected) return;
            if (_player == null || Vector2.Distance(transform.position, _player.position) > _magnetRange) return;
            transform.position = Vector2.MoveTowards(transform.position, _player.position, _magnetSpeed * Time.deltaTime);
            if (Vector2.Distance(transform.position, _player.position) < 0.08f) Collect();
        }
        private void Collect()
        {
            _collected = true;
            if (_saveData?.CurrentRun != null) _saveData.CurrentRun.RunCurrency += _amount;
            _pooledObject.ReturnToPool();
        }
    }
}
