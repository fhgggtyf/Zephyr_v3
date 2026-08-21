using System;
using UnityEngine;
using Zephyr.Core.Interfaces;
using Zephyr.Core.Stats;

namespace Zephyr.Core.Weapons
{
    public class WeaponController : MonoBehaviour
    {
        [Header("Runtime")]
        [Tooltip("Existing WeaponRuntime, normally attached directly to WeaponSocket.")]
        [SerializeField] private WeaponRuntime m_runtime;
        [Tooltip("Existing WeaponSocket transform. Used only when a runtime prefab must be created.")]
        [SerializeField] private Transform m_weaponSocket;
        [Tooltip("Optional prefab containing WeaponRuntime. No hierarchy is created when this is empty.")]
        [SerializeField] private WeaponRuntime m_runtimePrefab;

        [Header("Loadout")]
        [SerializeField] private WeaponSet m_weaponSet = new WeaponSet();
        [SerializeField] private WeaponSlot m_activeSlot = WeaponSlot.Primary;

        private IStatSource m_statSource;
        private IAttackResourceConsumer m_attackResourceConsumer;
        private int m_comboIndex;
        private float m_comboResetAt;
        private WeaponSO m_comboWeapon;
        private bool m_isAttackComplete = true;
        private WeaponSlot? m_attackSlotOverride;
        private AttackContext? m_currentAttackContext;
        private WeaponSlot? m_requestedAttackSlot;
        private bool m_requestedAttackResourcePreConsumed;

        public event Action<WeaponSlot, WeaponSO> WeaponEquipped;
        public event Action<WeaponSlot> ActiveSlotChanged;
        public event Action AttackCompleted;

        public WeaponSlot ActiveSlot => m_activeSlot;
        public WeaponSO ActiveWeapon => m_currentAttackContext?.Weapon ?? m_weaponSet.GetWeapon(m_activeSlot);
        public WeaponRuntime Runtime => m_runtime;
        public bool IsAttacking => m_runtime != null && m_runtime.IsAttacking;
        public bool IsAttackComplete => m_isAttackComplete;
        public AttackContext? CurrentAttackContext => m_currentAttackContext;

        private void Awake()
        {
            m_statSource = GetComponentInParent<StatsCore>();
            m_attackResourceConsumer = ResolveAttackResourceConsumer();
            ResolveRuntime();
        }

        private void OnEnable()
        {
            ResolveRuntime();
            SubscribeRuntime();
            ApplyActiveWeapon();
        }

        private void Start()
        {
            ApplyActiveWeapon();
        }

        private void OnDisable()
        {
            UnsubscribeRuntime();
            CancelAttack();
        }

        public bool CanAttack(WeaponSlot slot)
        {
            bool hasRuntime = m_runtime != null;
            bool isRuntimeAttacking = hasRuntime && m_runtime.IsAttacking;
            WeaponSO weapon = m_weaponSet.GetWeapon(slot);
            bool hasWeapon = weapon != null;
            bool resourcePreConsumed = m_requestedAttackResourcePreConsumed
                && m_requestedAttackSlot == slot;
            bool canAfford = hasWeapon && (resourcePreConsumed
                || CanAffordAttackResource(weapon.AttackResourceCost));

            return hasRuntime && !isRuntimeAttacking && hasWeapon && canAfford;
        }

        public bool HasAttackResource(WeaponSlot slot)
        {
            WeaponSO weapon = m_weaponSet.GetWeapon(slot);
            return weapon != null && CanAffordAttackResource(weapon.AttackResourceCost);
        }

        public void RequestAttack(WeaponSlot slot)
        {
            m_requestedAttackSlot = slot;
            m_requestedAttackResourcePreConsumed = false;
        }

        public bool ConsumeRequestedAttackResource()
        {
            if (!m_requestedAttackSlot.HasValue) return false;

            WeaponSO weapon = m_weaponSet.GetWeapon(m_requestedAttackSlot.Value);
            if (weapon == null || !CanAffordAttackResource(weapon.AttackResourceCost)) return false;

            if (!TryConsumeAttackResource(weapon.AttackResourceCost)) return false;
            m_requestedAttackResourcePreConsumed = true;
            return true;
        }

        public bool BeginRequestedAttack()
        {
            if (!m_requestedAttackSlot.HasValue) return false;

            WeaponSlot slot = m_requestedAttackSlot.Value;
            bool resourcePreConsumed = m_requestedAttackResourcePreConsumed;
            if (!CanAttack(slot))
            {
                m_requestedAttackSlot = null;
                m_requestedAttackResourcePreConsumed = false;
                return false;
            }

            m_requestedAttackSlot = null;

            WeaponSO weapon = m_weaponSet.GetWeapon(slot);
            int comboIndex = Time.time > m_comboResetAt || m_comboWeapon != weapon ? 0 : m_comboIndex;
            ComboStep step = weapon.Combo.GetStep(comboIndex);
            float startedAt = Time.time;
            float endsAt = startedAt + GetEffectiveAttackDuration(step, weapon);
            AttackContext context = new AttackContext(weapon, slot, comboIndex, startedAt, endsAt);

            if (!resourcePreConsumed
                && !TryConsumeAttackResource(weapon.AttackResourceCost)) return false;

            m_requestedAttackResourcePreConsumed = false;

            m_attackSlotOverride = slot;
            m_currentAttackContext = context;
            m_isAttackComplete = false;
            m_comboIndex = comboIndex;
            m_comboWeapon = weapon;
            m_runtime.Equip(weapon, transform.root.gameObject, m_statSource);

            if (m_runtime.BeginAttack(context)) return true;

            ClearAttackContextAndRestoreWeapon();
            return false;
        }

        public void UpdateAttack()
        {
            if (!IsAttacking || !m_currentAttackContext.HasValue) return;
            if (Time.time >= m_currentAttackContext.Value.EndsAt) m_runtime.FinishAttack();
        }

        public void EndAttack()
        {
            m_runtime?.EndAttack();
        }

        public void CancelAttack()
        {
            m_requestedAttackSlot = null;
            m_requestedAttackResourcePreConsumed = false;
            m_runtime?.CancelAttack();
            ClearAttackContextAndRestoreWeapon();
        }

        public void Equip(WeaponSlot slot, WeaponSO weapon)
        {
            m_weaponSet.SetWeapon(slot, weapon);
            WeaponEquipped?.Invoke(slot, weapon);

            if (slot == m_activeSlot && !m_currentAttackContext.HasValue) ApplyActiveWeapon();
        }

        public void SetActiveSlot(WeaponSlot slot)
        {
            if (m_activeSlot == slot && !m_attackSlotOverride.HasValue) return;

            CancelAttack();
            m_activeSlot = slot;
            m_attackSlotOverride = null;
            ResetCombo();
            ApplyActiveWeapon();
            ActiveSlotChanged?.Invoke(slot);
        }

        public void SwapActiveSlot()
        {
            SetActiveSlot(m_activeSlot == WeaponSlot.Primary ? WeaponSlot.Secondary : WeaponSlot.Primary);
        }

        public void ResetCombo()
        {
            m_comboIndex = 0;
            m_comboResetAt = 0f;
            m_comboWeapon = null;
        }

        public WeaponSO GetWeapon(WeaponSlot slot)
        {
            return m_weaponSet.GetWeapon(slot);
        }

        public AttackMovementAttackData GetCurrentAttackMovementData()
        {
            if (!m_currentAttackContext.HasValue) return null;

            AttackContext context = m_currentAttackContext.Value;
            AttackMovementData movementData = context.Weapon.GetData<AttackMovementData>();
            return movementData?.GetAttackData(context.ComboIndex);
        }

        public bool CanChangeFacingDuringCurrentAttack()
        {
            if (!m_currentAttackContext.HasValue) return false;

            AttackContext context = m_currentAttackContext.Value;
            AttackFacingData facingData = context.Weapon.GetData<AttackFacingData>();
            return facingData?.GetAttackData(context.ComboIndex)?.CanChangeFacing ?? false;
        }

        /// <summary>
        /// Returns the forced horizontal facing for the current attack step,
        /// based on AttackDirectionData. A value of +1 means force-right
        /// (Forward), -1 means force-left (Backward), and 0 means no forced
        /// direction (Facing / Up / Down — keep the current facing or let
        /// input drive it). Used by WeaponAttackAction to flip the player
        /// body + weapon sprites at attack start when the weapon component
        /// specifies a fixed horizontal attack direction.
        /// </summary>
        public float GetCurrentAttackForcedHorizontalFacing()
        {
            if (!m_currentAttackContext.HasValue) return 0f;

            AttackContext context = m_currentAttackContext.Value;
            AttackDirectionData directionData = context.Weapon.GetData<AttackDirectionData>();
            AttackDirectionMode mode = directionData?.GetAttackData(context.ComboIndex)?.Mode
                ?? AttackDirectionMode.Facing;

            return mode switch
            {
                AttackDirectionMode.Forward => 1f,
                AttackDirectionMode.Backward => -1f,
                _ => 0f
            };
        }

        public float GetAttackNormalizedTime()
        {
            if (!m_currentAttackContext.HasValue) return 1f;

            AttackContext context = m_currentAttackContext.Value;
            float duration = context.EndsAt - context.StartedAt;
            if (duration <= 0f) return 1f;

            return Mathf.Clamp01((Time.time - context.StartedAt) / duration);
        }

        private void ClearAttackContextAndRestoreWeapon()
        {
            bool shouldRestoreWeapon = m_attackSlotOverride.HasValue || m_currentAttackContext.HasValue;
            m_attackSlotOverride = null;
            m_currentAttackContext = null;
            m_isAttackComplete = true;

            if (!shouldRestoreWeapon || m_runtime == null) return;

            WeaponSO weapon = m_weaponSet.GetWeapon(m_activeSlot);
            m_runtime.Equip(weapon, transform.root.gameObject, m_statSource);
        }

        private bool CanAffordAttackResource(AttackResourceCost cost)
        {
            if (cost.ResourceType == AttackResourceType.None || cost.Amount <= 0f) return true;

            m_attackResourceConsumer ??= ResolveAttackResourceConsumer();
            return m_attackResourceConsumer != null
                && m_attackResourceConsumer.CanAffordAttackResource(cost);
        }

        private bool TryConsumeAttackResource(AttackResourceCost cost)
        {
            if (cost.ResourceType == AttackResourceType.None || cost.Amount <= 0f) return true;

            m_attackResourceConsumer ??= ResolveAttackResourceConsumer();
            return m_attackResourceConsumer != null
                && m_attackResourceConsumer.TryConsumeAttackResource(cost);
        }

        private IAttackResourceConsumer ResolveAttackResourceConsumer()
        {
            MonoBehaviour[] parentBehaviours = GetComponentsInParent<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in parentBehaviours)
            {
                if (behaviour is IAttackResourceConsumer consumer) return consumer;
            }

            MonoBehaviour[] childBehaviours = GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in childBehaviours)
            {
                if (behaviour is IAttackResourceConsumer consumer) return consumer;
            }

            return null;
        }

        private void ResolveRuntime()
        {
            if (m_runtime == null && m_weaponSocket != null)
            {
                m_runtime = m_weaponSocket.GetComponent<WeaponRuntime>();
            }

            if (m_runtime != null || m_runtimePrefab == null || m_weaponSocket == null) return;

            m_runtime = Instantiate(m_runtimePrefab, m_weaponSocket);
        }

        private void ApplyActiveWeapon()
        {
            if (m_runtime == null) return;

            WeaponSO weapon = ActiveWeapon;
            if (weapon == null)
            {
                m_runtime.Equip(null, transform.root.gameObject, m_statSource);
                ResetCombo();
                m_isAttackComplete = true;
                return;
            }

            m_runtime.Equip(weapon, transform.root.gameObject, m_statSource);
            ResetCombo();
            m_isAttackComplete = true;
        }

        private void SubscribeRuntime()
        {
            if (m_runtime == null) return;

            m_runtime.AttackFinished += HandleAttackFinished;
        }

        private void UnsubscribeRuntime()
        {
            if (m_runtime == null) return;

            m_runtime.AttackFinished -= HandleAttackFinished;
        }

        private void HandleAttackFinished()
        {
            if (!m_currentAttackContext.HasValue) return;

            AttackContext context = m_currentAttackContext.Value;
            ComboStep completedStep = context.Weapon.Combo.GetStep(context.ComboIndex);
            int stepCount = context.Weapon.Combo.StepCount;

            if (stepCount > 1)
            {
                m_comboIndex = (context.ComboIndex + 1) % stepCount;
                m_comboResetAt = Time.time + (completedStep?.ComboResetDelayInSeconds ?? 0f);
            }
            else
            {
                ResetCombo();
            }

            ClearAttackContextAndRestoreWeapon();
            AttackCompleted?.Invoke();
        }

        private static float GetEffectiveAttackDuration(ComboStep step, WeaponSO weapon)
        {
            float duration = step?.DurationInSeconds ?? 0.01f;
            return duration / Mathf.Max(0.01f, weapon.BaseAttackSpeed);
        }
    }
}
