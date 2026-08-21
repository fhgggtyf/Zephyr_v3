using System.Collections.Generic;
using UnityEngine;
using Zephyr.Core.DamageSystem;
using Zephyr.Core.Interfaces;

namespace Zephyr.Core.Weapons
{
    public class MeleeHitBoxWeaponComponent : WeaponComponent<AttackHitBoxData, AttackHitBoxAttackData>
    {
        private const int k_maxOverlapResults = 32;

        private readonly Collider2D[] m_overlapResults = new Collider2D[k_maxOverlapResults];
        private readonly HashSet<IDamageable> m_hitTargets = new HashSet<IDamageable>();
        private ContactFilter2D m_contactFilter;
        private bool m_isHitWindowOpen;

        private void FixedUpdate()
        {
            if (!m_isHitWindowOpen || Runtime == null || CurrentAttackData == null) return;

            ProcessOverlaps();
        }

        protected override void HandleAttackStarted(int comboIndex)
        {
            base.HandleAttackStarted(comboIndex);
            m_hitTargets.Clear();
            m_isHitWindowOpen = false;

            if (CurrentAttackData == null) return;

            m_contactFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = CurrentAttackData.LayerMask,
                useTriggers = true
            };
        }

        protected override void HandleAttackEnded()
        {
            CloseHitWindow();
        }

        protected override void HandleHitWindowOpened()
        {
            m_hitTargets.Clear();
            m_isHitWindowOpen = CurrentAttackData != null;

            if (m_isHitWindowOpen)
            {
                ProcessOverlaps();
            }
        }

        protected override void HandleHitWindowClosed()
        {
            CloseHitWindow();
        }

        protected override void OnShutdown()
        {
            CloseHitWindow();
            base.OnShutdown();
        }

        private void ProcessOverlaps()
        {
            Vector2 attackDirection = ResolveAttackDirection();
            (Vector2 worldCenter, float angle) = ResolveHitBoxTransform(attackDirection);
            int hitCount = Physics2D.OverlapBox(
                worldCenter,
                CurrentAttackData.Size,
                angle,
                m_contactFilter,
                m_overlapResults);

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = m_overlapResults[i];
                if (hit == null) continue;

                IDamageable damageable = FindDamageable(hit);
                if (damageable == null || m_hitTargets.Contains(damageable)) continue;

                Component receiver = damageable as Component;
                if (receiver != null && Runtime.Owner != null
                    && receiver.transform.root == Runtime.Owner.transform.root)
                {
                    continue;
                }

                DamageInfo damageInfo = WeaponDamageBuilder.Build(
                    Runtime.Weapon,
                    Runtime.StatSource,
                    Runtime.ComboIndex,
                    Runtime.Owner != null ? Runtime.Owner : Runtime.gameObject,
                    worldCenter);

                m_hitTargets.Add(damageable);
                damageable.TakeDamage(damageInfo);
            }
        }

        private Vector2 ResolveAttackDirection()
        {
            AttackDirectionData directionData = Runtime?.Weapon?.GetData<AttackDirectionData>();
            AttackDirectionMode mode = directionData?.GetAttackData(Runtime.ComboIndex)?.Mode
                ?? AttackDirectionMode.Facing;
            Vector2 facing = Runtime?.FacingProvider?.Facing ?? Vector2.right;

            return mode switch
            {
                AttackDirectionMode.Forward => Vector2.right,
                AttackDirectionMode.Backward => Vector2.left,
                AttackDirectionMode.Up => Vector2.up,
                AttackDirectionMode.Down => Vector2.down,
                _ => facing.sqrMagnitude > 0f ? facing.normalized : Vector2.right
            };
        }

        /// <summary>
        /// Resolves the final hitbox world center and rotation angle.
        /// For horizontal directions (Facing / Forward / Backward): mirrors only
        /// the X offset (Y stays unchanged), which matches how a player would
        /// swing on the opposite side without vertically flipping the hitbox.
        /// For vertical directions (Up / Down): applies a full 2D rotation to
        /// the LocalOffset and carries that rotation into the OverlapBox angle.
        /// </summary>
        private (Vector2 worldCenter, float angle) ResolveHitBoxTransform(Vector2 attackDirection)
        {
            Vector2 localOffset = CurrentAttackData.LocalOffset;
            AttackDirectionData directionData = Runtime?.Weapon?.GetData<AttackDirectionData>();
            AttackDirectionMode mode = directionData?.GetAttackData(Runtime.ComboIndex)?.Mode
                ?? AttackDirectionMode.Facing;
            float baseAngle = Runtime.transform.eulerAngles.z;

            bool isHorizontal = mode == AttackDirectionMode.Facing
                                || mode == AttackDirectionMode.Forward
                                || mode == AttackDirectionMode.Backward;

            if (isHorizontal)
            {
                // Horizontal: mirror X only, keep Y as authored
                if (attackDirection.x < 0f)
                {
                    localOffset.x = -localOffset.x;
                }

                Vector3 worldPos = Runtime.transform.TransformPoint(localOffset);
                return (worldPos, baseAngle);
            }
            else
            {
                // Vertical (Up / Down): apply full rotation to offset + angle
                float directionAngle = Mathf.Atan2(attackDirection.y, attackDirection.x) * Mathf.Rad2Deg;
                Vector2 rotatedOffset = Quaternion.Euler(0f, 0f, directionAngle) * localOffset;
                Vector3 worldPos = Runtime.transform.TransformPoint(rotatedOffset);
                return (worldPos, baseAngle + directionAngle);
            }
        }

        private static IDamageable FindDamageable(Collider2D collider)
        {
            MonoBehaviour[] parentBehaviours = collider.GetComponentsInParent<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in parentBehaviours)
            {
                if (behaviour is IDamageable damageable) return damageable;
            }

            MonoBehaviour[] childBehaviours = collider.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in childBehaviours)
            {
                if (behaviour is IDamageable damageable) return damageable;
            }

            return null;
        }

        private void CloseHitWindow()
        {
            m_isHitWindowOpen = false;
            m_hitTargets.Clear();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || CurrentAttackData == null || Runtime == null || !Runtime.IsAttacking)
            {
                return;
            }

            Vector2 attackDirection = ResolveAttackDirection();
            (Vector3 center, float angle) = ResolveHitBoxTransform(attackDirection);
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);

            Gizmos.color = m_isHitWindowOpen
                ? new Color(1f, 0.15f, 0.1f, 1f)
                : new Color(1f, 0.75f, 0.1f, 1f);
            Gizmos.matrix = Matrix4x4.TRS(center, rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, CurrentAttackData.Size);
        }
#endif
    }
}
