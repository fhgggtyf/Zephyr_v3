using System;
using System.Collections.Generic;
using UnityEngine;
using Zephyr.Core.DamageSystem;
using Zephyr.Contracts.Damage;
using Zephyr.Core.Health;
using Zephyr.Contracts.Interfaces;
using Zephyr.Core.Stats;
using Zephyr.Core;

namespace Zephyr.Core.Buff
{
    public enum StackPolicy { Refresh, Stack, Ignore }
    public enum BuffStatLayer { PotentialScaledGain, RunModifier, TemporaryModifier }

    [Serializable]
    public struct BuffStatModifier
    {
        [SerializeField] private StatType _stat;
        [SerializeField] private float _value;
        [SerializeField] private StatModifierOperation _operation;
        [SerializeField] private BuffStatLayer _layer;

        public StatType Stat => _stat;
        public float Value => _value;
        public StatModifierOperation Operation => _operation;
        public BuffStatLayer Layer => _layer;
    }

    [Serializable]
    public struct BuffCategoryModifier
    {
        [SerializeField] private string _category;
        [SerializeField] private float _value;

        public string Category => _category;
        public float Value => _value;
    }

    [CreateAssetMenu(fileName = "Buff", menuName = "Zephyr/Buffs/Buff")]
    public sealed class BuffConfigSO : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _tag;
        [SerializeField] private StackPolicy _stackPolicy = StackPolicy.Refresh;
        [SerializeField, Min(1)] private int _maxStacks = 1;
        [SerializeField, Min(0f)] private float _duration;
        [SerializeField] private BuffStatModifier[] _statModifiers = Array.Empty<BuffStatModifier>();
        [SerializeField] private BuffCategoryModifier[] _categoryModifiers = Array.Empty<BuffCategoryModifier>();

        [Header("Optional Damage Over Time")]
        [SerializeField, Min(0f)] private float _tickInterval;
        [SerializeField, Min(0f)] private float _damagePerTick;
        [SerializeField] private DamageType _damageType;

        public string Id => string.IsNullOrWhiteSpace(_id) ? name : _id;
        public string Tag => _tag;
        public StackPolicy StackPolicy => _stackPolicy;
        public int MaxStacks => Mathf.Max(1, _maxStacks);
        public float Duration => _duration;
        public IReadOnlyList<BuffStatModifier> StatModifiers => _statModifiers;
        public IReadOnlyList<BuffCategoryModifier> CategoryModifiers => _categoryModifiers;
        public float TickInterval => _tickInterval;
        public float DamagePerTick => _damagePerTick;
        public DamageType DamageType => _damageType;
    }

    public readonly struct BuffInstanceId : IEquatable<BuffInstanceId>
    {
        public readonly int Value;
        public bool IsValid => Value > 0;

        public BuffInstanceId(int value) => Value = value;
        public bool Equals(BuffInstanceId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is BuffInstanceId other && Equals(other);
        public override int GetHashCode() => Value;
    }

    public readonly struct BuffApplication
    {
        public readonly BuffConfigSO Config;
        public readonly GameObject Source;

        public BuffApplication(BuffConfigSO config, GameObject source = null)
        {
            Config = config;
            Source = source;
        }
    }

    [DisallowMultipleComponent]
    public sealed class BuffController : MonoBehaviour, IBuffable, ITimerGatewayConsumer
    {
        private sealed class ActiveBuff
        {
            public BuffInstanceId Id;
            public BuffConfigSO Config;
            public GameObject Source;
            public int Stacks;
            public IDisposable Expiry;
            public IDisposable Tick;
        }

        [SerializeField] private StatsCore _stats;
        [SerializeField] private HealthComponent _health;
        private readonly Dictionary<BuffInstanceId, ActiveBuff> _active = new();
        private int _nextId = 1;
        private ITimerGateway _timer;

        public event Action<BuffInstanceId, BuffConfigSO> BuffAdded;
        public event Action<BuffInstanceId, BuffConfigSO> BuffRemoved;

        public void Configure(ITimerGateway timer) => _timer = timer;

        private void Awake()
        {
            _stats ??= GetComponentInParent<StatsCore>();
            _health ??= GetComponentInParent<HealthComponent>();
        }

        public BuffInstanceId AddBuff(BuffApplication application)
        {
            BuffConfigSO config = application.Config;
            if (config == null) return default;

            ActiveBuff existing = FindExisting(config, application.Source);
            if (existing != null)
            {
                if (config.StackPolicy == StackPolicy.Ignore) return existing.Id;
                if (config.StackPolicy == StackPolicy.Stack && existing.Stacks < config.MaxStacks)
                {
                    ApplyModifiers(config, 1f);
                    existing.Stacks++;
                }

                RestartTimers(existing);
                return existing.Id;
            }

            var buff = new ActiveBuff
            {
                Id = new BuffInstanceId(_nextId++),
                Config = config,
                Source = application.Source,
                Stacks = 1
            };
            _active.Add(buff.Id, buff);
            ApplyModifiers(config, 1f);
            RestartTimers(buff);
            BuffAdded?.Invoke(buff.Id, config);
            return buff.Id;
        }

        public bool RemoveBuff(BuffInstanceId id)
        {
            if (!_active.Remove(id, out ActiveBuff buff)) return false;
            buff.Expiry?.Dispose();
            buff.Tick?.Dispose();
            ApplyModifiers(buff.Config, -buff.Stacks);
            BuffRemoved?.Invoke(id, buff.Config);
            return true;
        }

        public void ClearAllBuffs()
        {
            var ids = new List<BuffInstanceId>(_active.Keys);
            for (int i = 0; i < ids.Count; i++) RemoveBuff(ids[i]);
        }

        public void ClearBuffsByTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return;
            var ids = new List<BuffInstanceId>();
            foreach (KeyValuePair<BuffInstanceId, ActiveBuff> pair in _active)
                if (string.Equals(pair.Value.Config.Tag, tag, StringComparison.OrdinalIgnoreCase)) ids.Add(pair.Key);
            for (int i = 0; i < ids.Count; i++) RemoveBuff(ids[i]);
        }

        public bool HasBuff(string tag)
        {
            foreach (ActiveBuff buff in _active.Values)
                if (string.Equals(buff.Config.Tag, tag, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private ActiveBuff FindExisting(BuffConfigSO config, GameObject source)
        {
            foreach (ActiveBuff buff in _active.Values)
                if (buff.Config == config && buff.Source == source) return buff;
            return null;
        }

        private void RestartTimers(ActiveBuff buff)
        {
            buff.Expiry?.Dispose();
            buff.Tick?.Dispose();
            ITimerGateway timer = _timer;
            if (timer == null) return;
            if (buff.Config.Duration > 0f)
                buff.Expiry = timer.SetTimeout(buff.Config.Duration, () => RemoveBuff(buff.Id));
            if (buff.Config.TickInterval > 0f && buff.Config.DamagePerTick > 0f)
                buff.Tick = timer.SetInterval(buff.Config.TickInterval, () => ApplyTick(buff));
        }

        private void ApplyTick(ActiveBuff buff)
        {
            if (!_active.ContainsKey(buff.Id) || _health == null) return;
            var info = new DamageInfo(buff.Config.DamagePerTick * buff.Stacks, buff.Config.DamageType,
                sourcePosition: buff.Source != null ? buff.Source.transform.position : transform.position,
                source: buff.Source);
            _health.TakeDamage(info);
        }

        private void ApplyModifiers(BuffConfigSO config, float stackDelta)
        {
            if (_stats == null) return;
            foreach (BuffStatModifier modifier in config.StatModifiers)
            {
                bool add = modifier.Value * stackDelta >= 0f;
                float amount = Mathf.Abs(modifier.Value * stackDelta);
                switch (modifier.Layer)
                {
                    case BuffStatLayer.PotentialScaledGain:
                        float scale = _stats.GetPotential(modifier.Stat) / StatConstants.PotentialMax;
                        if (modifier.Operation == StatModifierOperation.Multiply)
                        {
                            float scaledPercent = amount * scale;
                            if (add) _stats.AddRunGainPercent(modifier.Stat, scaledPercent);
                            else _stats.RemoveRunGainPercent(modifier.Stat, scaledPercent);
                        }
                        else if (add) _stats.ApplyPotentialScaledGain(modifier.Stat, amount);
                        else _stats.RemoveRunGain(modifier.Stat, amount * scale);
                        break;
                    case BuffStatLayer.RunModifier:
                        if (modifier.Operation == StatModifierOperation.Multiply)
                        {
                            if (add) _stats.AddModifierPercent(modifier.Stat, amount);
                            else _stats.RemoveModifierPercent(modifier.Stat, amount);
                        }
                        else if (add) _stats.AddModifier(modifier.Stat, amount);
                        else _stats.RemoveModifier(modifier.Stat, amount);
                        break;
                    default:
                        if (modifier.Operation == StatModifierOperation.Multiply)
                        {
                            if (add) _stats.AddTemporaryModifierPercent(modifier.Stat, amount);
                            else _stats.RemoveTemporaryModifierPercent(modifier.Stat, amount);
                        }
                        else if (add) _stats.AddTemporaryModifier(modifier.Stat, amount);
                        else _stats.RemoveTemporaryModifier(modifier.Stat, amount);
                        break;
                }
            }

            foreach (BuffCategoryModifier modifier in config.CategoryModifiers)
            {
                float value = modifier.Value * stackDelta;
                if (value >= 0f) _stats.AddCategoryMultiplier(modifier.Category, value);
                else _stats.RemoveCategoryMultiplier(modifier.Category, -value);
            }
        }

        private void OnDisable() => ClearAllBuffs();
    }
}
