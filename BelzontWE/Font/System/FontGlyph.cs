using System;
using System.Runtime.InteropServices;
using Unity.Collections;

namespace BelzontWE.Font
{
    public unsafe struct FontGlyph : IDisposable
    {
        public unsafe static int Size => sizeof(FontGlyph);

        public static readonly FontGlyph Null = new FontGlyph();

        private NativeHashMap<int, int> _kernings;

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

        public int GetKerning(FontGlyph nextGlyph)
        {
            if (!_kernings.IsCreated)
            {
                _kernings = new NativeHashMap<int, int>(1, Allocator.Persistent);
            }
            if (_kernings.TryGetValue(nextGlyph.Index, out int result))
            {
                return result;
            }
            result = Font._font.stbtt_GetGlyphKernAdvance(Index, nextGlyph.Index);
            _kernings.Add(nextGlyph.Index, result);

            return result;
        }

        public int GetKerningCached(FontGlyph nextGlyph)
        {
            if (!_kernings.IsCreated)
            {
                _kernings = new NativeHashMap<int, int>(1, Allocator.Persistent);
            }
            return _kernings.TryGetValue(nextGlyph.Index, out int result) ? result : 0;
        }

        public static int PadFromBlur(int blur) => blur + 2;

        public void Dispose()
        {
            _kernings.Dispose();
        }

        public override string ToString() => $"Glyph#{Index}: x{x} y{y} w{width} h{height} xA{XAdvance} xO{XOffset} yO{YOffset}";
    }
}
