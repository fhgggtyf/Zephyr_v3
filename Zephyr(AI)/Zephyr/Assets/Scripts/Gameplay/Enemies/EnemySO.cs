using System.Collections.Generic;
using UnityEngine;
using Zephyr.Core;
using Zephyr.Core.DamageSystem;
using Zephyr.Core.Items;
using Zephyr.Core.StateMachine.ScriptableObjects;

namespace Zephyr.Gameplay.Enemies
{
    [CreateAssetMenu(fileName = "Enemy", menuName = "Zephyr/Enemies/Enemy")]
    public sealed class EnemySO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _enemyId;
        [SerializeField] private string[] _tags;
        [SerializeField] private TransitionTableSO _behaviourTable;
        [SerializeField] private DropTableSO _drops;

        [Header("Panel stats")]
        [Min(0f)] [SerializeField] private float _maxHp = 60f;
        [Min(0f)] [SerializeField] private float _attack = 12f;
        [Min(0f)] [SerializeField] private float _magicAttack;
        [Min(0f)] [SerializeField] private float _armor = 2f;
        [Min(0f)] [SerializeField] private float _magicResist = 2f;
        [Min(0f)] [SerializeField] private float _stamina;
        [Min(0f)] [SerializeField] private float _energy;
        [Min(0f)] [SerializeField] private float _attackSpeed = 1f;
        [Min(0f)] [SerializeField] private float _luck;
        [Min(0f)] [SerializeField] private float _tenacity;

        [Header("Behaviour / hidden tuning")]
        [Min(0f)] [SerializeField] private float _moveSpeed = 2f;
        [Min(0f)] [SerializeField] private float _patrolSpeed = 1.2f;
        [Min(0f)] [SerializeField] private float _patrolDistance = 2.5f;
        [Min(0f)] [SerializeField] private float _acceleration = 30f;
        [Min(0f)] [SerializeField] private float _aggroRange = 5f;
        [Min(0f)] [SerializeField] private float _detectionRange = 7f;
        [Min(0f)] [SerializeField] private float _alertDuration = 0.5f;
        [Min(0f)] [SerializeField] private float _attackRange = 1.1f;
        [Min(0f)] [SerializeField] private float _attackCooldown = 1.2f;
        [Min(0f)] [SerializeField] private float _attackWindup = 0.35f;
        [Min(0.01f)] [SerializeField] private float _attackDuration = 0.6f;
        [Min(0f)] [SerializeField] private float _attackActiveDuration = 0.18f;
        [Min(0f)] [SerializeField] private float _hurtDuration = 0.2f;
        [Min(0f)] [SerializeField] private float _loseAggroMultiplier = 1.5f;
        [Min(0f)] [SerializeField] private float _contactDamage = 12f;
        [SerializeField] private DamageType _damageType = DamageType.AD;
        [SerializeField] private WeaponCategory _damageCategory = WeaponCategory.Melee;

        [Header("Built-in attacks")]
        [SerializeField] private List<EnemyAttackDefinition> _attacks = new List<EnemyAttackDefinition>();
        [SerializeReference] private List<EnemyComponentData> _componentData = new List<EnemyComponentData>();

        [Header("Down / Potential attack")]
        [SerializeField] private StatType _targetPotential = StatType.MaxHp;
        [Min(0f)] [SerializeField] private float _potentialAttack = 1f;

        public string EnemyId => string.IsNullOrWhiteSpace(_enemyId) ? name : _enemyId;
        public string[] Tags => _tags;
        public TransitionTableSO BehaviourTable => _behaviourTable;
        public DropTableSO Drops => _drops;
        public float MaxHp => _maxHp;
        public float Attack => _attack;
        public float MagicAttack => _magicAttack;
        public float Armor => _armor;
        public float MagicResist => _magicResist;
        public float Stamina => _stamina;
        public float Energy => _energy;
        public float AttackSpeed => _attackSpeed;
        public float Luck => _luck;
        public float Tenacity => _tenacity;
        public float MoveSpeed => _moveSpeed;
        public float PatrolSpeed => _patrolSpeed;
        public float PatrolDistance => _patrolDistance;
        public float Acceleration => _acceleration;
        public float AggroRange => _aggroRange;
        public float DetectionRange => Mathf.Max(_aggroRange, _detectionRange);
        public float AlertDuration => _alertDuration;
        public IReadOnlyList<EnemyAttackDefinition> Attacks => _attacks;
        public IReadOnlyList<EnemyComponentData> Components => _componentData;
        public EnemyAttackDefinition GetAttack(int index)
        {
            if (_attacks == null || _attacks.Count == 0) return null;
            return _attacks[Mathf.Clamp(index, 0, _attacks.Count - 1)];
        }
        public float AttackRange => GetAttack(0)?.Range ?? _attackRange;
        public float AttackCooldown => GetAttack(0)?.Cooldown ?? _attackCooldown;
        public float AttackWindup => GetAttack(0)?.TelegraphDuration ?? _attackWindup;
        public float AttackDuration => GetAttack(0)?.TotalDuration ?? _attackDuration;
        public float AttackActiveDuration => GetAttack(0)?.ActiveDuration ?? Mathf.Min(_attackActiveDuration, _attackDuration);
        public float HurtDuration => _hurtDuration;
        public float LoseAggroMultiplier => Mathf.Max(1f, _loseAggroMultiplier);
        public float ContactDamage => _contactDamage;
        public DamageType DamageType => _damageType;
        public WeaponCategory DamageCategory => _damageCategory;
        public StatType TargetPotential => _targetPotential;
        public float PotentialAttack => _potentialAttack;

        private void OnValidate()
        {
            if (_attacks == null) _attacks = new List<EnemyAttackDefinition>();
            if (_componentData == null) _componentData = new List<EnemyComponentData>();
            foreach (EnemyComponentData data in _componentData)
                data?.SynchronizeAttackData(_attacks.Count);
        }
    }
}
