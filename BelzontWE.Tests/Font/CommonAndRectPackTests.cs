using NUnit.Framework;
using BelzontWE.Font;
using static BelzontWE.Font.Common;
using static BelzontWE.Font.PackContext;

namespace BelzontWE.Tests.Font
{
    [TestFixture]
    public class CommonTests
    {
        private static FakePtr<byte> Ptr(params byte[] data) => new FakePtr<byte>(data);

        // ── ttUSHORT ──────────────────────────────────────────────────────────

        [Test]
        public void TtUSHORT_ReadsBigEndian()
        {
            Assert.AreEqual(0x0102, ttUSHORT(Ptr(0x01, 0x02)));
        }

        [Test]
        public void TtUSHORT_FullRange()
        {
            Assert.AreEqual(0xFFFF, ttUSHORT(Ptr(0xFF, 0xFF)));
        }

        [Test]
        public void TtUSHORT_Zero()
        {
            Assert.AreEqual(0, ttUSHORT(Ptr(0x00, 0x00)));
        }

        // ── ttSHORT ───────────────────────────────────────────────────────────

        [Test]
        public void TtSHORT_PositiveValue()
        {
            Assert.AreEqual(0x0102, ttSHORT(Ptr(0x01, 0x02)));
        }

        [Test]
        public void TtSHORT_NegativeValue()
        {
            // 0xFF80 = -128 as signed short
            Assert.AreEqual(unchecked((short)0xFF80), ttSHORT(Ptr(0xFF, 0x80)));
        }

        // ── ttULONG ───────────────────────────────────────────────────────────

        [Test]
        public void TtULONG_ReadsBigEndian()
        {
            Assert.AreEqual(0x01020304u, ttULONG(Ptr(0x01, 0x02, 0x03, 0x04)));
        }

        [Test]
        public void TtULONG_Zero()
        {
            Assert.AreEqual(0u, ttULONG(Ptr(0x00, 0x00, 0x00, 0x00)));
        }

        [Test]
        public void TtULONG_MaxValue()
        {
            Assert.AreEqual(0xFFFFFFFFu, ttULONG(Ptr(0xFF, 0xFF, 0xFF, 0xFF)));
        }

        // ── ttLONG ────────────────────────────────────────────────────────────

        [Test]
        public void TtLONG_PositiveValue()
        {
            Assert.AreEqual(0x00010000, ttLONG(Ptr(0x00, 0x01, 0x00, 0x00)));
        }

        [Test]
        public void TtLONG_NegativeValue()
        {
            // 0x80000000 = -2147483648 as signed int
            Assert.AreEqual(unchecked((int)0x80000000), ttLONG(Ptr(0x80, 0x00, 0x00, 0x00)));
        }

        // ── stbtt__isfont ─────────────────────────────────────────────────────

        [Test]
        public void IsFontTrue10000_ReturnsOne()
        {
            // \x01\x00\x00\x00 — TrueType version 1
            Assert.AreEqual(1, stbtt__isfont(Ptr(0x00, 0x01, 0x00, 0x00)));
        }

        [Test]
        public void IsFont_OTTO_ReturnsOne()
        {
            Assert.AreEqual(1, stbtt__isfont(Ptr((byte)'O', (byte)'T', (byte)'T', (byte)'O')));
        }

        [Test]
        public void IsFont_True_ReturnsOne()
        {
            Assert.AreEqual(1, stbtt__isfont(Ptr((byte)'t', (byte)'r', (byte)'u', (byte)'e')));
        }

        [Test]
        public void IsFont_typ1_ReturnsOne()
        {
            Assert.AreEqual(1, stbtt__isfont(Ptr((byte)'t', (byte)'y', (byte)'p', (byte)'1')));
        }

        [Test]
        public void IsFont_1000_ReturnsOne()
        {
            // '1' followed by 3 zeros
            Assert.AreEqual(1, stbtt__isfont(Ptr((byte)'1', 0x00, 0x00, 0x00)));
        }

        [Test]
        public void IsFont_Unknown_ReturnsZero()
        {
            Assert.AreEqual(0, stbtt__isfont(Ptr(0xDE, 0xAD, 0xBE, 0xEF)));
        }

        // ── stbtt_setvertex ───────────────────────────────────────────────────

        [Test]
        public void SetVertex_AssignsAllFields()
        {
            var v = new stbtt_vertex();
            stbtt_setvertex(ref v, (byte)STBTT_vline, 10, 20, 3, 4);
            Assert.AreEqual((byte)STBTT_vline, v.type);
            Assert.AreEqual(10, v.x);
            Assert.AreEqual(20, v.y);
            Assert.AreEqual(3, v.cx);
            Assert.AreEqual(4, v.cy);
        }

        // ── stbtt_GetNumberOfFonts_internal ───────────────────────────────────

        [Test]
        public void GetNumberOfFonts_SingleFont_Returns1()
        {
            // A single-font collection starts with a valid font magic
            var data = new byte[16];
            data[0] = 0x00; data[1] = 0x01; data[2] = 0x00; data[3] = 0x00;
            Assert.AreEqual(1, stbtt_GetNumberOfFonts_internal(Ptr(data)));
        }

        [Test]
        public void GetNumberOfFonts_UnknownMagic_ReturnsZero()
        {
            var data = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0, 0, 0, 0, 0, 0, 0, 0 };
            Assert.AreEqual(0, stbtt_GetNumberOfFonts_internal(Ptr(data)));
        }

        // ── stbtt_GetFontOffsetForIndex_internal ──────────────────────────────

        [Test]
        public void GetFontOffset_SingleFontAtIndex0_ReturnsZero()
        {
            var data = new byte[16];
            data[0] = 0x00; data[1] = 0x01; data[2] = 0x00; data[3] = 0x00;
            Assert.AreEqual(0, stbtt_GetFontOffsetForIndex_internal(Ptr(data), 0));
        }

        [Test]
        public void GetFontOffset_SingleFontAtIndex1_ReturnsMinus1()
        {
            var data = new byte[16];
            data[0] = 0x00; data[1] = 0x01; data[2] = 0x00; data[3] = 0x00;
            Assert.AreEqual(-1, stbtt_GetFontOffsetForIndex_internal(Ptr(data), 1));
        }

        [Test]
        public void GetFontOffset_InvalidMagic_ReturnsMinus1()
        {
            var data = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0, 0, 0, 0, 0, 0, 0, 0 };
            Assert.AreEqual(-1, stbtt_GetFontOffsetForIndex_internal(Ptr(data), 0));
        }
    }

    [TestFixture]
    public class StbrpContextTests
    {
        // ── stbrp_context constructor ─────────────────────────────────────────

        [Test]
        public void Constructor_SetsWidthAndHeight()
        {
            var ctx = new stbrp_context(100, 200);
            Assert.AreEqual(100, ctx.width);
            Assert.AreEqual(200, ctx.height);
        }

        [Test]
        public void Constructor_InitializesPositionsToZero()
        {
            var ctx = new stbrp_context(100, 200);
            Assert.AreEqual(0, ctx.x);
            Assert.AreEqual(0, ctx.y);
            Assert.AreEqual(0, ctx.bottom_y);
        }

        // ── stbrp_pack_rects ──────────────────────────────────────────────────

        [Test]
        public void PackRects_SingleRect_PackedAtOrigin()
        {
            var ctx = new stbrp_context(100, 100);
            var rects = new[] { new stbrp_rect { w = 10, h = 10 } };
            ctx.stbrp_pack_rects(rects, 1);
            Assert.AreEqual(1, rects[0].was_packed);
            Assert.AreEqual(0, rects[0].x);
            Assert.AreEqual(0, rects[0].y);
        }

        [Test]
        public void PackRects_TwoRectsFitOnRow_ArePlacedSequentially()
        {
            var ctx = new stbrp_context(200, 100);
            var rects = new[]
            {
                new stbrp_rect { w = 10, h = 10 },
                new stbrp_rect { w = 10, h = 10 }
            };
            ctx.stbrp_pack_rects(rects, 2);
            Assert.AreEqual(1, rects[0].was_packed);
            Assert.AreEqual(1, rects[1].was_packed);
            Assert.AreEqual(0, rects[0].x);
            Assert.AreEqual(10, rects[1].x);
        }

        [Test]
        public void PackRects_RectExceedsRowWidth_WrapsToNextRow()
        {
            var ctx = new stbrp_context(15, 100);
            var rects = new[]
            {
                new stbrp_rect { w = 10, h = 10 },
                new stbrp_rect { w = 10, h = 10 } // won't fit at x=10, wraps to new row
            };
            ctx.stbrp_pack_rects(rects, 2);
            Assert.AreEqual(0, rects[1].x);
            Assert.AreEqual(10, rects[1].y); // moved down by first rect height
        }

        [Test]
        public void PackRects_RectDoesNotFitVertically_MarkedNotPacked()
        {
            var ctx = new stbrp_context(100, 5); // height too small for 10-high rect
            var rects = new[] { new stbrp_rect { w = 10, h = 10 } };
            ctx.stbrp_pack_rects(rects, 1);
            Assert.AreEqual(0, rects[0].was_packed);
        }

        [Test]
        public void PackRects_UpdatesBottomY()
        {
            var ctx = new stbrp_context(100, 100);
            var rects = new[] { new stbrp_rect { w = 10, h = 20 } };
            ctx.stbrp_pack_rects(rects, 1);
            Assert.AreEqual(20, ctx.bottom_y);
        }

        [Test]
        public void PackRects_ZeroRects_DoesNothing()
        {
            var ctx = new stbrp_context(100, 100);
            ctx.stbrp_pack_rects(new stbrp_rect[0], 0);
            Assert.AreEqual(0, ctx.x);
            Assert.AreEqual(0, ctx.y);
        }
    }

    [TestFixture]
    public class PackContextTests
    {
        // ── stbtt_PackBegin ───────────────────────────────────────────────────

        [Test]
        public void PackBegin_InitializesFields()
        {
            var ctx = new PackContext();
            var pixels = new byte[100 * 100];
            ctx.stbtt_PackBegin(pixels, 100, 100, 0, 1);
            Assert.AreEqual(100, ctx.width);
            Assert.AreEqual(100, ctx.height);
            Assert.AreEqual(1, ctx.h_oversample);
            Assert.AreEqual(1, ctx.v_oversample);
            Assert.AreEqual(0, ctx.skip_missing);
        }

        [Test]
        public void PackBegin_ClearsPixels()
        {
            var pixels = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
            var ctx = new PackContext();
            ctx.stbtt_PackBegin(pixels, 2, 2, 0, 0);
            foreach (var p in pixels) Assert.AreEqual(0, p);
        }

        [Test]
        public void PackBegin_ReturnsOne()
        {
            var ctx = new PackContext();
            var result = ctx.stbtt_PackBegin(new byte[100], 10, 10, 0, 0);
            Assert.AreEqual(1, result);
        }

        [Test]
        public void PackBegin_NullPixels_DoesNotCrash()
        {
            var ctx = new PackContext();
            Assert.DoesNotThrow(() => ctx.stbtt_PackBegin(null, 10, 10, 0, 0));
        }

        // ── stbtt_PackSetOversampling ─────────────────────────────────────────

        [Test]
        public void PackSetOversampling_SetsValues()
        {
            var ctx = new PackContext();
            ctx.stbtt_PackBegin(new byte[100], 10, 10, 0, 0);
            ctx.stbtt_PackSetOversampling(4, 2);
            Assert.AreEqual(4u, ctx.h_oversample);
            Assert.AreEqual(2u, ctx.v_oversample);
        }

        [Test]
        public void PackSetOversampling_ClampsToMax8()
        {
            var ctx = new PackContext();
            ctx.stbtt_PackBegin(new byte[100], 10, 10, 0, 0);
            ctx.stbtt_PackSetOversampling(10, 9); // both exceed 8
            Assert.AreEqual(1u, ctx.h_oversample); // unchanged
            Assert.AreEqual(1u, ctx.v_oversample); // unchanged
        }

        [Test]
        public void PackSetOversampling_ExactlyEight_IsAllowed()
        {
            var ctx = new PackContext();
            ctx.stbtt_PackBegin(new byte[100], 10, 10, 0, 0);
            ctx.stbtt_PackSetOversampling(8, 8);
            Assert.AreEqual(8u, ctx.h_oversample);
            Assert.AreEqual(8u, ctx.v_oversample);
        }

        // ── stbtt_PackSetSkipMissingCodepoints ────────────────────────────────

        [Test]
        public void PackSetSkipMissing_SetsFlag()
        {
            var ctx = new PackContext();
            ctx.stbtt_PackSetSkipMissingCodepoints(1);
            Assert.AreEqual(1, ctx.skip_missing);
        }

        [Test]
        public void PackSetSkipMissing_ClearsFlag()
        {
            var ctx = new PackContext();
            ctx.stbtt_PackSetSkipMissingCodepoints(1);
            ctx.stbtt_PackSetSkipMissingCodepoints(0);
            Assert.AreEqual(0, ctx.skip_missing);
        }
    }
}
