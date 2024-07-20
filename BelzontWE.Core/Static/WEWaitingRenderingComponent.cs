#define BURST
//#define VERBOSE 
using Belzont.Utils;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace BelzontWE
{
    public struct WEWaitingRenderingComponent : ISerializable, IBufferElementData
    {
        private const uint CURRENT_VERSION = 1;
        public WESimulationTextComponent src;
        public int originalIdx;

        public static WEWaitingRenderingComponent From(WESimulationTextComponent src, int originalIdx)
        {
            var result = new WEWaitingRenderingComponent
            {
                src = src,
                originalIdx = originalIdx
            };
            return result;
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out uint version);
            if (version > CURRENT_VERSION)
            {
                LogUtils.DoWarnLog($"Invalid version for {GetType()}: {version}");
                return;
            }
            reader.Read(out src);
            if (version >= 1)
            {
                reader.Read(out originalIdx);
            }
        }

        public readonly void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(CURRENT_VERSION);
            writer.Write(src);
            writer.Write(originalIdx);
        }
    }

}