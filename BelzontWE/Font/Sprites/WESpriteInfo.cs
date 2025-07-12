using Belzont.Utils;
using BelzontWE.Font.Utility;
using Colossal.Serialization.Entities;
using System;
using System.Xml.Serialization;
using Unity.Mathematics;
using UnityEngine;

namespace BelzontWE.Sprites
{
    public class WESpriteInfo : IComparable<WESpriteInfo>, IEquatable<WESpriteInfo>, ISerializable, IDisposable
    {
        public enum ExtraTexturesFlag
        {
            Control = 1,
            Emissive = 2,
            Mask = 4,
            Normal = 8
        }

        public const uint CURRENT_VERSION = 0;
        protected string m_Name;
        protected Rect m_AtlasRegion;
        public Rect Region { get => m_AtlasRegion; set => m_AtlasRegion = value; }

        public string Name { get => m_Name; set => m_Name = value; }

        [XmlIgnore] public ExtraTexturesFlag ExtraTextures { get; set; }
        public bool HasEmissive { get => (ExtraTextures & ExtraTexturesFlag.Emissive) != 0; set => ExtraTextures = value ? (ExtraTextures | ExtraTexturesFlag.Emissive) : (ExtraTextures & ~ExtraTexturesFlag.Emissive); }
        public bool HasControl { get => (ExtraTextures & ExtraTexturesFlag.Control) != 0; set => ExtraTextures = value ? (ExtraTextures | ExtraTexturesFlag.Control) : (ExtraTextures & ~ExtraTexturesFlag.Control); }
        public bool HasMask { get => (ExtraTextures & ExtraTexturesFlag.Mask) != 0; set => ExtraTextures = value ? (ExtraTextures | ExtraTexturesFlag.Mask) : (ExtraTextures & ~ExtraTexturesFlag.Mask); }
        public bool HasNormal { get => (ExtraTextures & ExtraTexturesFlag.Normal) != 0; set => ExtraTextures = value ? (ExtraTextures | ExtraTexturesFlag.Normal) : (ExtraTextures & ~ExtraTexturesFlag.Normal); }
        [XmlIgnore] public IBasicRenderInformation CachedBRI { get; set; }

        public int CompareTo(WESpriteInfo other) => m_Name.CompareTo(other.m_Name);

        public override int GetHashCode() => m_Name.GetHashCode();

        public override bool Equals(object obj) => obj is WESpriteInfo spriteInfo && m_Name.Equals(spriteInfo.m_Name);

        public bool Equals(WESpriteInfo other) => m_Name.Equals(other.m_Name);

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(CURRENT_VERSION);
            writer.Write(m_Name);
            writer.Write(m_AtlasRegion.position);
            writer.Write(m_AtlasRegion.size);
            writer.Write((int)ExtraTextures);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out uint version);
            if (version > CURRENT_VERSION)
            {
                LogUtils.DoWarnLog($"Invalid version for {GetType()}: {version}");
                return;
            }
            reader.Read(out m_Name);
            reader.Read(out float2 position);
            reader.Read(out float2 size);
            m_AtlasRegion.position = position;
            m_AtlasRegion.size = size;
            reader.Read(out int extraTextures);
            ExtraTextures = (ExtraTexturesFlag)extraTextures;
        }

        public void Dispose() => CachedBRI.Dispose();

        public static bool operator ==(WESpriteInfo lhs, WESpriteInfo rhs) => ReferenceEquals(lhs, rhs) || (lhs is not null && rhs is not null && lhs.m_Name.Equals(rhs.m_Name));

        public static bool operator !=(WESpriteInfo lhs, WESpriteInfo rhs) => !(lhs == rhs);
    }

}