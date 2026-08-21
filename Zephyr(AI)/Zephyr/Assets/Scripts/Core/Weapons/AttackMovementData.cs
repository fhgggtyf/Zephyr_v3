/*
 * AttackMovementData.cs
 * ---------------------
 * Module:  Core / Weapons
 * Purpose: Defines per-combo-step attack movement as locked, input-controlled, or
 *          a forward/backward surge with speed interpolation over the attack.
 */
using System;
using UnityEngine;

namespace Zephyr.Core.Weapons
{
    public enum AttackMovementMode
    {
        Locked,
        Input,
        SurgeForward,
        SurgeBackward,
        VariableInput
    }

    [Serializable]
    public class AttackMovementAttackData : AttackData
    {
        [SerializeField] private AttackMovementMode m_mode;
        [Min(0f)] [SerializeField] private float m_inputSpeed = 3f;
        [Min(0f)] [SerializeField] private float m_initialSpeed;
        [Min(0f)] [SerializeField] private float m_finalSpeed;

        public AttackMovementMode Mode => m_mode;
        public float InputSpeed => m_inputSpeed;
        public float InitialSpeed => m_initialSpeed;
        public float FinalSpeed => m_finalSpeed;
    }

    [Serializable]
    public class AttackMovementData : ComponentData<AttackMovementAttackData>
    {
        public override Type ComponentDependency => null;
    }
}
