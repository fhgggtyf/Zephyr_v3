using UnityEngine;
using Zephyr.Core.ObjectPool;
using Zephyr.Core.Save;

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
        private PooledObject _pooledObject;
        private void Awake() => _pooledObject = GetComponent<PooledObject>();
        public void ResetState() { _player = null; _collected = false; }
        public void Spawn(Vector2 position, int amount)
        { transform.position = position; _amount = Mathf.Max(0, amount); ResetState(); }
        private void Update()
        {
            if (_collected) return;
            if (_player == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                _player = player != null ? player.transform : null;
            }
            if (_player == null || Vector2.Distance(transform.position, _player.position) > _magnetRange) return;
            transform.position = Vector2.MoveTowards(transform.position, _player.position, _magnetSpeed * Time.deltaTime);
            if (Vector2.Distance(transform.position, _player.position) < 0.08f) Collect();
        }
        private void Collect()
        {
            _collected = true;
            if (SaveSystem.Instance?.CurrentRun != null) SaveSystem.Instance.CurrentRun.RunCurrency += _amount;
            _pooledObject.ReturnToPool();
        }
        private void OnDisable() => ResetState();
    }
}
