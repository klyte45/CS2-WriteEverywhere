using Belzont.Utils;
#pragma warning disable IDE1006 
using System.Xml.Serialization;
using UnityEngine;

namespace BelzontWE
{
    public class WETextDataXml
    {
        public Vector3Xml offsetPosition;
        public Vector3Xml offsetRotation;
        public Vector3Xml scale;
        [XmlAttribute] public string itemName;
        [XmlAttribute] public WEShader shader;
        [XmlAttribute] public string text;
        [XmlAttribute] public string atlas;
        [XmlAttribute] public WESimulationTextType textType;
        public WETextDataStyleXml style;
        [XmlAttribute] public string formulae;
        [XmlAttribute] public string fontName;
        internal float maxWidthMeters;

        public class WETextDataStyleXml
        {
            [XmlIgnore] public Color32 color;
            [XmlIgnore] public Color32 emissiveColor;
            [XmlAttribute] public string colorRGBA { get => color.ToRGBA(); set => color = ColorExtensions.FromRGBA(value); }
            [XmlAttribute] public string emissiveColorRGBA { get => emissiveColor.ToRGBA(); set => emissiveColor = ColorExtensions.FromRGBA(value); }
            [XmlAttribute] public float metallic;
            [XmlAttribute] public float smoothness;
            [XmlAttribute] public float emissiveIntensity;
            [XmlAttribute] public float emissiveExposureWeight;
            [XmlAttribute] public float coatStrength;
        }
    }
}