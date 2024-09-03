using Belzont.Utils;
using Colossal.OdinSerializer.Utilities;
using System;
using System.ComponentModel;


#pragma warning disable IDE1006
using System.Xml.Serialization;
using UnityEngine;

namespace BelzontWE
{
    public class WETextDataXml
    {
        public Vector3Xml offsetPosition = new();
        public Vector3Xml offsetRotation = new();
        public Vector3Xml scale = (Vector3Xml)Vector3.one;
        [XmlAttribute] public string itemName;
        [XmlAttribute] public WEShader shader;
        [XmlElement] public WETextDataFormulae<string> text;
        [XmlElement] public WETextDataFormulae<string> layoutName { get => text; set => text = value; }
        [XmlElement] public WETextDataFormulae<string> imageName { get => text; set => text = value; }

        [XmlAttribute] public WESimulationTextType textType;
        [XmlElement] public WETextDataDefaultStyleXml defaultStyle;
        [XmlElement] public WETextDataGlassStyleXml glassStyle;
        [XmlAttribute] public string atlas;
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
        public bool ShouldSerializestyle() => false;
        public bool ShouldSerializedefaultStyle() => shader == WEShader.Default;
        public bool ShouldSerializeglassStyle() => shader == WEShader.Glass;

        public class WETextDataDefaultStyleXml
        {
            [XmlElement] public WETextDataFormulaeColorRGBA color;
            [XmlElement] public WETextDataFormulaeColorRGBA emissiveColor;
            [XmlElement] public WETextDataFormulae<float> metallic;
            [XmlElement] public WETextDataFormulae<float> smoothness;
            [XmlElement] public WETextDataFormulae<float> emissiveIntensity;
            [XmlElement] public WETextDataFormulae<float> emissiveExposureWeight;
            [XmlElement] public WETextDataFormulae<float> coatStrength;
            [XmlElement] public WETextDataFormulaeColorRGB colorMask1 = new() { defaultValue = Color.white };
            [XmlElement] public WETextDataFormulaeColorRGB colorMask2 = new() { defaultValue = Color.white };
            [XmlElement] public WETextDataFormulaeColorRGB colorMask3 = new() { defaultValue = Color.white };
        }

        public class WETextDataGlassStyleXml
        {
            [XmlElement] public WETextDataFormulaeColorRGBA color;
            [XmlElement] public WETextDataFormulaeColorRGB glassColor;
            [XmlElement] public WETextDataFormulae<float> glassRefraction;
            [XmlElement] public WETextDataFormulae<float> metallic;
            [XmlElement] public WETextDataFormulae<float> smoothness;
            [XmlElement] public WETextDataFormulae<float> normalStrength;
            [XmlElement] public WETextDataFormulae<float> thickness = new() { defaultValue = .5f };
        }

        public class WETextDataFormulae<T>
        {
            [XmlAttribute] public T defaultValue;
            [XmlAttribute] public string formulae;
            public bool ShouldSerializeformulae() => !formulae.IsNullOrWhitespace();         
        }
        public class WETextDataFormulaeColorRGB
        {
            [XmlIgnore] public Color32 defaultValue;
            [XmlAttribute] public string defaultValueRGB { get => defaultValue.ToRGB(); set => defaultValue = ColorExtensions.FromRGB(value); }
            [XmlAttribute] public string formulae;
            public bool ShouldSerializeformulae() => !formulae.IsNullOrWhitespace();
        }
        public class WETextDataFormulaeColorRGBA
        {
            [XmlIgnore] public Color32 defaultValue;
            [XmlAttribute] public string defaultValueRGBA { get => defaultValue.ToRGBA(); set => defaultValue = ColorExtensions.FromRGBA(value); }
            [XmlAttribute] public string formulae;
            public bool ShouldSerializeformulae() => !formulae.IsNullOrWhitespace();
        }
    }
}