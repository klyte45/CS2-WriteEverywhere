using Belzont.Utils;
using Colossal.Serialization.Entities;
using UnityEngine;

namespace BelzontWE
{
    public unsafe partial struct WETextDataStruct
    {
        public unsafe struct WETextDataDefaultStyleStruct : ISerializable
        {
            public const int CURRENT_VERSION = 1;
            public static readonly int SIZE = sizeof(WETextDataDefaultStyleStruct);
            public WETextDataStyleStructFormulaeColor color;
            public WETextDataStyleStructFormulaeColor emissiveColor;
            public WETextDataStyleStructFormulaeFloat metallic;
            public WETextDataStyleStructFormulaeFloat smoothness;
            public WETextDataStyleStructFormulaeFloat emissiveIntensity;
            public WETextDataStyleStructFormulaeFloat emissiveExposureWeight;
            public WETextDataStyleStructFormulaeFloat coatStrength;
            public WETextDataStyleStructFormulaeColor colorMask1;
            public WETextDataStyleStructFormulaeColor colorMask2;
            public WETextDataStyleStructFormulaeColor colorMask3;

            internal static WETextDataDefaultStyleStruct FromXml(WETextDataXml.WETextDataDefaultStyleXml style)
                => style is null
                    ? default
                    : new()
                    {
                        color = WETextDataStyleStructFormulaeColor.FromXml(style.color),
                        emissiveColor = WETextDataStyleStructFormulaeColor.FromXml(style.emissiveColor),
                        metallic = WETextDataStyleStructFormulaeFloat.FromXml(style.metallic),
                        smoothness = WETextDataStyleStructFormulaeFloat.FromXml(style.smoothness),
                        emissiveIntensity = WETextDataStyleStructFormulaeFloat.FromXml(style.emissiveIntensity),
                        emissiveExposureWeight = WETextDataStyleStructFormulaeFloat.FromXml(style.emissiveExposureWeight),
                        coatStrength = WETextDataStyleStructFormulaeFloat.FromXml(style.coatStrength),
                        colorMask1 = WETextDataStyleStructFormulaeColor.FromXml(style.colorMask1),
                        colorMask2 = WETextDataStyleStructFormulaeColor.FromXml(style.colorMask2),
                        colorMask3 = WETextDataStyleStructFormulaeColor.FromXml(style.colorMask3),
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
                    reader.Read(out Color32 emissiveColor);
                    reader.Read(out float metallic);
                    reader.Read(out float smoothness);
                    reader.Read(out float emissiveIntensity);
                    reader.Read(out float emissiveExposureWeight);
                    reader.Read(out float coatStrength);
                    reader.Read(out Color32 colorMask1);
                    reader.Read(out Color32 colorMask2);
                    reader.Read(out Color32 colorMask3);

                    this.color = new() { defaultValue = color };
                    this.emissiveColor = new() { defaultValue = emissiveColor };
                    this.metallic = new() { defaultValue = metallic };
                    this.smoothness = new() { defaultValue = smoothness };
                    this.emissiveIntensity = new() { defaultValue = emissiveIntensity };
                    this.emissiveExposureWeight = new() { defaultValue = emissiveExposureWeight };
                    this.coatStrength = new() { defaultValue = coatStrength };
                    this.colorMask1 = new() { defaultValue = colorMask1 };
                    this.colorMask2 = new() { defaultValue = colorMask2 };
                    this.colorMask3 = new() { defaultValue = colorMask3 };
                }
                else
                {
                    reader.Read(out color);
                    reader.Read(out emissiveColor);
                    reader.Read(out metallic);
                    reader.Read(out smoothness);
                    reader.Read(out emissiveIntensity);
                    reader.Read(out emissiveExposureWeight);
                    reader.Read(out coatStrength);
                    reader.Read(out colorMask1);
                    reader.Read(out colorMask2);
                    reader.Read(out colorMask3);
                }
            }

            public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
            {
                writer.Write(CURRENT_VERSION);
                writer.Write(color);
                writer.Write(emissiveColor);
                writer.Write(metallic);
                writer.Write(smoothness);
                writer.Write(emissiveIntensity);
                writer.Write(emissiveExposureWeight);
                writer.Write(coatStrength);
                writer.Write(colorMask1);
                writer.Write(colorMask2);
                writer.Write(colorMask3);
            }

            internal readonly WETextDataXml.WETextDataDefaultStyleXml ToXml() => new()
            {
                color = color.ToXmlRGBA(),
                emissiveColor = emissiveColor.ToXmlRGBA(),
                metallic = metallic.ToXml(),
                smoothness = smoothness.ToXml(),
                emissiveIntensity = emissiveIntensity.ToXml(),
                emissiveExposureWeight = emissiveExposureWeight.ToXml(),
                coatStrength = coatStrength.ToXml(),
                colorMask1 = colorMask1.ToXmlRGB(),
                colorMask2 = colorMask2.ToXmlRGB(),
                colorMask3 = colorMask3.ToXmlRGB(),
            };
        }


    }
}