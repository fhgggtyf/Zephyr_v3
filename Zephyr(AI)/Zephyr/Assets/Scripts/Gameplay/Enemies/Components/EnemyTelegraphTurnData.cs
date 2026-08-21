using System;

namespace Zephyr.Gameplay.Enemies
{
    /// <summary>
    /// Opts an enemy into continuous target-facing during Telegraph.
    /// Without this marker, Telegraph only faces the target once on entry.
    /// </summary>
    [Serializable]
    public sealed class EnemyTelegraphTurnData : EnemyComponentData
    {
        public override Type ComponentDependency => typeof(EnemyTelegraphTurnComponent);
    }
}
