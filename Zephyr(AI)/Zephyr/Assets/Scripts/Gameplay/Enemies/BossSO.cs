using System;
using System.Collections.Generic;
using UnityEngine;
using Zephyr.Core;
using Zephyr.Core.DamageSystem;

namespace Zephyr.Gameplay.Enemies
{
    public enum BossAttackKind
    {
        Dash,
        HighJump,
        SpearThrowAndDash,
        CloseSlashCombo,
        CloseGroundStab
    }

    [Serializable]
    public sealed class BossHitboxSegment
    {
        [SerializeField] private string _id = "Hitbox";
        [Min(0f)] [SerializeField] private float _startTime;
        [Min(0f)] [SerializeField] private float _endTime = 0.2f;
        [SerializeField] private Vector2 _localOffset;
        [Min(0.01f)] [SerializeField] private Vector2 _size = new Vector2(1f, 1f);
        [Min(0f)] [SerializeField] private float _damageMultiplier = 1f;
        [SerializeField] private LayerMask _targetLayers = ~0;
        [SerializeField] private bool _damageOncePerTarget = true;
        [SerializeField] private bool _useSpearAnchor;
        [SerializeField] private bool _showGizmo = true;

        public string Id => string.IsNullOrWhiteSpace(_id) ? "Hitbox" : _id;
        public float StartTime => Mathf.Max(0f, _startTime);
        public float EndTime => Mathf.Max(StartTime, _endTime);
        public Vector2 LocalOffset => _localOffset;
        public Vector2 Size => new Vector2(Mathf.Max(0.01f, _size.x), Mathf.Max(0.01f, _size.y));
        public float DamageMultiplier => Mathf.Max(0f, _damageMultiplier);
        public LayerMask TargetLayers => _targetLayers;
        public bool DamageOncePerTarget => _damageOncePerTarget;
        public bool UseSpearAnchor => _useSpearAnchor;
        public bool ShowGizmo => _showGizmo;
    }

    [Serializable]
    public sealed class BossAttackDefinition
    {
        [SerializeField] private string _id = "Attack";
        [SerializeField] private BossAttackKind _kind;
        [SerializeField] private string _animationState = "Attack";
        [Min(0.1f)] [SerializeField] private float _selectionRange = 8f;
        [Min(0f)] [SerializeField] private float _sameLevelTolerance = 1.25f;
        [Min(0f)] [SerializeField] private float _telegraphDuration = 0.45f;
        [Min(0.05f)] [SerializeField] private float _totalDuration = 0.9f;
        [Min(0f)] [SerializeField] private float _damage = 20f;
        [Min(0f)] [SerializeField] private float _movementSpeed = 18f;
        [Min(0f)] [SerializeField] private float _jumpHorizontalSpeed = 4f;
        [Min(0f)] [SerializeField] private float _jumpVelocity = 17f;
        [Min(0f)] [SerializeField] private float _throwTime = 0.35f;
        [Min(0f)] [SerializeField] private float _dashToSpearTime = 0.55f;
        [SerializeField] private List<BossHitboxSegment> _hitboxes = new List<BossHitboxSegment>();

        public string Id => string.IsNullOrWhiteSpace(_id) ? "Attack" : _id;
        public BossAttackKind Kind => _kind;
        public string AnimationState => string.IsNullOrWhiteSpace(_animationState) ? "Attack" : _animationState;
        public float SelectionRange => Mathf.Max(0.1f, _selectionRange);
        public float SameLevelTolerance => Mathf.Max(0f, _sameLevelTolerance);
        public float TelegraphDuration => Mathf.Max(0f, _telegraphDuration);
        public float TotalDuration => Mathf.Max(TelegraphDuration + 0.01f, _totalDuration);
        public float Damage => Mathf.Max(0f, _damage);
        public float MovementSpeed => Mathf.Max(0f, _movementSpeed);
        public float JumpHorizontalSpeed => Mathf.Max(0f, _jumpHorizontalSpeed);
        public float JumpVelocity => Mathf.Max(0f, _jumpVelocity);
        public float ThrowTime => Mathf.Clamp(_throwTime, 0f, TotalDuration);
        public float DashToSpearTime => Mathf.Clamp(_dashToSpearTime, ThrowTime, TotalDuration);
        public IReadOnlyList<BossHitboxSegment> Hitboxes => _hitboxes;
    }

    [CreateAssetMenu(fileName = "Boss", menuName = "Zephyr/Enemies/Boss")]
    public sealed class BossSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _bossId = "boss";
        [SerializeField] private EnemySO _baseEnemy;

        [Header("Phases")]
        [Range(0.01f, 0.99f)] [SerializeField] private float _stageTwoHealthThreshold = 0.5f;
        [Min(0f)] [SerializeField] private float _stageOneAttackInterval = 2.5f;
        [Min(0f)] [SerializeField] private float _stageTwoAttackInterval = 1.5f;
        [Min(0f)] [SerializeField] private float _stageTwoSurroundDuration = 0.35f;
        [Min(0f)] [SerializeField] private float _stageTwoSurroundDamage = 1f;
        [SerializeField] private Vector2 _stageTwoSurroundSize = new Vector2(3f, 1.4f);
        [Min(0f)] [SerializeField] private float _stageTwoSurroundOffset = 1.5f;

        [Header("Potential attack")]
        [SerializeField] private StatType _targetPotential = StatType.MaxHp;
        [Min(0f)] [SerializeField] private float _potentialAttack = 1f;

        [Header("Attack timeline")]
        [SerializeField] private List<BossAttackDefinition> _attacks = new List<BossAttackDefinition>();

        public string BossId => string.IsNullOrWhiteSpace(_bossId) ? name : _bossId;
        public EnemySO BaseEnemy => _baseEnemy;
        public float StageTwoHealthThreshold => Mathf.Clamp(_stageTwoHealthThreshold, 0.01f, 0.99f);
        public float StageOneAttackInterval => Mathf.Max(0f, _stageOneAttackInterval);
        public float StageTwoAttackInterval => Mathf.Max(0f, _stageTwoAttackInterval);
        public float StageTwoSurroundDuration => Mathf.Max(0f, _stageTwoSurroundDuration);
        public float StageTwoSurroundDamage => Mathf.Max(0f, _stageTwoSurroundDamage);
        public Vector2 StageTwoSurroundSize => new Vector2(Mathf.Max(0.01f, _stageTwoSurroundSize.x), Mathf.Max(0.01f, _stageTwoSurroundSize.y));
        public float StageTwoSurroundOffset => Mathf.Max(0f, _stageTwoSurroundOffset);
        public StatType TargetPotential => _targetPotential;
        public float PotentialAttack => Mathf.Max(0f, _potentialAttack);
        public IReadOnlyList<BossAttackDefinition> Attacks => _attacks;
    }
}
