using System;
using UnityEngine;
using Zephyr.Contracts.Interfaces;
using Zephyr.Core.ObjectPool;

namespace Zephyr.Core.Projectiles
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(PooledObject))]
    public sealed class Projectile : MonoBehaviour, ITimerGatewayConsumer
    {
        [SerializeField] private LayerMask _hitLayers = ~0;
        [SerializeField] private bool _returnOnWorldCollision = true;

        private Rigidbody2D _body;
        private PooledObject _pooledObject;
        private ProjectileLaunchData _data;
        private IDisposable _lifetime;
        private int _remainingPierces;
        private GameObject _sourceRoot;
        private ITimerGateway _timer;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _pooledObject = GetComponent<PooledObject>();
        }

        public void Configure(ITimerGateway timer) => _timer = timer;

        public void Launch(in ProjectileLaunchData data)
        {
            _data = data;
            _remainingPierces = data.PierceCount;
            _sourceRoot = data.DamageOnHit.Source != null ? data.DamageOnHit.Source.transform.root.gameObject : null;
            transform.position = data.Origin;
            _body.gravityScale = data.AffectedByGravity ? data.GravityScale : 0f;
            _body.linearVelocity = data.Velocity;
            _lifetime?.Dispose();
            if (data.Lifetime > 0f)
                _lifetime = _timer?.SetTimeout(data.Lifetime, ReturnToPool);
        }

        public void ResetState()
        {
            _lifetime?.Dispose();
            _lifetime = null;
            _remainingPierces = 0;
            _sourceRoot = null;
            if (_body != null)
            {
                _body.linearVelocity = Vector2.zero;
                _body.angularVelocity = 0f;
                _body.gravityScale = 0f;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null || (_hitLayers.value & (1 << other.gameObject.layer)) == 0) return;
            if (_sourceRoot != null && other.transform.root.gameObject == _sourceRoot) return;

            Component target = FindDamageReceiver(other);
            if (target != null)
            {
                if (target is IDamageReceiver damageReceiver)
                    damageReceiver.ReceiveDamage(_data.DamageOnHit, other.ClosestPoint(transform.position));
                if (_remainingPierces-- <= 0) ReturnToPool();
                return;
            }

            if (_returnOnWorldCollision) ReturnToPool();
        }

        private static Component FindDamageReceiver(Collider2D collider)
        {
            MonoBehaviour[] behaviours = collider.GetComponentsInParent<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
                if (behaviours[i] is IDamageReceiver) return behaviours[i];
            return null;
        }

        private void ReturnToPool()
        {
            ResetState();
            _pooledObject.ReturnToPool();
        }

        private void OnDisable() => ResetState();
    }
}
