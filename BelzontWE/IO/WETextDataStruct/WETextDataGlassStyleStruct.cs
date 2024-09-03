using Belzont.Utils;
using Colossal.Serialization.Entities;
using UnityEngine;

namespace BelzontWE
{
    public unsafe partial struct WETextDataStruct
    {
        public unsafe struct WETextDataGlassStyleStruct : ISerializable
        {
            public const int CURRENT_VERSION = 1;
            public static readonly int SIZE = sizeof(WETextDataGlassStyleStruct);
            public WETextDataStyleStructFormulaeColor color;
            public WETextDataStyleStructFormulaeColor glassColor;
            public WETextDataStyleStructFormulaeFloat glassRefraction;
            public WETextDataStyleStructFormulaeFloat metallic;
            public WETextDataStyleStructFormulaeFloat smoothness;
            public WETextDataStyleStructFormulaeFloat normalStrength;
            public WETextDataStyleStructFormulaeFloat thickness;

            internal static WETextDataGlassStyleStruct FromXml(WETextDataXml.WETextDataGlassStyleXml style)
                => style is null
                    ? default
                    : new()
                    {
                        color = WETextDataStyleStructFormulaeColor.FromXml(style.color),
                        glassColor = WETextDataStyleStructFormulaeColor.FromXml(style.glassColor),
                        glassRefraction = WETextDataStyleStructFormulaeFloat.FromXml(style.glassRefraction),
                        metallic = WETextDataStyleStructFormulaeFloat.FromXml(style.metallic),
                        smoothness = WETextDataStyleStructFormulaeFloat.FromXml(style.smoothness),
                        normalStrength = WETextDataStyleStructFormulaeFloat.FromXml(style.normalStrength),
                        thickness = WETextDataStyleStructFormulaeFloat.FromXml(style.thickness),
                    };

            public void Deserialize<TReader>(TReader reader) where TReader : IReader
            {
                reader.Read(out int version);
                if (version > CURRENT_VERSION)
                {
                    LogUtils.DoWarnLog($"Invalid version for {GetType()}: {version}");
                    return;
                }
                if (version == 0)
                {
                    reader.Read(out Color32 color);
                    reader.Read(out Color32 glassColor);
                    reader.Read(out float glassRefraction);
                    reader.Read(out float metallic);
                    reader.Read(out float smoothness);
                    reader.Read(out float thickness);
                    reader.Read(out float normalStrength);

                    this.color = new() { defaultValue = color };
                    this.glassColor = new() { defaultValue = glassColor };
                    this.glassRefraction = new() { defaultValue = glassRefraction };
                    this.metallic = new() { defaultValue = metallic };
                    this.smoothness = new() { defaultValue = smoothness };
                    this.thickness = new() { defaultValue = thickness };
                    this.normalStrength = new() { defaultValue = normalStrength };
                }
                else
                {
                    reader.Read(out color);
                    reader.Read(out glassColor);
                    reader.Read(out glassRefraction);
                    reader.Read(out metallic);
                    reader.Read(out smoothness);
                    reader.Read(out thickness);
                    reader.Read(out normalStrength);
                }
            }

            public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
            {
                writer.Write(CURRENT_VERSION);
                writer.Write(color);
                writer.Write(glassColor);
                writer.Write(glassRefraction);
                writer.Write(metallic);
                writer.Write(smoothness);
                writer.Write(thickness);
                writer.Write(normalStrength);
            }

            internal readonly WETextDataXml.WETextDataGlassStyleXml ToXml() => new()
            {
                color = color.ToXmlRGBA(),
                glassColor = glassColor.ToXmlRGB(),
                glassRefraction = glassRefraction.ToXml(),
                metallic = metallic.ToXml(),
                smoothness = smoothness.ToXml(),
                normalStrength = normalStrength.ToXml(),
                thickness = thickness.ToXml(),
            };
        }
    }
}