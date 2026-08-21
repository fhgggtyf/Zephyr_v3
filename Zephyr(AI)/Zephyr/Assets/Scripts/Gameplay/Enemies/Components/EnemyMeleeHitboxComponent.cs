using System.Collections.Generic;
using UnityEngine;
using Zephyr.Core.Interfaces;

namespace Zephyr.Gameplay.Enemies
{
    /// <summary>
    /// Built-in melee effect for an enemy attack. The active window comes from
    /// EnemyAttackDefinition; this component only owns hit geometry and targets.
    /// </summary>
    public sealed class EnemyMeleeHitboxComponent : EnemyComponent
    {
        private const int MaxOverlapResults = 32;

        [Header("Hitbox")]
        [Tooltip("Center offset from the enemy root. X is mirrored with attack facing.")]
        [SerializeField] private Vector2 _localOffset = new Vector2(0.55f, 0.85f);
        [SerializeField] private Vector2 _size = new Vector2(0.9f, 1.2f);
        [SerializeField] private LayerMask _targetLayers = ~0;

        [Header("Scene Gizmo")]
        [SerializeField] private bool _showGizmo = true;
        [Tooltip("Facing used for the edit-mode preview. Runtime attacks use EnemyMotor facing.")]
        [SerializeField] private bool _previewFacingRight = true;

        private readonly Collider2D[] _overlapResults = new Collider2D[MaxOverlapResults];
        private readonly HashSet<IDamageable> _hitTargets = new HashSet<IDamageable>();
        private ContactFilter2D _contactFilter;
        private bool _hitWindowOpen;

        public Vector2 LocalOffset => _localOffset;
        public Vector2 Size => _size;
        public LayerMask TargetLayers => _targetLayers;

        private void Update()
        {
            if (_hitWindowOpen && Controller != null)
                ProcessOverlaps();
        }

        protected override void HandleAttackStarted(int attackIndex)
        {
            _hitTargets.Clear();
            _hitWindowOpen = false;

            _contactFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = _targetLayers,
                useTriggers = true
            };
        }

        protected override void HandleAttackEnded() => CloseHitWindow();

        protected override void HandleHitWindowOpened()
        {
            _hitTargets.Clear();
            _hitWindowOpen = Controller != null;
            if (_hitWindowOpen) ProcessOverlaps();
        }

        protected override void HandleHitWindowClosed() => CloseHitWindow();

        protected override void OnShutdown()
        {
            CloseHitWindow();
            base.OnShutdown();
        }

        private void ProcessOverlaps()
        {
            Vector2 center = ResolveWorldCenter();
            float angle = transform.eulerAngles.z;
            int hitCount = Physics2D.OverlapBox(
                center, _size, angle, _contactFilter, _overlapResults);

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = _overlapResults[i];
                if (hit == null) continue;

                IDamageable damageable = FindDamageable(hit);
                if (damageable == null || !_hitTargets.Add(damageable)) continue;

                Component receiver = damageable as Component;
                if (receiver != null && receiver.transform.root == transform.root)
                {
                    _hitTargets.Remove(damageable);
                    continue;
                }

                damageable.TakeDamage(Controller.CurrentDamageInfo);
            }
        }

        private Vector2 ResolveWorldCenter()
        {
            Vector2 offset = _localOffset;
            bool facingRight = Application.isPlaying && Controller != null && Controller.IsAttacking
                ? Controller.AttackFacing.x >= 0f
                : _previewFacingRight;
            if (!facingRight) offset.x = -Mathf.Abs(offset.x);
            else offset.x = Mathf.Abs(offset.x);
            return transform.TransformPoint(offset);
        }

        private void OnValidate()
        {
            _size.x = Mathf.Max(0f, _size.x);
            _size.y = Mathf.Max(0f, _size.y);
        }

        private static IDamageable FindDamageable(Collider2D hit)
        {
            foreach (MonoBehaviour behaviour in hit.GetComponentsInParent<MonoBehaviour>(true))
                if (behaviour is IDamageable damageable) return damageable;
            foreach (MonoBehaviour behaviour in hit.GetComponentsInChildren<MonoBehaviour>(true))
                if (behaviour is IDamageable damageable) return damageable;
            return null;
        }

        private void CloseHitWindow()
        {
            _hitWindowOpen = false;
            _hitTargets.Clear();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!_showGizmo) return;

            bool active = Application.isPlaying && _hitWindowOpen;
            Gizmos.color = active
                ? new Color(1f, 0.15f, 0.1f, 1f)
                : new Color(0.1f, 0.85f, 1f, 0.9f);
            Gizmos.matrix = Matrix4x4.TRS(ResolveWorldCenter(), transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, _size);
        }
#endif
    }
}
