using System.Xml.Serialization;
using UnityEngine;

namespace BelzontWE.Sprites
{
    public class XmlVTAtlasInfo
    {
        [XmlAttribute]
        public string Name;
        [XmlAttribute]
        public string FullName;

        [XmlIgnore] public Colossal.Hash128 MainTex;
        [XmlIgnore] public Colossal.Hash128 Normal;
        [XmlIgnore] public Colossal.Hash128 MaskMap;
        [XmlIgnore] public Colossal.Hash128 ControlMap;
        [XmlIgnore] public Colossal.Hash128 Emissive;

        [XmlAttribute("MainTex")] public string MainTexStr { get => MainTex.ToString(); set => MainTex = Colossal.Hash128.Parse(value); }
        [XmlAttribute("Normal")] public string NormalStr { get => Normal.ToString(); set => Normal = Colossal.Hash128.Parse(value); }
        [XmlAttribute("MaskMap")] public string MaskMapStr { get => MaskMap.ToString(); set => MaskMap = Colossal.Hash128.Parse(value); }
        [XmlAttribute("ControlMap")] public string ControlMapStr { get => ControlMap.ToString(); set => ControlMap = Colossal.Hash128.Parse(value); }
        [XmlAttribute("Emissive")] public string EmissiveStr { get => Emissive.ToString(); set => Emissive = Colossal.Hash128.Parse(value); }
       
        [XmlAttribute]
        public ulong Checksum;

        [XmlElement("Sprite")]
        public XmlSpriteEntry[] Sprites;

        public class XmlSpriteEntry
        {
            [XmlAttribute]
            public string Name;
            [XmlIgnore]
            public Rect area;
            [XmlAttribute]
            public float MinX
            {
                get => area.x;
                set => area.x = value;
            }
            [XmlAttribute]
            public float MinY
            {
                get => area.y;
                set => area.y = value;
            }

            [XmlAttribute]
            public float Width
            {
                get => area.width;
                set => area.width = value;
            }
            [XmlAttribute]
            public float Height
            {
                get => area.height;
                set => area.height = value;
            }
            [XmlAttribute]
            public bool HasEmissive;
            [XmlAttribute]
            public bool HasControl;
            [XmlAttribute]
            public bool HasMaskMap;
            [XmlAttribute]
            public bool HasNormal;
        }
    }
}