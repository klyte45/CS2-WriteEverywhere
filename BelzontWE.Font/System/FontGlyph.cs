using Unity.Collections;
using UnityEngine;

namespace WriteEverywhere.Font
{
    public struct FontGlyph
    {
        public unsafe static int Size => sizeof(FontGlyph);

        public static readonly FontGlyph Null = new FontGlyph();

        private readonly NativeHashMap<int, int> _kernings;
        public Font Font;
        public int Codepoint;
        public int Index;
        public int Height;
        public int Blur;
        public Rect Bounds
        {
            set
            {
                x = value.x;
                xMax = value.xMax;
                xMin = value.xMin;
                y = value.y;
                yMax = value.yMax;
                yMin = value.yMin;
                width = value.width;
                height = value.height;
            }
        }

        public float xMin;
        public float yMin;
        public float xMax;
        public float yMax;
        public float x;
        public float y;
        public float width;
        public float height;


        public int XAdvance;
        public int XOffset;
        public int YOffset;

        public int Pad => PadFromBlur(Blur);

        public bool AtlasGenerated { get; internal set; }

        public int GetKerning(FontGlyph nextGlyph)
        {
            if (_kernings.TryGetValue(nextGlyph.Index, out int result))
            {
                return result;
            }
            result = Font._font.stbtt_GetGlyphKernAdvance(Index, nextGlyph.Index);
            _kernings.Add(nextGlyph.Index, result);

            return result;
        }

        public static int PadFromBlur(int blur) => blur + 2;
    }
}
