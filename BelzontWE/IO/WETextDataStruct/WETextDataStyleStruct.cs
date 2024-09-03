using Belzont.Utils;
using Colossal.Serialization.Entities;
using System;
using UnityEngine;

namespace BelzontWE
{
    public unsafe partial struct WETextDataStruct
    {
        [Obsolete("Use specific class")]
        public unsafe struct WETextDataStyleStruct : ISerializable
        {
            public const int CURRENT_VERSION = 1;
            public static readonly int SIZE = sizeof(WETextDataStyleStruct);
            public Color32 color;
            public Color32 emissiveColor;
            public Color32 glassColor;
            public float glassRefraction;
            public float metallic;
            public float smoothness;
            public float emissiveIntensity;
            public float emissiveExposureWeight;
            public float coatStrength;
            public Color32 colorMask1;
            public Color32 colorMask2;
            public Color32 colorMask3;
            public float normalStrength;

            public WETextDataDefaultStyleStruct ToDefaultStyle() => new()
            {
                coatStrength = new() { defaultValue = coatStrength },
                color = new() { defaultValue = color },
                colorMask1 = new() { defaultValue = colorMask1 },
                colorMask2 = new() { defaultValue = colorMask2 },
                colorMask3 = new() { defaultValue = colorMask3 },
                emissiveColor = new() { defaultValue = emissiveColor },
                emissiveExposureWeight = new() { defaultValue = emissiveExposureWeight },
                emissiveIntensity = new() { defaultValue = emissiveIntensity },
                metallic = new() { defaultValue = metallic },
                smoothness = new() { defaultValue = smoothness }
            };
            public WETextDataGlassStyleStruct ToGlassStyle() => new()
            {
                color = new() { defaultValue = color },
                metallic = new() { defaultValue = metallic },
                smoothness = new() { defaultValue = smoothness },
                glassColor = new() { defaultValue = glassColor },
                glassRefraction = new() { defaultValue = glassRefraction },
                normalStrength = new() { defaultValue = normalStrength },
                thickness = new() { defaultValue = .5f }
            };
            public void Deserialize<TReader>(TReader reader) where TReader : IReader
            {
                reader.Read(out int version);
                if (version > CURRENT_VERSION)
                {
                    LogUtils.DoWarnLog($"Invalid version for {GetType()}: {version}");
                    return;
                }
                reader.Read(out color);
                reader.Read(out emissiveColor);
                reader.Read(out glassColor);
                reader.Read(out glassRefraction);
                reader.Read(out metallic);
                reader.Read(out smoothness);
                reader.Read(out emissiveIntensity);
                reader.Read(out emissiveExposureWeight);
                reader.Read(out coatStrength);
                if (version >= 1)
                {
                    reader.Read(out colorMask1);
                    reader.Read(out colorMask2);
                    reader.Read(out colorMask3);
                    reader.Read(out normalStrength);
                }
                else
                {
                    colorMask1 = Color.white;
                    colorMask2 = Color.white;
                    colorMask3 = Color.white;
                }
            }

            public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
            {
                writer.Write(CURRENT_VERSION);
                writer.Write(color);
                writer.Write(emissiveColor);
                writer.Write(glassColor);
                writer.Write(glassRefraction);
                writer.Write(metallic);
                writer.Write(smoothness);
                writer.Write(emissiveIntensity);
                writer.Write(emissiveExposureWeight);
                writer.Write(coatStrength);
                writer.Write(colorMask1);
                writer.Write(colorMask2);
                writer.Write(colorMask3);
                writer.Write(normalStrength);
            }
        }
    }
}