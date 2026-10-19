using Zephyr.Contracts.Damage;

namespace Zephyr.Contracts.Interfaces
{
    public interface IDamageFilter
    {
        bool ShouldBlockDamage(DamageInfo damageInfo);
    }
}
