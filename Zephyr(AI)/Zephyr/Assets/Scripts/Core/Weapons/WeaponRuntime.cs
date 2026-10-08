using System;
using System.Collections.Generic;
using UnityEngine;
using Zephyr.Core.Interfaces;

namespace Zephyr.Core.Weapons
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(WeaponGenerator))]
    public class WeaponRuntime : MonoBehaviour
    {
        private const string k_attackIndexParameter = "AttackIndex";
        private const string k_attackTriggerParameter = "Attack";

        [SerializeField] private Animator m_animator;
        [Tooltip("If true, flips child weapon sprite via SpriteRenderer.flipX; otherwise uses localScale.x.")]
        [SerializeField] private bool m_useFlipX = true;

        private WeaponGenerator m_generator;
        private SpriteRenderer[] m_weaponSprites;
        private WeaponSO m_equippedWeapon;

        private readonly Dictionary<Transform, float> m_weaponOriginalXPositions = new();

        public event Action<int> AttackStarted;
        public event Action AttackEnded;
        public event Action HitWindowOpened;
        public event Action HitWindowClosed;
        public event Action AttackFinished;

        public WeaponSO Weapon => CurrentAttackContext?.Weapon ?? m_equippedWeapon;
        public IStatSource StatSource { get; private set; }
        public IFacingProvider FacingProvider { get; private set; }
        public GameObject Owner { get; private set; }
        public int ComboIndex => CurrentAttackContext?.ComboIndex ?? 0;
        public bool IsAttacking { get; private set; }
        public AttackContext? CurrentAttackContext { get; private set; }

        private void Awake()
        {
            m_generator = GetComponent<WeaponGenerator>();
            if (m_animator == null) m_animator = GetComponentInChildren<Animator>();
            CacheWeaponSprites();
            CaptureVisualAnchors();
        }

        private void LateUpdate()
        {
            UpdateWeaponSpriteFacing();
        }

        public void Equip(WeaponSO weapon, GameObject owner, IStatSource statSource)
        {
            CancelAttack();
            m_equippedWeapon = weapon;
            Owner = owner;
            StatSource = statSource;
            FacingProvider = ResolveFacingProvider(owner);

            if (m_generator == null) m_generator = GetComponent<WeaponGenerator>();
            m_generator?.Generate(weapon);

            // Re-fetch animator/sprites in case WeaponGenerator created/destroyed child objects
            m_animator = GetComponentInChildren<Animator>();
            CacheWeaponSprites();
            CaptureVisualAnchors();
            ApplyInitialFacingFlip();

            if (m_animator != null)
            {
                m_animator.speed = 1f;
                m_animator.runtimeAnimatorController = weapon != null ? weapon.AnimatorController : null;
                m_animator.ResetTrigger(k_attackTriggerParameter);
                m_animator.SetInteger(k_attackIndexParameter, 0);
            }
        }

        public bool BeginAttack(AttackContext context)
        {
            if (context.Weapon == null || context.Weapon != m_equippedWeapon || IsAttacking) return false;

            CurrentAttackContext = context;
            IsAttacking = true;
            AttackStarted?.Invoke(context.ComboIndex);

            if (m_animator != null && m_animator.runtimeAnimatorController != null)
            {
                ComboStep step = context.Weapon.Combo.GetStep(context.ComboIndex);
                float durationInSeconds = context.Duration;
                float clipLength = step?.AnimationClip != null
                    ? step.AnimationClip.length
                    : durationInSeconds;
                m_animator.speed = clipLength / Mathf.Max(0.01f, durationInSeconds);
                m_animator.SetInteger(k_attackIndexParameter, context.ComboIndex);
                m_animator.SetTrigger(k_attackTriggerParameter);
            }

            return true;
        }

        public void EndAttack()
        {
            if (!IsAttacking) return;

            HitWindowClosed?.Invoke();
            IsAttacking = false;
            if (m_animator != null) m_animator.speed = 1f;
            AttackEnded?.Invoke();
        }

        public void CancelAttack()
        {
            EndAttack();
            CurrentAttackContext = null;
        }

        public void BeginHitWindow()
        {
            if (IsAttacking) HitWindowOpened?.Invoke();
        }

        public void EndHitWindow()
        {
            HitWindowClosed?.Invoke();
        }

        public void FinishAttack()
        {
            if (!IsAttacking) return;

            EndAttack();
            AttackFinished?.Invoke();
            CurrentAttackContext = null;
        }

        /// <summary>
        /// Caches all SpriteRenderers below this object so the weapon visual
        /// can be flipped in sync with the facing direction. Called on Awake
        /// and after each Equip (WeaponGenerator may rebuild children).
        /// </summary>
        private void CacheWeaponSprites()
        {
            m_weaponSprites = GetComponentsInChildren<SpriteRenderer>(true);
        }

        private void CaptureVisualAnchors()
        {
            m_weaponOriginalXPositions.Clear();
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                m_weaponOriginalXPositions.Add(child, Mathf.Abs(child.localPosition.x));
            }
        }

        /// <summary>
        /// Applies the current facing flip once after equip, so the weapon
        /// does not appear facing the wrong side before the next LateUpdate.
        /// </summary>
        private void ApplyInitialFacingFlip()
        {
            Vector2 facing = FacingProvider?.Facing ?? Vector2.right;
            bool facingRight = facing.x >= 0f;
            ApplyFlipToSprites(facingRight);
            ApplyAnchorMirror(facingRight);
        }

        /// <summary>
        /// Runs in LateUpdate every frame to mirror child weapon sprites and
        /// anchor positions. The Animator writes the "right-facing" animation
        /// pose to localPosition / SpriteRenderer each frame during Play, so
        /// we must re-apply the mirror every frame — otherwise only the last
        /// frame (after the Animator has stopped writing) would appear flipped.
        /// </summary>
        private void UpdateWeaponSpriteFacing()
        {
            if (FacingProvider == null) return;

            Vector2 facing = FacingProvider.Facing;
            bool facingRight = facing.x >= 0f;
            ApplyFlipToSprites(facingRight);
            ApplyAnchorMirror(facingRight);
        }

        /// <summary>
        /// Actually writes the flip state to every cached weapon SpriteRenderer.
        /// Supports two modes: SpriteRenderer.flipX or negative localScale.x.
        /// </summary>
        private void ApplyFlipToSprites(bool facingRight)
        {
            if (m_weaponSprites == null || m_weaponSprites.Length == 0) return;

            foreach (SpriteRenderer sr in m_weaponSprites)
            {
                if (sr == null) continue;

                if (m_useFlipX)
                {
                    sr.flipX = !facingRight;
                }
                else
                {
                    Vector3 scale = sr.transform.localScale;
                    scale.x = facingRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
                    sr.transform.localScale = scale;
                }
            }
        }

        private void ApplyAnchorMirror(bool facingRight)
        {
            float direction = facingRight ? 1f : -1f;

            foreach (KeyValuePair<Transform, float> weaponPosition in m_weaponOriginalXPositions)
            {
                Transform weaponTransform = weaponPosition.Key;
                if (weaponTransform == null) continue;

                Vector3 localPosition = weaponTransform.localPosition;
                localPosition.x = weaponPosition.Value * direction;
                weaponTransform.localPosition = localPosition;
            }
        }

        private static IFacingProvider ResolveFacingProvider(GameObject owner)
        {
            if (owner == null) return null;

            MonoBehaviour[] behaviours = owner.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IFacingProvider facingProvider) return facingProvider;
            }

            return null;
        }

    }
}
