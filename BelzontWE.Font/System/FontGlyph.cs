using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;

namespace BelzontWE.Font
{
    public unsafe struct FontGlyph : IDisposable
    {
        public unsafe static int Size => sizeof(FontGlyph);

        public static readonly FontGlyph Null = new FontGlyph();

        private readonly NativeHashMap<int, int> _kernings;
        private GCHandle fontAddr;
        public Font Font
        {
            get => (fontAddr.IsAllocated) ? (Font)fontAddr.Target : default;
            set
            {
                if (fontAddr.IsAllocated) fontAddr.Free();
                fontAddr = GCHandle.Alloc(value);
            }
        }
        public int Codepoint;
        public int Index;
        public int Height;
        public int Blur;
  
        public float xMin => x;
        public float yMin => y;
        public float xMax => x + width;
        public float yMax => y + height;
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

        public void Dispose()
        {
            if (fontAddr.IsAllocated) fontAddr.Free();
        }
    }
}
