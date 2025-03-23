using Belzont.Utils;
using Colossal.Serialization.Entities;
using Unity.Collections;
using Unity.Entities;

namespace BelzontWE
{
    public struct WETextDataVariable : IBufferElementData, ISerializable
    {
        private const uint VERSION = 0;
        private FixedString32Bytes key;
        private FixedString32Bytes value;

        public FixedString32Bytes Key { readonly get => key; set => key = value; }
        public FixedString32Bytes Value { readonly get => value; set => this.value = value; }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            EntitySerializableUtils.CheckVersionK45(reader, VERSION, GetType());
            reader.Read(out key);
            reader.Read(out value);
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(VERSION);
            writer.Write(key);
            writer.Write(value);
        }
    }
}