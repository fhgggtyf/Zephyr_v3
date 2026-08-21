/*
 * AttackFacingData.cs
 * -------------------
 * Module:  Core / Weapons
 * Purpose: Opts individual combo steps into changing the player's facing direction
 *          from attack input while the attack state is active.
 */
using System;
using UnityEngine;

namespace Zephyr.Core.Weapons
{
    [Serializable]
    public class AttackFacingAttackData : AttackData
    {
        [SerializeField] private bool m_canChangeFacing;

        public bool CanChangeFacing => m_canChangeFacing;
    }

    [Serializable]
    public class AttackFacingData : ComponentData<AttackFacingAttackData>
    {
        public override Type ComponentDependency => null;
    }
}
