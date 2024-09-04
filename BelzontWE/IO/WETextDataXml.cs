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

        [XmlElement] public TransformXml transform;
        [XmlElement] public MeshDataTextXml textMesh;
        [XmlElement] public MeshDataImageXml imageMesh;
        [XmlElement] public MeshDataPlaceholderXml layoutMesh;
        [XmlElement] public MeshDataWhiteTextureXml whiteMesh;
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
        public bool ShouldSerializedefaultStyle() => defaultStyle != null;
        public bool ShouldSerializeglassStyle() => glassStyle != null;

        public class TransformXml
        {
            public Vector3Xml offsetPosition = new();
            public Vector3Xml offsetRotation = new();
            public Vector3Xml scale = (Vector3Xml)Vector3.one;
        }

        public class MeshDataWhiteTextureXml
        {
            [XmlIgnore] public WESimulationTextType textType => WESimulationTextType.WhiteTexture;
        }

        public class MeshDataTextXml
        {
            [XmlIgnore] public WESimulationTextType textType => WESimulationTextType.Text;
            [XmlAttribute] public string fontName;
            [XmlElement] public FormulaeXml<string> text;
            [XmlAttribute][DefaultValue(0f)] public float maxWidthMeters;
        }
        public class MeshDataImageXml
        {
            [XmlIgnore] public WESimulationTextType textType => WESimulationTextType.Image;
            [XmlAttribute] public string atlas;
            [XmlElement] public FormulaeXml<string> image;
        }
        public class MeshDataPlaceholderXml
        {
            [XmlIgnore] public WESimulationTextType textType => WESimulationTextType.Placeholder;
            [XmlElement] public FormulaeXml<string> layout;
        }

        public class DefaultStyleXml
        {
            [XmlIgnore] public WEShader shader => WEShader.Default;
            [XmlAttribute][DefaultValue(WETextDataMaterial.DEFAULT_DECAL_FLAGS)] public int decalFlags = WETextDataMaterial.DEFAULT_DECAL_FLAGS;
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
            [XmlIgnore] public WEShader shader => WEShader.Glass;
            [XmlAttribute][DefaultValue(WETextDataMaterial.DEFAULT_DECAL_FLAGS)] public int decalFlags = WETextDataMaterial.DEFAULT_DECAL_FLAGS;
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