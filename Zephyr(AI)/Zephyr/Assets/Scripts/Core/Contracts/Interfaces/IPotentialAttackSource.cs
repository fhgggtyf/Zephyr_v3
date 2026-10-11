using Zephyr.Core;

namespace Zephyr.Core.Interfaces
{
    /// <summary>
    /// Describes the Potential penalty an attack source applies when it downs a player.
    /// The source carries data only; the player's down/respawn flow performs the loss.
    /// </summary>
    public interface IPotentialAttackSource
    {
        StatType TargetPotential { get; }
        float PotentialAttack { get; }
    }
}
