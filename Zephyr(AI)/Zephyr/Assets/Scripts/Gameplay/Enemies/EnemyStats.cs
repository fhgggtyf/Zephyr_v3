using UnityEngine;
using Zephyr.Core;
using Zephyr.Core.DamageSystem;
using Zephyr.Core.Interfaces;
using Zephyr.Core.Stats;

namespace Zephyr.Gameplay.Enemies
{
    [DefaultExecutionOrder(-300)]
    [RequireComponent(typeof(StatsCore))]
    public sealed class EnemyStats : MonoBehaviour, IStatSource, IPotentialAttackSource
    {
        [SerializeField] private EnemySO _config;
        [Header("Runtime overrides (used when Config is empty)")]
        [Min(0f)] [SerializeField] private float _maxHp = 60f;
        [Min(0f)] [SerializeField] private float _attack = 12f;
        [Min(0f)] [SerializeField] private float _magicAttack;
        [Min(0f)] [SerializeField] private float _armor = 2f;
        [Min(0f)] [SerializeField] private float _magicResist = 2f;
        [Min(0f)] [SerializeField] private float _moveSpeed = 2f;
        [Min(0f)] [SerializeField] private float _patrolSpeed = 1.2f;
        [Min(0f)] [SerializeField] private float _patrolDistance = 2.5f;
        [Min(0f)] [SerializeField] private float _aggroRange = 5f;
        [Min(0f)] [SerializeField] private float _detectionRange = 7f;
        [Min(0f)] [SerializeField] private float _attackRange = 1.1f;
        [Min(0f)] [SerializeField] private float _attackCooldown = 1.2f;
        [Min(0.01f)] [SerializeField] private float _attackDuration = 0.6f;
        [Min(0f)] [SerializeField] private float _attackActiveDuration = 0.18f;
        [Min(0f)] [SerializeField] private float _contactDamage = 12f;
        [SerializeField] private StatType _targetPotential = StatType.MaxHp;
        [Min(0f)] [SerializeField] private float _potentialAttack = 1f;

        private StatsCore _stats;
        public EnemySO Config => _config;
        public float MaxHp => _config != null ? _config.MaxHp : _maxHp;
        public float MoveSpeed => _config != null ? _config.MoveSpeed : _moveSpeed;
        public float PatrolSpeed => _config != null ? _config.PatrolSpeed : _patrolSpeed;
        public float PatrolDistance => _config != null ? _config.PatrolDistance : _patrolDistance;
        public float Acceleration => _config != null ? _config.Acceleration : 30f;
        public float AggroRange => _config != null ? _config.AggroRange : _aggroRange;
        public float DetectionRange => _config != null ? _config.DetectionRange : Mathf.Max(_aggroRange, _detectionRange);
        public float AlertDuration => _config != null ? _config.AlertDuration : 0.5f;
        public float AttackRange => _config != null ? _config.AttackRange : _attackRange;
        public float AttackCooldown => _config != null ? _config.AttackCooldown : _attackCooldown;
        public float AttackDuration => _config != null ? _config.AttackDuration : _attackDuration;
        public float AttackActiveDuration => _config != null
            ? _config.AttackActiveDuration
            : Mathf.Min(_attackActiveDuration, _attackDuration);
        public float AttackWindup => _config != null ? _config.AttackWindup : 0.35f;
        public float HurtDuration => _config != null ? _config.HurtDuration : 0.2f;
        public float LoseAggroMultiplier => _config != null ? _config.LoseAggroMultiplier : 1.5f;
        public float ContactDamage => _config != null ? _config.ContactDamage : _contactDamage;
        public float AttackDamage => _config != null ? _config.Attack : _attack;
        public float MagicAttack => _config != null ? _config.MagicAttack : _magicAttack;
        public float Armor => _config != null ? _config.Armor : _armor;
        public float MagicResist => _config != null ? _config.MagicResist : _magicResist;
        public float Stamina => _config != null ? _config.Stamina : 0f;
        public float Energy => _config != null ? _config.Energy : 0f;
        public float AttackSpeed => _config != null ? _config.AttackSpeed : 1f;
        public float Luck => _config != null ? _config.Luck : 0f;
        public float Tenacity => _config != null ? _config.Tenacity : 0f;
        public DamageType DamageType => _config != null ? _config.DamageType : DamageType.AD;
        public WeaponCategory DamageCategory => _config != null ? _config.DamageCategory : WeaponCategory.Melee;
        public StatType TargetPotential => _config != null ? _config.TargetPotential : _targetPotential;
        public float PotentialAttack => _config != null ? _config.PotentialAttack : _potentialAttack;

        private void Awake()
        {
            _stats = GetComponent<StatsCore>();
            _stats.SetOwnerRole(StatsOwnerRole.Enemy);
            ApplyToStatsCore();
        }

        public void ApplyToStatsCore()
        {
            if (_stats == null) _stats = GetComponent<StatsCore>();
            if (_stats == null) return;
            _stats.SetBase(StatType.Attack, _config != null ? _config.Attack : _attack);
            _stats.SetBase(StatType.MagicAttack, _config != null ? _config.MagicAttack : _magicAttack);
            _stats.SetBase(StatType.Armor, _config != null ? _config.Armor : _armor);
            _stats.SetBase(StatType.MagicResist, _config != null ? _config.MagicResist : _magicResist);
            _stats.SetBase(StatType.MaxHp, MaxHp);
            _stats.SetBase(StatType.Stamina, _config != null ? _config.Stamina : 0f);
            _stats.SetBase(StatType.Energy, _config != null ? _config.Energy : 0f);
            _stats.SetBase(StatType.Luck, _config != null ? _config.Luck : 0f);
            _stats.SetBase(StatType.Tenacity, _config != null ? _config.Tenacity : 0f);
            _stats.SetBase(StatType.AttackSpeed, _config != null ? _config.AttackSpeed : 1f);
        }

        public float GetStatValue(StatType statType) => _stats != null ? _stats.GetStatValue(statType) : 0f;
        public float GetPotential(StatType statType) => GameConstants.Stats.PotentialMax;
    }
}
