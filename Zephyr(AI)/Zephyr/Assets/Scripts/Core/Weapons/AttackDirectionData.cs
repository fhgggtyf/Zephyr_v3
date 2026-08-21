/*
 * AttackDirectionData.cs
 * ----------------------
 * Module:  Core / Weapons
 * Purpose: Defines the direction used by each attack hitbox. Facing is the default;
 *          fixed directions allow attacks to point independently of player facing.
 */
using System;
using UnityEngine;

namespace Zephyr.Core.Weapons
{
    public enum AttackDirectionMode
    {
        Facing,
        Forward,
        Backward,
        Up,
        Down
    }

    [Serializable]
    public class AttackDirectionAttackData : AttackData
    {
        [SerializeField] private AttackDirectionMode m_mode = AttackDirectionMode.Facing;

        public AttackDirectionMode Mode => m_mode;
    }

    [Serializable]
    public class AttackDirectionData : ComponentData<AttackDirectionAttackData>
    {
        public override Type ComponentDependency => null;
    }
}
