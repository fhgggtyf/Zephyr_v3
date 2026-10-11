using Zephyr.Core;

namespace Zephyr.Core.Stats
{
    public interface IMetaStatSource
    {
        float GetMetaStat(MetaStatType statType);
    }
}
