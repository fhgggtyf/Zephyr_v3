using Zephyr.Core.Save;

namespace Zephyr.Core.Meta
{
    public interface IMetaGateway
    {
        MetaData Meta { get; }
        bool HasMeta { get; }
        void Save();
    }

    public sealed class MetaGateway : IMetaGateway
    {
        private readonly ISaveDataGateway _saveData;

        public MetaGateway(ISaveDataGateway saveData)
        {
            _saveData = saveData;
        }

        public MetaData Meta => _saveData?.Meta;
        public bool HasMeta => Meta != null;

        public void Save() => _saveData?.SaveMeta();
    }
}
