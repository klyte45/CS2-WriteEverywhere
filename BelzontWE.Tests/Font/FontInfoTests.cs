using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using BelzontWE.Font;

namespace BelzontWE.Tests.Font
{
    /// <summary>
    /// Helper that loads the embedded SourceSansPro-Regular.ttf fixture once
    /// and initialises a FontInfo instance for use across test fixtures.
    /// </summary>
    internal static class TestFontFixture
    {
        private static byte[]? _data;
        private static FontInfo? _info;

        public static byte[] Data
        {
            get
            {
                if (_data == null) Load();
                return _data!;
            }
        }

        public static FontInfo Info
        {
            get
            {
                if (_info == null) Load();
                return _info!;
            }
        }

        private static void Load()
        {
            var asm = Assembly.GetExecutingAssembly();
            using var stream = asm.GetManifestResourceStream("TestFont.ttf")!;
            _data = new byte[stream.Length];
            stream.Read(_data, 0, _data.Length);
            _info = new FontInfo();
            var ok = _info.stbtt_InitFont_internal(_data, 0);
            if (ok == 0)
                throw new InvalidOperationException("stbtt_InitFont_internal failed to load test TTF");
        }
    }

    [TestFixture]
    public class FontInfo_InitTests
    {
        // ── stbtt_InitFont_internal ───────────────────────────────────────────

        [Test]
        public void InitFont_WithValidTTF_ReturnsOne()
        {
            var info = new FontInfo();
            var ok = info.stbtt_InitFont_internal(TestFontFixture.Data, 0);
            Assert.AreEqual(1, ok);
        }

        [Test]
        public void InitFont_WithGarbageData_ReturnsZero()
        {
            var info = new FontInfo();
            var garbage = new byte[256]; // all zeros — no valid tables
            var ok = info.stbtt_InitFont_internal(garbage, 0);
            Assert.AreEqual(0, ok);
        }

        [Test]
        public void InitFont_SetsNumGlyphsPositive()
        {
            Assert.Greater(TestFontFixture.Info.numGlyphs, 0);
        }

        [Test]
        public void InitFont_IndexMapIsNonZero()
        {
            Assert.AreNotEqual(0, TestFontFixture.Info.index_map);
        }

        [Test]
        public void InitFont_HeadIsNonZero()
        {
            Assert.AreNotEqual(0, TestFontFixture.Info.head);
        }

        [Test]
        public void InitFont_HheaIsNonZero()
        {
            Assert.AreNotEqual(0, TestFontFixture.Info.hhea);
        }
    }

    [TestFixture]
    public class FontInfo_GlyphTests
    {
        private FontInfo F => TestFontFixture.Info;

        // ── stbtt_FindGlyphIndex ──────────────────────────────────────────────

        [Test]
        public void FindGlyphIndex_SpaceChar_ReturnsPositiveIndex()
        {
            var idx = F.stbtt_FindGlyphIndex(' ');
            Assert.Greater(idx, 0);
        }

        [Test]
        public void FindGlyphIndex_LetterA_ReturnsPositiveIndex()
        {
            var idx = F.stbtt_FindGlyphIndex('A');
            Assert.Greater(idx, 0);
        }

        [Test]
        public void FindGlyphIndex_LetterLow_a_ReturnsPositiveIndex()
        {
            var idx = F.stbtt_FindGlyphIndex('a');
            Assert.Greater(idx, 0);
        }

        [Test]
        public void FindGlyphIndex_DigitZero_ReturnsPositiveIndex()
        {
            var idx = F.stbtt_FindGlyphIndex('0');
            Assert.Greater(idx, 0);
        }

        [Test]
        public void FindGlyphIndex_ValidPrintableCharsAreDifferent()
        {
            var idxA = F.stbtt_FindGlyphIndex('A');
            var idxB = F.stbtt_FindGlyphIndex('B');
            Assert.AreNotEqual(idxA, idxB);
        }

        // ── stbtt_IsGlyphEmpty ────────────────────────────────────────────────

        [Test]
        public void IsGlyphEmpty_SpaceGlyph_IsEmpty()
        {
            var spaceGlyph = F.stbtt_FindGlyphIndex(' ');
            Assert.AreEqual(1, F.stbtt_IsGlyphEmpty(spaceGlyph));
        }

        [Test]
        public void IsGlyphEmpty_LetterA_IsNotEmpty()
        {
            var glyphA = F.stbtt_FindGlyphIndex('A');
            Assert.AreEqual(0, F.stbtt_IsGlyphEmpty(glyphA));
        }

        // ── stbtt_GetGlyphShape ───────────────────────────────────────────────

        [Test]
        public void GetGlyphShape_LetterA_HasVertices()
        {
            var glyphA = F.stbtt_FindGlyphIndex('A');
            var count = F.stbtt_GetGlyphShape(glyphA, out var verts);
            Assert.Greater(count, 0);
            Assert.IsNotNull(verts);
        }

        [Test]
        public void GetGlyphShape_SpaceGlyph_HasZeroVertices()
        {
            var spaceGlyph = F.stbtt_FindGlyphIndex(' ');
            var count = F.stbtt_GetGlyphShape(spaceGlyph, out var verts);
            Assert.AreEqual(0, count);
        }

        // ── stbtt_GetGlyphBox ─────────────────────────────────────────────────

        [Test]
        public void GetGlyphBox_LetterA_Returns1()
        {
            var glyphA = F.stbtt_FindGlyphIndex('A');
            int x0 = 0, y0 = 0, x1 = 0, y1 = 0;
            var result = F.stbtt_GetGlyphBox(glyphA, ref x0, ref y0, ref x1, ref y1);
            Assert.AreEqual(1, result);
        }

        [Test]
        public void GetGlyphBox_LetterA_HasNonZeroWidth()
        {
            var glyphA = F.stbtt_FindGlyphIndex('A');
            int x0 = 0, y0 = 0, x1 = 0, y1 = 0;
            F.stbtt_GetGlyphBox(glyphA, ref x0, ref y0, ref x1, ref y1);
            Assert.Greater(x1, x0);
        }

        [Test]
        public void GetGlyphBox_LetterA_HasNonZeroHeight()
        {
            var glyphA = F.stbtt_FindGlyphIndex('A');
            int x0 = 0, y0 = 0, x1 = 0, y1 = 0;
            F.stbtt_GetGlyphBox(glyphA, ref x0, ref y0, ref x1, ref y1);
            Assert.Greater(y1, y0);
        }

        // ── stbtt_GetGlyphHMetrics ────────────────────────────────────────────

        [Test]
        public void GetGlyphHMetrics_LetterA_HasPositiveAdvance()
        {
            var glyphA = F.stbtt_FindGlyphIndex('A');
            int advance = 0, lsb = 0;
            F.stbtt_GetGlyphHMetrics(glyphA, ref advance, ref lsb);
            Assert.Greater(advance, 0);
        }
    }

    [TestFixture]
    public class FontInfo_MetricsTests
    {
        private FontInfo F => TestFontFixture.Info;

        // ── stbtt_GetFontVMetrics ─────────────────────────────────────────────

        [Test]
        public void GetFontVMetrics_AscentIsBiggerThanDescent()
        {
            F.stbtt_GetFontVMetrics(out var ascent, out var descent, out var lineGap);
            Assert.Greater(ascent, descent);
        }

        [Test]
        public void GetFontVMetrics_AscentIsPositive()
        {
            F.stbtt_GetFontVMetrics(out var ascent, out _, out _);
            Assert.Greater(ascent, 0);
        }

        [Test]
        public void GetFontVMetrics_DescentIsNegative()
        {
            F.stbtt_GetFontVMetrics(out _, out var descent, out _);
            Assert.Less(descent, 0);
        }

        // ── stbtt_GetFontBoundingBox ──────────────────────────────────────────

        [Test]
        public void GetFontBoundingBox_X1GreaterThanX0()
        {
            int x0 = 0, y0 = 0, x1 = 0, y1 = 0;
            F.stbtt_GetFontBoundingBox(ref x0, ref y0, ref x1, ref y1);
            Assert.Greater(x1, x0);
        }

        [Test]
        public void GetFontBoundingBox_Y1GreaterThanY0()
        {
            int x0 = 0, y0 = 0, x1 = 0, y1 = 0;
            F.stbtt_GetFontBoundingBox(ref x0, ref y0, ref x1, ref y1);
            Assert.Greater(y1, y0);
        }

        // ── stbtt_ScaleForPixelHeight ─────────────────────────────────────────

        [Test]
        public void ScaleForPixelHeight_24px_IsPositive()
        {
            var scale = F.stbtt_ScaleForPixelHeight(24f);
            Assert.Greater(scale, 0f);
        }

        [Test]
        public void ScaleForPixelHeight_LargerHeight_LargerScale()
        {
            var scale16 = F.stbtt_ScaleForPixelHeight(16f);
            var scale32 = F.stbtt_ScaleForPixelHeight(32f);
            Assert.Greater(scale32, scale16);
        }

        // ── stbtt_ScaleForMappingEmToPixels ───────────────────────────────────

        [Test]
        public void ScaleForMappingEmToPixels_IsPositive()
        {
            var scale = F.stbtt_ScaleForMappingEmToPixels(16f);
            Assert.Greater(scale, 0f);
        }

        // ── stbtt_GetCodepointHMetrics ────────────────────────────────────────

        [Test]
        public void GetCodepointHMetrics_A_HasPositiveAdvance()
        {
            int advance = 0, lsb = 0;
            F.stbtt_GetCodepointHMetrics('A', ref advance, ref lsb);
            Assert.Greater(advance, 0);
        }

        // ── stbtt_GetCodepointBox ─────────────────────────────────────────────

        [Test]
        public void GetCodepointBox_LetterA_Returns1()
        {
            int x0 = 0, y0 = 0, x1 = 0, y1 = 0;
            var result = F.stbtt_GetCodepointBox('A', ref x0, ref y0, ref x1, ref y1);
            Assert.AreEqual(1, result);
        }

        // ── stbtt_GetFontVMetricsOS2 ──────────────────────────────────────────

        [Test]
        public void GetFontVMetricsOS2_ReturnsOne()
        {
            int typoAscent = 0, typoDescent = 0, typoLineGap = 0;
            var result = F.stbtt_GetFontVMetricsOS2(ref typoAscent, ref typoDescent, ref typoLineGap);
            Assert.AreEqual(1, result);
        }

        [Test]
        public void GetFontVMetricsOS2_TypoAscentIsPositive()
        {
            int typoAscent = 0, typoDescent = 0, typoLineGap = 0;
            F.stbtt_GetFontVMetricsOS2(ref typoAscent, ref typoDescent, ref typoLineGap);
            Assert.Greater(typoAscent, 0);
        }

        // ── stbtt_GetKerningTableLength ───────────────────────────────────────

        [Test]
        public void GetKerningTableLength_IsNonNegative()
        {
            var len = F.stbtt_GetKerningTableLength();
            Assert.GreaterOrEqual(len, 0);
        }
    }

    [TestFixture]
    public class FontInfo_GetFontOffsetForIndexTests
    {
        // ── stbtt_GetFontOffsetForIndex_internal (static via Common) ──────────

        [Test]
        public void GetNumberOfFonts_WithLoadedFont_ReturnsOne()
        {
            var count = Common.stbtt_GetNumberOfFonts_internal(new FakePtr<byte>(TestFontFixture.Data));
            Assert.AreEqual(1, count);
        }

        [Test]
        public void GetFontOffset_Index0_ReturnsZero()
        {
            var offset = Common.stbtt_GetFontOffsetForIndex_internal(new FakePtr<byte>(TestFontFixture.Data), 0);
            Assert.AreEqual(0, offset);
        }

        [Test]
        public void GetFontOffset_Index1_ReturnsMinus1()
        {
            var offset = Common.stbtt_GetFontOffsetForIndex_internal(new FakePtr<byte>(TestFontFixture.Data), 1);
            Assert.AreEqual(-1, offset);
        }
    }
}
