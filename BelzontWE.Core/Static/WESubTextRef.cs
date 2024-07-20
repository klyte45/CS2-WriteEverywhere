#define BURST
//#define VERBOSE 
using Belzont.Utils;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace BelzontWE
{
    public struct WESubTextRef : IBufferElementData, ISerializable
    {
        public const uint CURRENT_VERSION = 0;
        public Entity m_weTextData;

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out uint version);
            if (version > CURRENT_VERSION)
            {
                LogUtils.DoWarnLog($"Invalid version for {GetType()}: {version}");
                return;
            }
            reader.Read(out m_weTextData);
        }

       
        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(CURRENT_VERSION);
            writer.Write(m_weTextData);
        }
    }

}