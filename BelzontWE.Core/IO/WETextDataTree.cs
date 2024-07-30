using Belzont.Utils;
using Colossal.Entities;
using System.Xml.Serialization;
using Unity.Entities;

namespace BelzontWE
{
    public class WETextDataTree
    {
        public WETextDataXml self;
        [XmlElement("children")]
        public WETextDataTree[] children;

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
    }
}