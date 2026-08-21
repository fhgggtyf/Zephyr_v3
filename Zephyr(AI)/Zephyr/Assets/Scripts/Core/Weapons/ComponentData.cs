using System;
using UnityEngine;

namespace Zephyr.Core.Weapons
{
    [Serializable]
    public abstract class AttackData
    {
    }

    [Serializable]
    public abstract class ComponentData
    {
        /// <summary>
        /// Runtime component required by this data, or null when the data is consumed as pure configuration.
        /// </summary>
        public abstract Type ComponentDependency { get; }

        public virtual void SynchronizeAttackData(int attackCount)
        {
        }
    }

    [Serializable]
    public abstract class ComponentData<TAttackData> : ComponentData where TAttackData : AttackData, new()
    {
        [Tooltip("Use one data entry for every combo step.")]
        [SerializeField] private bool m_repeatData;
        [SerializeField] private TAttackData[] m_attackData = Array.Empty<TAttackData>();

        public TAttackData GetAttackData(int attackIndex)
        {
            if (m_attackData == null || m_attackData.Length == 0) return null;

            int index = m_repeatData ? 0 : Mathf.Clamp(attackIndex, 0, m_attackData.Length - 1);
            return m_attackData[index];
        }

        public override void SynchronizeAttackData(int attackCount)
        {
            int newLength = m_repeatData ? 1 : Mathf.Max(0, attackCount);
            int oldLength = m_attackData?.Length ?? 0;
            if (oldLength == newLength) return;

            Array.Resize(ref m_attackData, newLength);
            for (int i = oldLength; i < newLength; i++)
            {
                m_attackData[i] = new TAttackData();
            }
        }
    }
}
