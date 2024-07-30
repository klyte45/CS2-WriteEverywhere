using Belzont.Utils;
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
        [XmlAttribute] public string Text;
        [XmlAttribute] public string Atlas;
        [XmlAttribute] public WESimulationTextType TextType;
        public WETextDataStyleXml style;
        [XmlAttribute] public string Formulae;
        [XmlAttribute] public string FontName;

        public class WETextDataStyleXml
        {
            [XmlIgnore] public Color32 Color;
            [XmlIgnore] public Color32 EmissiveColor;
            [XmlAttribute] public string ColorRGBA { get => Color.ToRGBA(); set => Color = ColorExtensions.FromRGBA(value); }
            [XmlAttribute] public string EmissiveColorRGBA { get => EmissiveColor.ToRGBA(); set => EmissiveColor = ColorExtensions.FromRGBA(value); }
            [XmlAttribute] public float Metallic;
            [XmlAttribute] public float Smoothness;
            [XmlAttribute] public float EmissiveIntensity;
            [XmlAttribute] public float EmissiveExposureWeight;
            [XmlAttribute] public float CoatStrength;
        }
    }
}