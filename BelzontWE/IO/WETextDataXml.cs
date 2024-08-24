﻿using Belzont.Utils;
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
        [XmlAttribute] public string text;
        [XmlAttribute] public string layoutName { get => text; set => text = value; }
        [XmlAttribute] public string imageName { get => text; set => text = value; }
        [XmlAttribute] public string atlas;
        [XmlAttribute] public WESimulationTextType textType;
        [XmlElement] public WETextDataDefaultStyleXml defaultStyle;
        [XmlElement] public WETextDataGlassStyleXml glassStyle;
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
        public bool ShouldSerializestyle() => false;
        public bool ShouldSerializedefaultStyle() => shader == WEShader.Default;
        public bool ShouldSerializeglassStyle() => shader == WEShader.Glass;

        public class WETextDataDefaultStyleXml
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
            [XmlIgnore] public Color32 colorMask1 = Color.white;
            [XmlIgnore] public Color32 colorMask2 = Color.white;
            [XmlIgnore] public Color32 colorMask3 = Color.white;
            [XmlAttribute][DefaultValue("FFFFFF")] public string colorMask1RGB { get => colorMask1.ToRGB(); set => colorMask1 = ColorExtensions.FromRGB(value); }
            [XmlAttribute][DefaultValue("FFFFFF")] public string colorMask2RGB { get => colorMask2.ToRGB(); set => colorMask2 = ColorExtensions.FromRGB(value); }
            [XmlAttribute][DefaultValue("FFFFFF")] public string colorMask3RGB { get => colorMask3.ToRGB(); set => colorMask3 = ColorExtensions.FromRGB(value); }
        }

        public class WETextDataGlassStyleXml
        {
            [XmlIgnore] public Color32 color;
            [XmlIgnore] public Color32 glassColor;
            [XmlAttribute][DefaultValue("00000000")] public string colorRGBA { get => color.ToRGBA(); set => color = ColorExtensions.FromRGBA(value); }
            [XmlAttribute][DefaultValue("000000")] public string glassColorRGB { get => glassColor.ToRGB(); set => glassColor = ColorExtensions.FromRGB(value); }
            [XmlAttribute][DefaultValue(0f)] public float glassRefraction;
            [XmlAttribute][DefaultValue(0f)] public float metallic;
            [XmlAttribute][DefaultValue(0f)] public float smoothness;
            [XmlAttribute][DefaultValue(0f)] public float normalStrength;
            [XmlAttribute][DefaultValue(0f)] public float thickness = 1;
        }
    }
}