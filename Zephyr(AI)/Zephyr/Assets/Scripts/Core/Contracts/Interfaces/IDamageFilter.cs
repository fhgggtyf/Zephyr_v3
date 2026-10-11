using Zephyr.Core.DamageSystem;

namespace Zephyr.Core.Interfaces
{
    public interface IDamageFilter
    {
        bool ShouldBlockDamage(DamageInfo damageInfo);
    }
}
