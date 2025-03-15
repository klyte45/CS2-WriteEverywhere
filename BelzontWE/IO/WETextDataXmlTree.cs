using Belzont.Serialization;
using Belzont.Utils;
using BelzontWE.Utils;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Unity.Entities;

namespace BelzontWE
{
    [XmlRoot("WELayout")]
    public class WETextDataXmlTree : IEquatable<WETextDataXmlTree>, ISerializable
    {
        public const int CURRENT_VERSION = 0;

        [XmlElement("self")]
        public WETextDataXml self;
        [XmlIgnore] public Colossal.Hash128 Guid { get; private set; } = System.Guid.NewGuid();

        [XmlElement("children")]
        public WETextDataXmlTree[] children = new WETextDataXmlTree[0];

        public bool ShouldSerializechildren() => self.layoutMesh is null;

        internal string ModSource { get; set; }
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
            Guid = System.Guid.NewGuid();
        }

        public WETextDataXmlTree Clone() => XmlUtils.CloneViaXml(this);

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(CURRENT_VERSION);
            writer.WriteNullCheck(self);
            writer.Write(children?.Length ?? 0);
            for (int i = 0; i < children?.Length; i++)
            {
                writer.Write(children[i]);
            }
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out int version);
            if (version > CURRENT_VERSION)
            {
                LogUtils.DoWarnLog($"Invalid version for {GetType()}: {version}");
                return;
            }
            reader.ReadNullCheck(out self);
            reader.Read(out int count);
            children = new WETextDataXmlTree[count];
            for (int i = 0; i < count; i++)
            {
                children[i] = new();
                reader.Read(children[i]);
            }
        }

        internal void MapFontAtlasesTemplates(string modId, HashSet<string> dictAtlases, HashSet<string> dictFonts, HashSet<string> dictTemplates)
        {
            self.MapFontAtlasesTemplates(modId, dictAtlases, dictFonts, dictTemplates);
            if (children?.Length > 0)
            {
                foreach (var child in children)
                {
                    child.MapFontAtlasesTemplates(modId, dictAtlases, dictFonts, dictTemplates);
                }
            }
        }
    }
}