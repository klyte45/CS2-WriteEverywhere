using BelzontWE.Commons.Utils.AssetPipeline;
using BelzontWE.Font.Utility;
using System;
using System.Xml.Serialization;

namespace BelzontWE.Sprites
{
    // WE-specific sprite info: extends KSpriteInfo with CachedBRI for render caching.
    public class WESpriteInfo : KSpriteInfo, IComparable<WESpriteInfo>, IEquatable<WESpriteInfo>
    {
        [XmlIgnore] public IBasicRenderInformation CachedBRI { get; set; }

        public int CompareTo(WESpriteInfo other) => m_Name.CompareTo(other.m_Name);

        public bool Equals(WESpriteInfo other) => m_Name.Equals(other.m_Name);

        public override bool Equals(object obj) => obj is WESpriteInfo spriteInfo && m_Name.Equals(spriteInfo.m_Name);

        public override int GetHashCode() => m_Name.GetHashCode();

        public override void Dispose() => CachedBRI?.Dispose();

        public static bool operator ==(WESpriteInfo lhs, WESpriteInfo rhs) => ReferenceEquals(lhs, rhs) || (lhs is not null && rhs is not null && lhs.m_Name.Equals(rhs.m_Name));

        public static bool operator !=(WESpriteInfo lhs, WESpriteInfo rhs) => !(lhs == rhs);
    }
}