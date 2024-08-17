using Belzont.Utils;
using Colossal.Entities;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Unity.Entities;

namespace BelzontWE
{
    [XmlRoot("WELayout")]
    public class WESelflessTextDataTree : IEquatable<WESelflessTextDataTree>
    {
        [XmlElement("children")]
        public WETextDataTree[] children;

        public static WESelflessTextDataTree FromEntity(Entity e, EntityManager em)
        {
            var result = new WESelflessTextDataTree();
            if (em.TryGetBuffer<WESubTextRef>(e, true, out var subTextData))
            {
                result.children = new WETextDataTree[subTextData.Length];
                for (int i = 0; i < subTextData.Length; i++)
                {
                    result.children[i] = WETextDataTree.FromEntity(subTextData[i].m_weTextData, em);
                }
            }
            return result;
        }

        public string ToXML(bool pretty = true) => XmlUtils.DefaultXmlSerialize(this, pretty);

        public WETextDataTreeStruct ToStruct() => new WETextDataTree { children = children }.ToStruct();
        public static WESelflessTextDataTree FromXML(string text)
        {
            try
            {
                return XmlUtils.DefaultXmlDeserialize<WESelflessTextDataTree>(text);
            }
            catch { return null; }
        }

        public override bool Equals(object obj) => Equals(obj as WESelflessTextDataTree);

        public bool Equals(WESelflessTextDataTree other) => other is not null &&
                   EqualityComparer<WETextDataTree[]>.Default.Equals(children, other.children);

        public override int GetHashCode() => HashCode.Combine(children);

        public static bool operator ==(WESelflessTextDataTree left, WESelflessTextDataTree right) => EqualityComparer<WESelflessTextDataTree>.Default.Equals(left, right);

        public static bool operator !=(WESelflessTextDataTree left, WESelflessTextDataTree right) => !(left == right);
    }
}