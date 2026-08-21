using System;
using UnityEngine;

namespace Zephyr.Core.Weapons
{
    [Serializable]
    public class AttackHitBoxAttackData : AttackData
    {
        [SerializeField] private Vector2 m_localOffset;
        [SerializeField] private Vector2 m_size = Vector2.one;
        [SerializeField] private LayerMask m_layerMask = ~0;

        public Vector2 LocalOffset => m_localOffset;
        public Vector2 Size => m_size;
        public LayerMask LayerMask => m_layerMask;

    }

    [Serializable]
    public class AttackHitBoxData : ComponentData<AttackHitBoxAttackData>
    {
        public override Type ComponentDependency => typeof(MeleeHitBoxWeaponComponent);
    }
}
