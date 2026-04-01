using System;
using System.Runtime.InteropServices;
using Unity.Collections;

namespace BelzontWE.Font
{
    public unsafe struct FontGlyph : IDisposable
    {
        public unsafe static int Size => sizeof(FontGlyph);

        public static readonly FontGlyph Null = new FontGlyph();

        public int Codepoint;
        public int Index;
        public int Height;
        public int Blur;
        private GCHandle fontAddr;
        public Font Font
        {
            get => fontAddr.IsAllocated && fontAddr.Target is Font fnt ? fnt : null;
            set
            {
                if (fontAddr.IsAllocated) fontAddr.Free();
                fontAddr = GCHandle.Alloc(value, GCHandleType.Weak);
            }
        }
        public readonly bool IsValid => fontAddr.IsAllocated && fontAddr.Target != null;
        public readonly bool IsValidSimple => fontAddr.IsAllocated;

        public readonly float xMin => x;
        public readonly float yMin => y;
        public readonly float xMax => x + width;
        public readonly float yMax => y + height;
        public float x;
        public float y;
        public float width;
        public float height;


        public int XAdvance;
        public int XOffset;
        public int YOffset;

        public readonly int Pad => PadFromBlur(Blur);

        public bool AtlasGenerated { get; internal set; }

        /// <summary>
        /// Populates kerning for this glyph pair into the font-level kerning table.
        /// Key encoding: ((long)this.Index &lt;&lt; 32) | (uint)nextGlyph.Index
        /// </summary>
        public int GetKerning(FontGlyph nextGlyph, ref NativeHashMap<long, int> kerningTable)
        {
            if (Font?._font is null) return 0;
            long key = ((long)Index << 32) | (uint)nextGlyph.Index;
            if (kerningTable.TryGetValue(key, out int result))
            {
                return result;
            }
            result = Font._font.stbtt_GetGlyphKernAdvance(Index, nextGlyph.Index);
            kerningTable.Add(key, result);

            return result;
        }

        public readonly int GetKerningCached(FontGlyph nextGlyph, NativeHashMap<long, int>.ReadOnly kerningTable)
        {
            long key = ((long)Index << 32) | (uint)nextGlyph.Index;
            return kerningTable.TryGetValue(key, out int result) ? result : 0;
        }

        public static int PadFromBlur(int blur) => blur + 2;

        public void Dispose()
        {
            if (fontAddr.IsAllocated) fontAddr.Free();
        }

        public override string ToString() => $"Glyph#{Index}: x{x} y{y} w{width} h{height} xA{XAdvance} xO{XOffset} yO{YOffset}";
    }
}
