using System;
using UnityEngine;

namespace WriteEverywhere.Layout
{
    public class WEImageInfo : IDisposable
    {
        public WEImageInfo(string xmlPath)
        {
            this.xmlPath = xmlPath;
        }
        public readonly string xmlPath;
        public Vector4 Borders { get; set; }
        public string Name { get; set; }
        public Texture2D Texture { get; set; }
        public Texture2D ControlMask { get; set; }
        public Texture2D MaskMap { get; set; }
        public Texture2D Normal { get; set; }
        public Texture2D Emissive { get; set; }
        public float PixelsPerMeter { get; set; }
        public RectOffset OffsetBorders => new RectOffset(Mathf.RoundToInt(Borders.x * Texture.width), Mathf.RoundToInt(Borders.y * Texture.width), Mathf.RoundToInt(Borders.z * Texture.height), Mathf.RoundToInt(Borders.w * Texture.height));

        public void Dispose()
        {
            if (Texture) GameObject.Destroy(Texture);
            if (ControlMask) GameObject.Destroy(ControlMask);
            if (MaskMap) GameObject.Destroy(MaskMap);
            if (Normal) GameObject.Destroy(Normal);
            if (Emissive) GameObject.Destroy(Emissive);
        }
    }
}
