using System.Xml.Serialization;

namespace BelzontWE
{
    public class WEXmlMetadata
    {
        [XmlAttribute] public string dll;
        [XmlAttribute] public string refName;
        [XmlText] public string content;
    }
}