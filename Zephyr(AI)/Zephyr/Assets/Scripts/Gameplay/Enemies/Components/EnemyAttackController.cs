using System;
using UnityEngine;
using Zephyr.Core;
using Zephyr.Core.DamageSystem;

namespace Zephyr.Gameplay.Enemies
{
    /// <summary>
    /// High-level runtime for attacks built into an enemy. State actions only
    /// start/cancel attacks; generated components implement the actual effects.
    /// </summary>
    [DefaultExecutionOrder(-340)]
    [DisallowMultipleComponent]
    public sealed class EnemyAttackController : MonoBehaviour
    {
        public event Action<int> AttackStarted;
        public event Action AttackEnded;
        public event Action HitWindowOpened;
        public event Action HitWindowClosed;

        private EnemyBrain _brain;
        private EnemyStats _stats;
        private float _attackStartedAt;
        private float _lastAttackAt = float.NegativeInfinity;
        private bool _hitWindowOpen;

        public int AttackIndex { get; private set; } = -1;
        public EnemyAttackDefinition CurrentAttack { get; private set; }
        public DamageInfo CurrentDamageInfo { get; private set; }
        public Vector2 AttackFacing { get; private set; } = Vector2.left;
        public bool IsAttacking { get; private set; }
        public bool AttackFinished { get; private set; } = true;

        public bool CanStartAttack
        {
            get
            {
                EnemyAttackDefinition attack = GetAttack(0);
                float cooldown = attack != null ? attack.Cooldown : (_stats != null ? _stats.AttackCooldown : 1f);
                return !IsAttacking && (_brain == null || !_brain.IsDead) && Time.time - _lastAttackAt >= cooldown;
            }
        }

        public void Initialize(EnemyBrain brain, EnemyStats stats)
        {
            _brain = brain != null ? brain : GetComponent<EnemyBrain>();
            _stats = stats != null ? stats : GetComponent<EnemyStats>();
        }

        public EnemyAttackDefinition GetAttack(int attackIndex)
        {
            if (_stats?.Config == null) return null;
            return _stats.Config.GetAttack(attackIndex);
        }

        public bool TryStartAttack(int attackIndex = 0)
        {
            if (IsAttacking || (_brain != null && _brain.IsDead)) return false;

            EnemyAttackDefinition attack = GetAttack(attackIndex);
            if (attack == null)
            {
                Debug.LogWarning("EnemyAttackController: EnemySO has no attack definition.", this);
                AttackFinished = true;
                _brain?.MarkAttackFinished();
                return false;
            }

            if (Time.time - _lastAttackAt < attack.Cooldown) return false;

            AttackIndex = attackIndex;
            CurrentAttack = attack;
            AttackFacing = _brain?.Motor != null ? _brain.Motor.Facing : Vector2.left;
            if (AttackFacing.sqrMagnitude <= 0.001f) AttackFacing = Vector2.left;

            float attackStat = _stats != null ? _stats.GetStatValue(StatType.Attack) : 0f;
            if (attackStat <= 0f && _stats != null) attackStat = _stats.AttackDamage;
            EnemySO config = _stats != null ? _stats.Config : null;
            CurrentDamageInfo = new DamageInfo(
                attackStat * attack.DamageMultiplier,
                config != null ? config.DamageType : DamageType.AD,
                sourcePosition: transform.position,
                source: gameObject,
                category: config != null ? config.DamageCategory : WeaponCategory.Melee);

            _attackStartedAt = Time.time;
            _lastAttackAt = Time.time;
            _hitWindowOpen = false;
            IsAttacking = true;
            AttackFinished = false;
            _brain?.MarkAttackStarted();
            AttackStarted?.Invoke(attackIndex);

            if (attack.ActiveDuration > 0f && attack.ActiveStartTime <= 0f)
                OpenHitWindow();

            return true;
        }

        public void CancelAttack()
        {
            if (!IsAttacking) return;
            FinishAttack();
        }

        public void Shutdown()
        {
            CancelAttack();
            _brain = null;
            _stats = null;
        }

        private void Update()
        {
            if (!IsAttacking || CurrentAttack == null) return;

            float elapsed = Time.time - _attackStartedAt;
            float activeEnd = CurrentAttack.ActiveStartTime + CurrentAttack.ActiveDuration;
            if (!_hitWindowOpen && CurrentAttack.ActiveDuration > 0f &&
                elapsed >= CurrentAttack.ActiveStartTime && elapsed < activeEnd)
                OpenHitWindow();

            if (_hitWindowOpen && elapsed >= activeEnd)
                CloseHitWindow();

            if (elapsed >= CurrentAttack.TotalDuration)
                FinishAttack();
        }

        private void OpenHitWindow()
        {
            if (_hitWindowOpen) return;
            _hitWindowOpen = true;
            HitWindowOpened?.Invoke();
        }

        private void CloseHitWindow()
        {
            if (!_hitWindowOpen) return;
            _hitWindowOpen = false;
            HitWindowClosed?.Invoke();
        }

        private void FinishAttack()
        {
            CloseHitWindow();
            IsAttacking = false;
            AttackFinished = true;
            _brain?.MarkAttackFinished();
            AttackEnded?.Invoke();
            AttackIndex = -1;
            CurrentAttack = null;
        }
    }
}
