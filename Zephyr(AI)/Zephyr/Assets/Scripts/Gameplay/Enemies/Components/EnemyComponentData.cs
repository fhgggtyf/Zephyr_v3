using System;
using UnityEngine;

namespace Zephyr.Gameplay.Enemies
{
    [Serializable]
    public abstract class EnemyAttackData
    {
    }

    /// <summary>Serializable configuration for one generated enemy component.</summary>
    [Serializable]
    public abstract class EnemyComponentData
    {
        public abstract Type ComponentDependency { get; }

        public virtual void SynchronizeAttackData(int attackCount)
        {
        }
    }

    [Serializable]
    public abstract class EnemyComponentData<TAttackData> : EnemyComponentData
        where TAttackData : EnemyAttackData, new()
    {
        [Tooltip("Reuse the first entry for every attack definition.")]
        [SerializeField] private bool _repeatData;
        [SerializeField] private TAttackData[] _attackData = Array.Empty<TAttackData>();

        public TAttackData GetAttackData(int attackIndex)
        {
            if (_attackData == null || _attackData.Length == 0) return null;
            int index = _repeatData ? 0 : Mathf.Clamp(attackIndex, 0, _attackData.Length - 1);
            return _attackData[index];
        }

        public override void SynchronizeAttackData(int attackCount)
        {
            int newLength = _repeatData ? 1 : Mathf.Max(0, attackCount);
            int oldLength = _attackData?.Length ?? 0;
            if (oldLength == newLength) return;

            Array.Resize(ref _attackData, newLength);
            for (int i = oldLength; i < newLength; i++)
                _attackData[i] = new TAttackData();
        }
    }
}
