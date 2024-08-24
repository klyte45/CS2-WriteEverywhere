using Belzont.Utils;
using Colossal.Serialization.Entities;
using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace BelzontWE
{
    public unsafe struct WETextDataStruct : ISerializable
    {
        public const int CURRENT_VERSION = 1;
        public static readonly int SIZE = sizeof(WETextDataStruct);

        public bool IsInitialized { get; private set; }
        public float3 offsetPosition = default;
        public float3 offsetRotation = default;
        public float3 scale = Vector3.one;
        public FixedString32Bytes itemName = default;
        public FixedString512Bytes text = default;
        public FixedString32Bytes atlas = default;
        public WEShader shader = default;
        public WESimulationTextType textType = default;
        public WETextDataDefaultStyleStruct defaultStyle = default;
        public WETextDataGlassStyleStruct glassStyle = default;
        public FixedString512Bytes formulae = default;
        public FixedString32Bytes fontName = default;
        public float maxWidthMeters = default;
        public int decalFlags = WETextData.DEFAULT_DECAL_FLAGS;

        public WETextDataStruct()
        {
            IsInitialized = true;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(CURRENT_VERSION);
            writer.Write(offsetPosition);
            writer.Write(offsetRotation);
            writer.Write(scale);
            writer.Write(itemName);
            writer.Write(text);
            writer.Write(atlas);
            writer.Write((int)shader);
            writer.Write((int)textType);
            writer.Write(formulae);
            writer.Write(fontName);
            writer.Write(maxWidthMeters);
            writer.Write(decalFlags);
            switch (shader)
            {
                case WEShader.Default: writer.Write(defaultStyle); break;
                case WEShader.Glass: writer.Write(glassStyle); break;
            }
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out int version);
            if (version > CURRENT_VERSION)
            {
                LogUtils.DoWarnLog($"Invalid version for {GetType()}: {version}");
                return;
            }
            reader.Read(out offsetPosition);
            reader.Read(out offsetRotation);
            reader.Read(out scale);
            reader.Read(out itemName);
            reader.Read(out text);
            reader.Read(out atlas);
            reader.Read(out int shader); this.shader = (WEShader)shader;
            reader.Read(out int textType); this.textType = (WESimulationTextType)textType;
            if (version == 0)
            {
                reader.Read(out WETextDataStyleStruct oldStyle);
                defaultStyle = oldStyle.ToDefaultStyle();
                glassStyle = oldStyle.ToGlassStyle();
            }
            reader.Read(out formulae);
            reader.Read(out fontName);
            reader.Read(out maxWidthMeters);
            reader.Read(out decalFlags);
            if (version >= 1)
            {
                switch (this.shader)
                {
                    case WEShader.Default: reader.Read(out defaultStyle); break;
                    case WEShader.Glass: reader.Read(out glassStyle); break;
                }
            }
            IsInitialized = true;
        }

        internal static WETextDataStruct FromXml(WETextDataXml data)
            => data is null
                ? default
                : new WETextDataStruct
                {
                    IsInitialized = true,
                    offsetPosition = data.offsetPosition,
                    offsetRotation = data.offsetRotation,
                    scale = data.scale,
                    itemName = data.itemName ?? "",
                    text = data.text ?? "",
                    atlas = data.atlas ?? "",
                    shader = data.shader,
                    textType = data.textType,
                    glassStyle = WETextDataGlassStyleStruct.FromXml(data.glassStyle),
                    defaultStyle = WETextDataDefaultStyleStruct.FromXml(data.defaultStyle),
                    formulae = data.formulae ?? "",
                    fontName = data.fontName ?? "",
                    maxWidthMeters = data.maxWidthMeters,
                    decalFlags = data.decalFlags,

                };

        internal WETextDataXml ToXml() => new()
        {
            offsetPosition = (Vector3Xml)offsetPosition,
            offsetRotation = (Vector3Xml)offsetRotation,
            scale = (Vector3Xml)scale,
            itemName = itemName.ToString(),
            text = text.ToString(),
            atlas = atlas.ToString(),
            shader = shader,
            textType = textType,
            defaultStyle = defaultStyle.ToXml(),
            glassStyle = glassStyle.ToXml(),
            formulae = formulae.ToString(),
            fontName = fontName.ToString(),
            maxWidthMeters = maxWidthMeters,
            decalFlags = decalFlags,

        };
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
                coatStrength = coatStrength,
                color = color,
                colorMask1 = colorMask1,
                colorMask2 = colorMask2,
                colorMask3 = colorMask3,
                emissiveColor = emissiveColor,
                emissiveExposureWeight = emissiveExposureWeight,
                emissiveIntensity = emissiveIntensity,
                metallic = metallic,
                smoothness = smoothness
            };
            public WETextDataGlassStyleStruct ToGlassStyle() => new()
            {
                color = color,
                metallic = metallic,
                smoothness = smoothness,
                glassColor = glassColor,
                glassRefraction = glassRefraction,
                normalStrength = normalStrength,
                thickness = .5f
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

        public unsafe struct WETextDataDefaultStyleStruct : ISerializable
        {
            public const int CURRENT_VERSION = 0;
            public static readonly int SIZE = sizeof(WETextDataDefaultStyleStruct);
            public Color32 color;
            public Color32 emissiveColor;
            public float metallic;
            public float smoothness;
            public float emissiveIntensity;
            public float emissiveExposureWeight;
            public float coatStrength;
            public Color32 colorMask1;
            public Color32 colorMask2;
            public Color32 colorMask3;

            internal static WETextDataDefaultStyleStruct FromXml(WETextDataXml.WETextDataDefaultStyleXml style)
                => style is null
                    ? default
                    : new()
                    {
                        color = style.color,
                        emissiveColor = style.emissiveColor,
                        metallic = style.metallic,
                        smoothness = style.smoothness,
                        emissiveIntensity = style.emissiveIntensity,
                        emissiveExposureWeight = style.emissiveExposureWeight,
                        coatStrength = style.coatStrength,
                        colorMask1 = style.colorMask1,
                        colorMask2 = style.colorMask2,
                        colorMask3 = style.colorMask3,
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
                reader.Read(out metallic);
                reader.Read(out smoothness);
                reader.Read(out emissiveIntensity);
                reader.Read(out emissiveExposureWeight);
                reader.Read(out coatStrength);
                reader.Read(out colorMask1);
                reader.Read(out colorMask2);
                reader.Read(out colorMask3);
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
                color = color,
                emissiveColor = emissiveColor,
                metallic = metallic,
                smoothness = smoothness,
                emissiveIntensity = emissiveIntensity,
                emissiveExposureWeight = emissiveExposureWeight,
                coatStrength = coatStrength,
                colorMask1 = colorMask1,
                colorMask2 = colorMask2,
                colorMask3 = colorMask3,
            };
        }
        public unsafe struct WETextDataGlassStyleStruct : ISerializable
        {
            public const int CURRENT_VERSION = 0;
            public static readonly int SIZE = sizeof(WETextDataGlassStyleStruct);
            public Color32 color;
            public Color32 glassColor;
            public float glassRefraction;
            public float metallic;
            public float smoothness;
            public float normalStrength;
            public float thickness;

            internal static WETextDataGlassStyleStruct FromXml(WETextDataXml.WETextDataGlassStyleXml style)
                => style is null
                    ? default
                    : new()
                    {
                        color = style.color,
                        glassColor = style.glassColor,
                        glassRefraction = style.glassRefraction,
                        metallic = style.metallic,
                        smoothness = style.smoothness,
                        normalStrength = style.normalStrength,
                        thickness = style.thickness
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
                reader.Read(out glassColor);
                reader.Read(out glassRefraction);
                reader.Read(out metallic);
                reader.Read(out smoothness);
                reader.Read(out thickness);
                reader.Read(out normalStrength);
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
                color = color,
                glassColor = glassColor,
                glassRefraction = glassRefraction,
                metallic = metallic,
                smoothness = smoothness,
                normalStrength = normalStrength,
                thickness = thickness,
            };
        }
    }
}