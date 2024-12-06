﻿using Belzont.Serialization;
using Belzont.Utils;
using Colossal.OdinSerializer.Utilities;
using Colossal.Serialization.Entities;
using System.Collections.Generic;
using System.ComponentModel;


#pragma warning disable IDE1006
using System.Xml.Serialization;
using Unity.Mathematics;
using UnityEngine;

namespace BelzontWE
{
    public class WETextDataXml : ISerializable
    {
        private const int CURRENT_VERSION = 0;
        [XmlAttribute] public string itemName;

        [XmlElement] public TransformXml transform;
        [XmlElement][DefaultValue(null)] public MeshDataTextXml textMesh;
        [XmlElement][DefaultValue(null)] public MeshDataImageXml imageMesh;
        [XmlElement][DefaultValue(null)] public MeshDataPlaceholderXml layoutMesh;
        [XmlElement][DefaultValue(null)] public MeshDataWhiteTextureXml whiteMesh;
        [XmlElement] public DefaultStyleXml defaultStyle;
        [XmlElement] public GlassStyleXml glassStyle;

        internal WESimulationTextType EffectiveTextType => textMesh?.textType
            ?? imageMesh?.textType
            ?? layoutMesh?.textType
            ?? whiteMesh?.textType
            ?? WESimulationTextType.Archetype;

        public bool ShouldSerializetextMesh() => textMesh != null;
        public bool ShouldSerializeimageMesh() => imageMesh != null;
        public bool ShouldSerializelayoutMesh() => layoutMesh != null;
        public bool ShouldSerializewhiteMesh() => whiteMesh != null;
        public bool ShouldSerializedefaultStyle() => layoutMesh is null && defaultStyle != null;
        public bool ShouldSerializeglassStyle() => layoutMesh is null && glassStyle != null;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(CURRENT_VERSION);
            writer.Write(itemName ?? "");
            writer.WriteNullCheck(transform);
            writer.WriteNullCheck(textMesh);
            writer.WriteNullCheck(imageMesh);
            writer.WriteNullCheck(layoutMesh);
            writer.WriteNullCheck(whiteMesh);
            writer.WriteNullCheck(defaultStyle);
            writer.WriteNullCheck(glassStyle);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out int version);
            if (version > CURRENT_VERSION)
            {
                LogUtils.DoWarnLog($"Invalid version for {GetType()}: {version}");
                return;
            }
            reader.Read(out itemName);
            reader.ReadNullCheck(out transform);
            reader.ReadNullCheck(out textMesh);
            reader.ReadNullCheck(out imageMesh);
            reader.ReadNullCheck(out layoutMesh);
            reader.ReadNullCheck(out whiteMesh);
            reader.ReadNullCheck(out defaultStyle);
            reader.ReadNullCheck(out glassStyle);

        }

        internal void MapFontAndAtlases(HashSet<string> dictAtlases, HashSet<string> dictFonts)
        {
            if (imageMesh != null && imageMesh.atlas.TrimToNull() != null)
            {
                dictAtlases.Add(imageMesh.atlas);
            }
            if (textMesh != null && textMesh.fontName.TrimToNull() != null)
            {
                dictFonts.Add(textMesh.fontName);
            }

        }

        public class TransformXml : ISerializable
        {
            private const int CURRENT_VERSION = 1;
            public Vector3Xml offsetPosition = new();
            public Vector3Xml offsetRotation = new();
            public Vector3Xml scale = (Vector3Xml)Vector3.one;
            [XmlAttribute][DefaultValue(false)] public bool isAbsoluteScale;

            public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
            {
                writer.Write(CURRENT_VERSION);
                writer.Write((Vector3)offsetPosition);
                writer.Write((Vector3)offsetRotation);
                writer.Write((Vector3)scale);
                writer.Write(isAbsoluteScale);
            }
            public void Deserialize<TReader>(TReader reader) where TReader : IReader
            {
                reader.Read(out int version);
                if (version > CURRENT_VERSION)
                {
                    LogUtils.DoWarnLog($"Invalid version for {GetType()}: {version}");
                    return;
                }
                reader.Read(out float3 offsetPosition);
                reader.Read(out float3 offsetRotation);
                reader.Read(out float3 scale);

                this.offsetPosition = (Vector3Xml)(Vector3)offsetPosition;
                this.offsetRotation = (Vector3Xml)(Vector3)offsetRotation;
                this.scale = (Vector3Xml)(Vector3)scale;
                if (version >= 1)
                {
                    reader.Read(out isAbsoluteScale);
                }
            }
        }

        public class MeshDataWhiteTextureXml : ISerializable
        {
            private const int CURRENT_VERSION = 0;
            [XmlIgnore] public WESimulationTextType textType => WESimulationTextType.WhiteTexture;
            public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
            {
                writer.Write(CURRENT_VERSION);
            }
            public void Deserialize<TReader>(TReader reader) where TReader : IReader
            {
                reader.Read(out int version);
                if (version > CURRENT_VERSION)
                {
                    LogUtils.DoWarnLog($"Invalid version for {GetType()}: {version}");
                    return;
                }
            }
        }
        public class MeshDataTextXml : ISerializable
        {
            private const int CURRENT_VERSION = 0;
            [XmlIgnore] public WESimulationTextType textType => WESimulationTextType.Text;
            [XmlAttribute] public string fontName;
            [XmlElement] public FormulaeStringXml text;
            [XmlAttribute][DefaultValue(0f)] public float maxWidthMeters;
            public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
            {
                writer.Write(CURRENT_VERSION);
                writer.Write(fontName ?? "");
                writer.WriteNullCheck(text);
                writer.Write(maxWidthMeters);
            }
            public void Deserialize<TReader>(TReader reader) where TReader : IReader
            {
                reader.Read(out int version);
                if (version > CURRENT_VERSION)
                {
                    LogUtils.DoWarnLog($"Invalid version for {GetType()}: {version}");
                    return;
                }
                reader.Read(out fontName);
                reader.ReadNullCheck(out text);
                reader.Read(out maxWidthMeters);
            }
        }
        public class MeshDataImageXml : ISerializable
        {
            private const int CURRENT_VERSION = 0;
            [XmlIgnore] public WESimulationTextType textType => WESimulationTextType.Image;
            [XmlAttribute] public string atlas;
            [XmlElement] public FormulaeStringXml image;
            public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
            {
                writer.Write(CURRENT_VERSION);
                writer.Write(atlas ?? "");
                writer.WriteNullCheck(image);
            }
            public void Deserialize<TReader>(TReader reader) where TReader : IReader
            {
                reader.Read(out int version);
                if (version > CURRENT_VERSION)
                {
                    LogUtils.DoWarnLog($"Invalid version for {GetType()}: {version}");
                    return;
                }
                reader.Read(out atlas);
                reader.ReadNullCheck(out image);
            }
        }
        public class MeshDataPlaceholderXml : ISerializable
        {
            private const int CURRENT_VERSION = 0;
            [XmlIgnore] public WESimulationTextType textType => WESimulationTextType.Placeholder;
            [XmlElement] public FormulaeStringXml layout;
            public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
            {
                writer.Write(CURRENT_VERSION);
                writer.WriteNullCheck(layout);
            }
            public void Deserialize<TReader>(TReader reader) where TReader : IReader
            {
                reader.Read(out int version);
                if (version > CURRENT_VERSION)
                {
                    LogUtils.DoWarnLog($"Invalid version for {GetType()}: {version}");
                    return;
                }
                reader.ReadNullCheck(out layout);
            }
        }

        public class DefaultStyleXml : ISerializable
        {
            private const int CURRENT_VERSION = 0;
            [XmlIgnore] public WEShader shader => WEShader.Default;
            [XmlAttribute][DefaultValue(WETextDataMaterial.DEFAULT_DECAL_FLAGS)] public int decalFlags = WETextDataMaterial.DEFAULT_DECAL_FLAGS;
            [XmlElement] public FormulaeColorRgbaXml color = new() { defaultValue = Color.white };
            [XmlElement] public FormulaeColorRgbaXml emissiveColor = new() { defaultValue = Color.white };
            [XmlElement] public FormulaeFloatXml metallic;
            [XmlElement] public FormulaeFloatXml smoothness;
            [XmlElement] public FormulaeFloatXml emissiveIntensity;
            [XmlElement] public FormulaeFloatXml emissiveExposureWeight;
            [XmlElement] public FormulaeFloatXml coatStrength;
            [XmlElement] public FormulaeColorRgbXml colorMask1 = new() { defaultValue = Color.white };
            [XmlElement] public FormulaeColorRgbXml colorMask2 = new() { defaultValue = Color.white };
            [XmlElement] public FormulaeColorRgbXml colorMask3 = new() { defaultValue = Color.white };
            public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
            {
                writer.Write(CURRENT_VERSION);
                writer.Write(decalFlags);
                writer.WriteNullCheck(color);
                writer.WriteNullCheck(emissiveColor);
                writer.WriteNullCheck(metallic);
                writer.WriteNullCheck(smoothness);
                writer.WriteNullCheck(emissiveIntensity);
                writer.WriteNullCheck(emissiveExposureWeight);
                writer.WriteNullCheck(coatStrength);
                writer.WriteNullCheck(colorMask1);
                writer.WriteNullCheck(colorMask2);
                writer.WriteNullCheck(colorMask3);
            }
            public void Deserialize<TReader>(TReader reader) where TReader : IReader
            {
                reader.Read(out int version);
                if (version > CURRENT_VERSION)
                {
                    LogUtils.DoWarnLog($"Invalid version for {GetType()}: {version}");
                    return;
                }
                reader.Read(out decalFlags);
                reader.ReadNullCheck(out color);
                reader.ReadNullCheck(out emissiveColor);
                reader.ReadNullCheck(out metallic);
                reader.ReadNullCheck(out smoothness);
                reader.ReadNullCheck(out emissiveIntensity);
                reader.ReadNullCheck(out emissiveExposureWeight);
                reader.ReadNullCheck(out coatStrength);
                reader.ReadNullCheck(out colorMask1);
                reader.ReadNullCheck(out colorMask2);
                reader.ReadNullCheck(out colorMask3);
            }
        }

        public class GlassStyleXml : ISerializable
        {
            private const int CURRENT_VERSION = 0;
            [XmlIgnore] public WEShader shader => WEShader.Glass;
            [XmlAttribute][DefaultValue(WETextDataMaterial.DEFAULT_DECAL_FLAGS)] public int decalFlags = WETextDataMaterial.DEFAULT_DECAL_FLAGS;
            [XmlElement] public FormulaeColorRgbaXml color = new() { defaultValue = Color.clear };
            [XmlElement] public FormulaeColorRgbXml glassColor = new() { defaultValue = Color.white };
            [XmlElement] public FormulaeFloatXml glassRefraction = new() { defaultValue = 1 };
            [XmlElement] public FormulaeFloatXml metallic;
            [XmlElement] public FormulaeFloatXml smoothness;
            [XmlElement] public FormulaeFloatXml normalStrength;
            [XmlElement] public FormulaeFloatXml glassThickness = new() { defaultValue = .5f };
            public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
            {
                writer.Write(CURRENT_VERSION);
                writer.WriteNullCheck(color);
                writer.WriteNullCheck(glassColor);
                writer.WriteNullCheck(glassRefraction);
                writer.WriteNullCheck(metallic);
                writer.WriteNullCheck(smoothness);
                writer.WriteNullCheck(normalStrength);
                writer.WriteNullCheck(glassThickness);
            }
            public void Deserialize<TReader>(TReader reader) where TReader : IReader
            {
                reader.Read(out int version);
                if (version > CURRENT_VERSION)
                {
                    LogUtils.DoWarnLog($"Invalid version for {GetType()}: {version}");
                    return;
                }
                reader.Read(out decalFlags);
                reader.ReadNullCheck(out color);
                reader.ReadNullCheck(out glassColor);
                reader.ReadNullCheck(out glassRefraction);
                reader.ReadNullCheck(out metallic);
                reader.ReadNullCheck(out smoothness);
                reader.ReadNullCheck(out normalStrength);
                reader.ReadNullCheck(out glassThickness);
            }
        }

        public class FormulaeStringXml : ISerializable
        {
            private const int CURRENT_VERSION = 0;
            [XmlText] public string defaultValue;
            [XmlAttribute] public string formulae;
            public bool ShouldSerializeformulae() => !formulae.IsNullOrWhitespace();
            public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
            {
                writer.Write(CURRENT_VERSION);
                writer.Write(defaultValue ?? "");
                writer.Write(formulae ?? "");
            }
            public void Deserialize<TReader>(TReader reader) where TReader : IReader
            {
                reader.Read(out int version);
                if (version > CURRENT_VERSION)
                {
                    LogUtils.DoWarnLog($"Invalid version for {GetType()}: {version}");
                    return;
                }
                reader.Read(out defaultValue);
                reader.Read(out formulae);
            }
        }
        public class FormulaeFloatXml : ISerializable
        {
            private const int CURRENT_VERSION = 0;
            [XmlText] public float defaultValue;
            [XmlAttribute] public string formulae;
            public bool ShouldSerializeformulae() => !formulae.IsNullOrWhitespace();
            public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
            {
                writer.Write(CURRENT_VERSION);
                writer.Write(defaultValue);
                writer.Write(formulae ?? "");
            }
            public void Deserialize<TReader>(TReader reader) where TReader : IReader
            {
                reader.Read(out int version);
                if (version > CURRENT_VERSION)
                {
                    LogUtils.DoWarnLog($"Invalid version for {GetType()}: {version}");
                    return;
                }
                reader.Read(out defaultValue);
                reader.Read(out formulae);
            }
        }
        public class FormulaeColorRgbXml : ISerializable
        {
            private const int CURRENT_VERSION = 0;
            [XmlIgnore] public Color32 defaultValue;
            [XmlText] public string defaultValueRGB { get => defaultValue.ToRGB(); set => defaultValue = ColorExtensions.FromRGB(value); }
            [XmlAttribute] public string formulae;
            public bool ShouldSerializeformulae() => !formulae.IsNullOrWhitespace();
            public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
            {
                writer.Write(CURRENT_VERSION);
                writer.Write(defaultValue);
                writer.Write(formulae ?? "");
            }
            public void Deserialize<TReader>(TReader reader) where TReader : IReader
            {
                reader.Read(out int version);
                if (version > CURRENT_VERSION)
                {
                    LogUtils.DoWarnLog($"Invalid version for {GetType()}: {version}");
                    return;
                }
                reader.Read(out defaultValue);
                reader.Read(out formulae);
            }
        }
        public class FormulaeColorRgbaXml : ISerializable
        {
            private const int CURRENT_VERSION = 0;
            [XmlIgnore] public Color32 defaultValue;
            [XmlText] public string defaultValueRGBA { get => defaultValue.ToRGBA(); set => defaultValue = ColorExtensions.FromRGBA(value); }
            [XmlAttribute] public string formulae;
            public bool ShouldSerializeformulae() => !formulae.IsNullOrWhitespace();
            public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
            {
                writer.Write(CURRENT_VERSION);
                writer.Write(defaultValue);
                writer.Write(formulae ?? "");
            }
            public void Deserialize<TReader>(TReader reader) where TReader : IReader
            {
                reader.Read(out int version);
                if (version > CURRENT_VERSION)
                {
                    LogUtils.DoWarnLog($"Invalid version for {GetType()}: {version}");
                    return;
                }
                reader.Read(out defaultValue);
                reader.Read(out formulae);
            }
        }
    }
}
