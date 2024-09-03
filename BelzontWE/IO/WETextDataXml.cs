using Belzont.Utils;
using Colossal.OdinSerializer.Utilities;
using System.ComponentModel;


#pragma warning disable IDE1006
using System.Xml.Serialization;
using UnityEngine;

namespace BelzontWE
{
    public class WETextDataXml
    {
        [XmlAttribute] public string itemName;
        [XmlAttribute] public WEShader shader;
        [XmlAttribute][DefaultValue(WETextDataMain.DEFAULT_DECAL_FLAGS)] public int decalFlags = WETextDataMain.DEFAULT_DECAL_FLAGS;

        [XmlAttribute] public WESimulationTextType textType;
        [XmlElement] public TransformXml transform;
        [XmlElement] public MeshDataTextXml textMesh;
        [XmlElement] public MeshDataImageXml imageMesh;
        [XmlElement] public MeshDataPlaceholderXml layoutMesh;
        [XmlElement] public DefaultStyleXml defaultStyle;
        [XmlElement] public GlassStyleXml glassStyle;

        public bool ShouldSerializeshader() => textType != WESimulationTextType.Placeholder;
        public bool ShouldSerializetextMesh() => textType == WESimulationTextType.Text;
        public bool ShouldSerializeimageMesh() => textType == WESimulationTextType.Image;
        public bool ShouldSerializelayoutMesh() => textType == WESimulationTextType.Placeholder;
        public bool ShouldSerializedefaultStyle() => shader == WEShader.Default;
        public bool ShouldSerializeglassStyle() => shader == WEShader.Glass;

        public class TransformXml
        {
            public Vector3Xml offsetPosition = new();
            public Vector3Xml offsetRotation = new();
            public Vector3Xml scale = (Vector3Xml)Vector3.one;
        }

        public class MeshDataTextXml
        {
            [XmlAttribute] public string fontName;
            [XmlElement] public FormulaeXml<string> text;
            [XmlAttribute][DefaultValue(0f)] public float maxWidthMeters;
        }
        public class MeshDataImageXml
        {
            [XmlAttribute] public string atlas;
            [XmlElement] public FormulaeXml<string> image;
        }
        public class MeshDataPlaceholderXml
        {
            [XmlElement] public FormulaeXml<string> layout;
        }

        public class DefaultStyleXml
        {
            [XmlElement] public FormulaeColorRgbaXml color;
            [XmlElement] public FormulaeColorRgbaXml emissiveColor;
            [XmlElement] public FormulaeXml<float> metallic;
            [XmlElement] public FormulaeXml<float> smoothness;
            [XmlElement] public FormulaeXml<float> emissiveIntensity;
            [XmlElement] public FormulaeXml<float> emissiveExposureWeight;
            [XmlElement] public FormulaeXml<float> coatStrength;
            [XmlElement] public FormulaeColorRgbXml colorMask1 = new() { defaultValue = Color.white };
            [XmlElement] public FormulaeColorRgbXml colorMask2 = new() { defaultValue = Color.white };
            [XmlElement] public FormulaeColorRgbXml colorMask3 = new() { defaultValue = Color.white };
        }

        public class GlassStyleXml
        {
            [XmlElement] public FormulaeColorRgbaXml color;
            [XmlElement] public FormulaeColorRgbXml glassColor;
            [XmlElement] public FormulaeXml<float> glassRefraction;
            [XmlElement] public FormulaeXml<float> metallic;
            [XmlElement] public FormulaeXml<float> smoothness;
            [XmlElement] public FormulaeXml<float> normalStrength;
            [XmlElement] public FormulaeXml<float> glassThickness = new() { defaultValue = .5f };
        }

        public class FormulaeXml<T>
        {
            [XmlText] public T defaultValue;
            [XmlAttribute] public string formulae;
            public bool ShouldSerializeformulae() => !formulae.IsNullOrWhitespace();
        }
        public class FormulaeColorRgbXml
        {
            [XmlIgnore] public Color32 defaultValue;
            [XmlText] public string defaultValueRGB { get => defaultValue.ToRGB(); set => defaultValue = ColorExtensions.FromRGB(value); }
            [XmlAttribute] public string formulae;
            public bool ShouldSerializeformulae() => !formulae.IsNullOrWhitespace();
        }
        public class FormulaeColorRgbaXml
        {
            [XmlIgnore] public Color32 defaultValue;
            [XmlText] public string defaultValueRGBA { get => defaultValue.ToRGBA(); set => defaultValue = ColorExtensions.FromRGBA(value); }
            [XmlAttribute] public string formulae;
            public bool ShouldSerializeformulae() => !formulae.IsNullOrWhitespace();
        }
    }
}