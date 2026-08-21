using System;

namespace Zephyr.Gameplay.Enemies
{
    /// <summary>
    /// Declares that this enemy uses a melee hitbox. Geometry is authored on
    /// EnemyMeleeHitboxComponent so it can be edited and previewed on the prefab.
    /// </summary>
    [Serializable]
    public sealed class EnemyMeleeHitboxData : EnemyComponentData
    {
        public override Type ComponentDependency => typeof(EnemyMeleeHitboxComponent);
    }
}
