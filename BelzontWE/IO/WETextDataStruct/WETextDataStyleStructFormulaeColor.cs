using Belzont.Utils;
using Colossal.Serialization.Entities;
using System;
using Unity.Collections;
using UnityEngine;

namespace BelzontWE
{
    public unsafe partial struct WETextDataStruct
    {
        public struct WETextDataStyleStructFormulaeColor : ISerializable
        {
            public FixedString512Bytes formulae;
            public Color defaultValue;

            public static WETextDataStyleStructFormulaeColor FromXml(WETextDataXml.WETextDataFormulaeColorRGBA xml) => new()
            {
                formulae = xml?.formulae ?? "",
                defaultValue = xml?.defaultValue ?? default
            };

            public static WETextDataStyleStructFormulaeColor FromXml(WETextDataXml.WETextDataFormulaeColorRGB xml) => new()
            {
                formulae = xml?.formulae ?? "",
                defaultValue = xml?.defaultValue ?? default
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

            internal WETextDataXml.WETextDataFormulaeColorRGB ToXmlRGB() => new()
            {
                formulae = formulae.ToString(),
                defaultValue = defaultValue
            };

            internal WETextDataXml.WETextDataFormulaeColorRGBA ToXmlRGBA() => new()
            {
                formulae = formulae.ToString(),
                defaultValue = defaultValue
            };
        }
    }
}