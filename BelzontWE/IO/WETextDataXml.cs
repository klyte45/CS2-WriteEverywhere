using Belzont.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;

#pragma warning disable IDE1006
using System.Xml.Serialization;
using UnityEngine;

namespace BelzontWE
{
    public class WETextDataXml : IEquatable<WETextDataXml>
    {
        public Vector3Xml offsetPosition = new();
        public Vector3Xml offsetRotation = new();
        public Vector3Xml scale = (Vector3Xml)Vector3.one;
        [XmlAttribute] public string itemName;
        [XmlAttribute] public WEShader shader;
        [XmlAttribute] public string text;
        [XmlAttribute] public string layoutName { get => text; set => text = value; }
        [XmlAttribute] public string imageName { get => text; set => text = value; }
        [XmlAttribute] public string atlas;
        [XmlAttribute] public WESimulationTextType textType;
        public WETextDataStyleXml style = new();
        [XmlAttribute] public string formulae;
        [XmlAttribute] public string fontName;
        [XmlAttribute][DefaultValue(0f)] public float maxWidthMeters;
        [XmlAttribute][DefaultValue(WETextData.DEFAULT_DECAL_FLAGS)] public int decalFlags = WETextData.DEFAULT_DECAL_FLAGS;

        public bool ShouldSerializeshader() => textType != WESimulationTextType.Placeholder;
        public bool ShouldSerializetext() => textType == WESimulationTextType.Text;
        public bool ShouldSerializeimageName() => textType == WESimulationTextType.Image;
        public bool ShouldSerializelayoutName() => textType == WESimulationTextType.Placeholder;
        public bool ShouldSerializeatlas() => textType == WESimulationTextType.Image;
        public bool ShouldSerializeformulae() => textType == WESimulationTextType.Text || textType == WESimulationTextType.Image;
        public bool ShouldSerializefontName() => textType == WESimulationTextType.Text;
        public bool ShouldSerializemaxWidthMeters() => textType == WESimulationTextType.Text;

        public override bool Equals(object obj) => Equals(obj as WETextDataXml);

        public bool Equals(WETextDataXml other) => other is not null &&
                   EqualityComparer<Vector3Xml>.Default.Equals(offsetPosition, other.offsetPosition) &&
                   EqualityComparer<Vector3Xml>.Default.Equals(offsetRotation, other.offsetRotation) &&
                   EqualityComparer<Vector3Xml>.Default.Equals(scale, other.scale) &&
                   itemName == other.itemName &&
                   shader == other.shader &&
                   text == other.text &&
                   layoutName == other.layoutName &&
                   imageName == other.imageName &&
                   atlas == other.atlas &&
                   textType == other.textType &&
                   EqualityComparer<WETextDataStyleXml>.Default.Equals(style, other.style) &&
                   formulae == other.formulae &&
                   fontName == other.fontName &&
                   maxWidthMeters == other.maxWidthMeters &&
                   decalFlags == other.decalFlags;

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(offsetPosition);
            hash.Add(offsetRotation);
            hash.Add(scale);
            hash.Add(itemName);
            hash.Add(shader);
            hash.Add(text);
            hash.Add(layoutName);
            hash.Add(imageName);
            hash.Add(atlas);
            hash.Add(textType);
            hash.Add(style);
            hash.Add(formulae);
            hash.Add(fontName);
            hash.Add(maxWidthMeters);
            hash.Add(decalFlags);
            return hash.ToHashCode();
        }

        public class WETextDataStyleXml : IEquatable<WETextDataStyleXml>
        {
            [XmlIgnore] public Color32 color;
            [XmlIgnore] public Color32 emissiveColor;
            [XmlAttribute][DefaultValue("00000000")] public string colorRGBA { get => color.ToRGBA(); set => color = ColorExtensions.FromRGBA(value); }
            [XmlAttribute][DefaultValue("00000000")] public string emissiveColorRGBA { get => emissiveColor.ToRGBA(); set => emissiveColor = ColorExtensions.FromRGBA(value); }
            [XmlAttribute][DefaultValue(0f)] public float metallic;
            [XmlAttribute][DefaultValue(0f)] public float smoothness;
            [XmlAttribute][DefaultValue(0f)] public float emissiveIntensity;
            [XmlAttribute][DefaultValue(0f)] public float emissiveExposureWeight;
            [XmlAttribute][DefaultValue(0f)] public float coatStrength;

            public override bool Equals(object obj) => Equals(obj as WETextDataStyleXml);

            public bool Equals(WETextDataStyleXml other) => other is not null &&
                       EqualityComparer<Color32>.Default.Equals(color, other.color) &&
                       EqualityComparer<Color32>.Default.Equals(emissiveColor, other.emissiveColor) &&
                       metallic == other.metallic &&
                       smoothness == other.smoothness &&
                       emissiveIntensity == other.emissiveIntensity &&
                       emissiveExposureWeight == other.emissiveExposureWeight &&
                       coatStrength == other.coatStrength;

            public override int GetHashCode() => HashCode.Combine(color, emissiveColor, metallic, smoothness, emissiveIntensity, emissiveExposureWeight, coatStrength);

            public static bool operator ==(WETextDataStyleXml left, WETextDataStyleXml right) => EqualityComparer<WETextDataStyleXml>.Default.Equals(left, right);

            public static bool operator !=(WETextDataStyleXml left, WETextDataStyleXml right) => !(left == right);
        }

        public static bool operator ==(WETextDataXml left, WETextDataXml right) => EqualityComparer<WETextDataXml>.Default.Equals(left, right);

        public static bool operator !=(WETextDataXml left, WETextDataXml right) => !(left == right);
    }
}