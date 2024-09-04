using Belzont.Utils;
using BelzontWE.Utils;
using Colossal.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Unity.Entities;

namespace BelzontWE
{
    [XmlRoot("WELayout")]
    public class WETextDataXmlTree : IEquatable<WETextDataXmlTree>
    {
        public WETextDataXml self;
        [XmlIgnore] public Colossal.Hash128 Guid { get; } = System.Guid.NewGuid();
        [XmlElement("children")]
        public WETextDataXmlTree[] children;

        public bool ShouldSerializechildren() => self.layoutMesh is null;


        public static WETextDataXmlTree FromEntity(Entity e, EntityManager em)
        {
            var result = new WETextDataXmlTree
            {
                self = WEXmlExtensions.ToXml(e, em)
            };
            if (em.TryGetBuffer<WESubTextRef>(e, true, out var subTextData))
            {
                result.children = new WETextDataXmlTree[subTextData.Length];
                for (int i = 0; i < subTextData.Length; i++)
                {
                    result.children[i] = FromEntity(subTextData[i].m_weTextData, em);
                }
            }
            return result;
        }

        public string ToXML(bool pretty = true) => XmlUtils.DefaultXmlSerialize(this, pretty);
        public static WETextDataXmlTree FromXML(string text)
        {
            try
            {
                return XmlUtils.DefaultXmlDeserialize<WETextDataXmlTree>(text);
            }
            catch { return null; }
        }

        public override bool Equals(object obj) => Equals(obj as WETextDataXmlTree);

        public bool Equals(WETextDataXmlTree other) => other is not null &&
                   EqualityComparer<WETextDataXml>.Default.Equals(self, other.self) &&
                   EqualityComparer<WETextDataXmlTree[]>.Default.Equals(children, other.children);

        public override int GetHashCode() => HashCode.Combine(self, children);

        public static bool operator ==(WETextDataXmlTree left, WETextDataXmlTree right) => EqualityComparer<WETextDataXmlTree>.Default.Equals(left, right);

        public static bool operator !=(WETextDataXmlTree left, WETextDataXmlTree right) => !(left == right);

        public void MergeChildren(WETextDataXmlTree other)
        {
            children = children.Concat(other.children).ToArray();
        }

        public WETextDataXmlTree Clone() => XmlUtils.CloneViaXml(this);

    }
}