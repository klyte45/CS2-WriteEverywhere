using Belzont.Utils;
using Colossal.Entities;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Unity.Entities;

namespace BelzontWE
{
    [XmlRoot("WELayout")]
    public class WETextDataTree : IEquatable<WETextDataTree>
    {
        public WETextDataXml self;
        [XmlElement("children")]
        public WETextDataTree[] children;

        public bool ShouldSerializechildren() => self?.textType != WESimulationTextType.Placeholder;

        public static WETextDataTree FromEntity(Entity e, EntityManager em)
        {
            if (!em.TryGetComponent<WETextData>(e, out var weTextData)) return default;
            var result = new WETextDataTree
            {
                self = weTextData.ToDataXml(em)
            };
            if (em.TryGetBuffer<WESubTextRef>(e, true, out var subTextData))
            {
                result.children = new WETextDataTree[subTextData.Length];
                for (int i = 0; i < subTextData.Length; i++)
                {
                    result.children[i] = FromEntity(subTextData[i].m_weTextData, em);
                }
            }
            return result;
        }

        public string ToXML(bool pretty = true) => XmlUtils.DefaultXmlSerialize(this, pretty);
        public static WETextDataTree FromXML(string text)
        {
            try
            {
                return XmlUtils.DefaultXmlDeserialize<WETextDataTree>(text);
            }
            catch { return null; }
        }

        public override bool Equals(object obj) => Equals(obj as WETextDataTree);

        public bool Equals(WETextDataTree other) => other is not null &&
                   EqualityComparer<WETextDataXml>.Default.Equals(self, other.self) &&
                   EqualityComparer<WETextDataTree[]>.Default.Equals(children, other.children);

        public override int GetHashCode() => HashCode.Combine(self, children);

        public static bool operator ==(WETextDataTree left, WETextDataTree right) => EqualityComparer<WETextDataTree>.Default.Equals(left, right);

        public static bool operator !=(WETextDataTree left, WETextDataTree right) => !(left == right);
    }
}