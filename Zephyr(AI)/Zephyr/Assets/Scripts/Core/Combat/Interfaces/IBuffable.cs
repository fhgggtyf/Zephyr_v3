using Zephyr.Core.Buff;

namespace Zephyr.Core.Interfaces
{
    public interface IBuffable
    {
        BuffInstanceId AddBuff(BuffApplication application);
        bool RemoveBuff(BuffInstanceId id);
        void ClearAllBuffs();
        void ClearBuffsByTag(string tag);
        bool HasBuff(string tag);
    }
}
