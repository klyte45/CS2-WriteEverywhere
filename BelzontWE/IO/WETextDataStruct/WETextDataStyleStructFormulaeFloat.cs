using Belzont.Utils;
using Colossal.Serialization.Entities;
using Unity.Collections;

namespace BelzontWE
{
    public unsafe partial struct WETextDataStruct
    {
        public struct WETextDataStyleStructFormulaeFloat : ISerializable
        {
            public FixedString512Bytes formulae;
            public float defaultValue;

            internal static WETextDataStyleStructFormulaeFloat FromXml(WETextDataXml.WETextDataFormulae<float> src)
                => new()
                {
                    defaultValue = src.defaultValue,
                    formulae = src.formulae.ToString()
                };

            public void Deserialize<TReader>(TReader reader) where TReader : IReader
            {
                reader.Read(out formulae);
                reader.Read(out defaultValue);
            }

            public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
            {
                writer.Write(formulae);
                writer.Write(defaultValue);
            }

            public WETextDataXml.WETextDataFormulae<float> ToXml()
                => new()
                {
                    defaultValue = defaultValue,
                    formulae = formulae.ToString()
                };
        }
    }
}