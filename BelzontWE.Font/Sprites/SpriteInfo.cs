using System;
using UnityEngine;

namespace WriteEverywhere.Sprites
{
    public class SpriteInfo : IComparable<SpriteInfo>, IEquatable<SpriteInfo>
    {
        [SerializeField]
        protected string m_Name;

        [SerializeField]
        protected Rect m_AtlasRegion;

        [SerializeField]
        protected RectOffset m_Border = new RectOffset();

        [SerializeField]
        protected Texture2D m_Texture;

        public Vector2 pixelSize => new Vector2(m_Texture.width, m_Texture.height);

        public float width => m_Texture.width;

        public float height => m_Texture.height;

        public Rect region
        {
            get
            {
                return m_AtlasRegion;
            }
            set
            {
                m_AtlasRegion = value;
            }
        }

        public Texture2D texture
        {
            get
            {
                return m_Texture;
            }
            set
            {
                m_Texture = value;
            }
        }

        public string name
        {
            get
            {
                return m_Name;
            }
            set
            {
                m_Name = value;
            }
        }

        public RectOffset border
        {
            get
            {
                return m_Border;
            }
            set
            {
                m_Border = value;
            }
        }

        public bool isSliced
        {
            get
            {
                if (border.horizontal <= 0)
                {
                    return border.vertical > 0;
                }

                return true;
            }
        }

        public int CompareTo(SpriteInfo other)
        {
            return m_Name.CompareTo(other.m_Name);
        }

        public override int GetHashCode()
        {
            return m_Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            SpriteInfo spriteInfo = obj as SpriteInfo;
            if (spriteInfo == null)
            {
                return false;
            }

            return m_Name.Equals(spriteInfo.m_Name);
        }

        public bool Equals(SpriteInfo other)
        {
            return m_Name.Equals(other.m_Name);
        }

        public static bool operator ==(SpriteInfo lhs, SpriteInfo rhs)
        {
            if (object.ReferenceEquals(lhs, rhs))
            {
                return true;
            }

            if ((object)lhs == null || (object)rhs == null)
            {
                return false;
            }

            return lhs.m_Name.Equals(rhs.m_Name);
        }

        public static bool operator !=(SpriteInfo lhs, SpriteInfo rhs)
        {
            return !(lhs == rhs);
        }
    }

}