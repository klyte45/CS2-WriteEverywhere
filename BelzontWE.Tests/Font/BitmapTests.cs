using NUnit.Framework;
using BelzontWE.Font;
using static BelzontWE.Font.Common;

namespace BelzontWE.Tests.Font
{
    [TestFixture]
    public class BitmapTests
    {
        private static stbtt__active_edge MakeEdge(float sy, float ey, float direction = 1f)
        {
            return new stbtt__active_edge
            {
                sy = sy,
                ey = ey,
                direction = direction
            };
        }

        // ── stbtt__handle_clipped_edge ────────────────────────────────────────

        [Test]
        public void HandleClippedEdge_EqualY0Y1_NoChange()
        {
            var scanline = new float[10];
            var e = MakeEdge(0f, 5f);
            Bitmap.stbtt__handle_clipped_edge(scanline, 0, 2, e, 1.5f, 2.0f, 3.0f, 2.0f); // y0==y1
            Assert.AreEqual(0f, scanline[2]);
        }

        [Test]
        public void HandleClippedEdge_Y0AboveEy_NoChange()
        {
            var scanline = new float[10];
            var e = MakeEdge(0f, 1f); // ey = 1
            Bitmap.stbtt__handle_clipped_edge(scanline, 0, 2, e, 1.5f, 1.5f, 3.0f, 3.0f); // y0 > e.ey
            Assert.AreEqual(0f, scanline[2]);
        }

        [Test]
        public void HandleClippedEdge_Y1BelowSy_NoChange()
        {
            var scanline = new float[10];
            var e = MakeEdge(5f, 10f); // sy = 5
            Bitmap.stbtt__handle_clipped_edge(scanline, 0, 2, e, 1.5f, 1.0f, 3.0f, 4.0f); // y1 < e.sy
            Assert.AreEqual(0f, scanline[2]);
        }

        [Test]
        public void HandleClippedEdge_BothXLeftOfPixel_AccumulatesFullHeight()
        {
            var scanline = new float[10];
            var e = MakeEdge(0f, 5f, 1f); // direction = 1
            // x0=0.5, x1=0.5: both <= x=2; so scanline[2] += 1*(y1-y0)
            Bitmap.stbtt__handle_clipped_edge(scanline, 0, 2, e, 0.5f, 1.0f, 0.5f, 2.0f);
            Assert.AreEqual(1.0f, scanline[2], 1e-6f); // direction * (y1-y0) = 1 * 1 = 1
        }

        [Test]
        public void HandleClippedEdge_BothXRightOfPixel_NoChange()
        {
            var scanline = new float[10];
            var e = MakeEdge(0f, 5f, 1f);
            // x0=4.0, x1=4.5: both >= x+1=3; empty body
            Bitmap.stbtt__handle_clipped_edge(scanline, 0, 2, e, 4.0f, 1.0f, 4.5f, 2.0f);
            Assert.AreEqual(0f, scanline[2]);
        }

        [Test]
        public void HandleClippedEdge_XSpansPixel_AccumulatesPartialHeight()
        {
            var scanline = new float[10];
            var e = MakeEdge(0f, 5f, 1f);
            // x0=1.5, x1=3.5: straddles pixel x=2 boundary
            // result = direction * (y1-y0) * (1 - (x0-x+(x1-x))/2) = 1*1*(1-(1.5-2+(3.5-2))/2) = 1*(1-(−0.5+1.5)/2) = 1*(1-0.5) = 0.5
            Bitmap.stbtt__handle_clipped_edge(scanline, 0, 2, e, 1.5f, 1.0f, 3.5f, 2.0f);
            Assert.AreEqual(0.5f, scanline[2], 1e-5f);
        }

        [Test]
        public void HandleClippedEdge_DirectionNegative_AccumulatesNegative()
        {
            var scanline = new float[10];
            var e = MakeEdge(0f, 5f, -1f);
            Bitmap.stbtt__handle_clipped_edge(scanline, 0, 2, e, 0.5f, 1.0f, 0.5f, 2.0f);
            Assert.AreEqual(-1.0f, scanline[2], 1e-6f);
        }

        [Test]
        public void HandleClippedEdge_Y0ClippedToSy_AdjustsX0()
        {
            var scanline = new float[10];
            // e.sy = 1.5; y0 = 0 < e.sy, so y0 gets clipped to 1.5
            // x0 adjusted proportionally. After clip, effective y-range = (e.sy to y1) = (1.5 to 2.0) = 0.5
            var e = MakeEdge(1.5f, 5f, 1f);
            // x0=1, y0=0, x1=1, y1=2 → clip y0 to 1.5: x0 = 1 + (1-1)*(1.5-0)/(2-0) = 1
            // Both x=1: x <= pixel x=2, y1-y0 after clip = 2-1.5 = 0.5
            Bitmap.stbtt__handle_clipped_edge(scanline, 0, 2, e, 1.0f, 0.0f, 1.0f, 2.0f);
            Assert.AreEqual(0.5f, scanline[2], 1e-5f);
        }

        [Test]
        public void HandleClippedEdge_Y1ClippedToEy_AdjustsX1()
        {
            var scanline = new float[10];
            // e.ey = 1.5; y1 = 2 > e.ey, so y1 gets clipped to 1.5
            // Effective y-range = (1.0 to e.ey) = (1.0 to 1.5) = 0.5
            var e = MakeEdge(0f, 1.5f, 1f);
            Bitmap.stbtt__handle_clipped_edge(scanline, 0, 2, e, 1.0f, 1.0f, 1.0f, 2.0f);
            Assert.AreEqual(0.5f, scanline[2], 1e-5f);
        }

        [Test]
        public void HandleClippedEdge_NonZeroOffset_WritesToCorrectIndex()
        {
            var scanline = new float[10];
            var e = MakeEdge(0f, 5f, 1f);
            // offset=3, x=2 → writes to scanline[2+3=5]
            Bitmap.stbtt__handle_clipped_edge(scanline, 3, 2, e, 0.5f, 1.0f, 0.5f, 2.0f);
            Assert.AreEqual(0f, scanline[2]);
            Assert.AreEqual(1.0f, scanline[5], 1e-6f);
        }

        // ── stbtt__new_active (via IsFontFamilyReachable indirectly) ──────────
        // The stbtt__new_active method is private; the following tests confirm 
        // the Bitmap class instantiates without errors

        [Test]
        public void Bitmap_CanBeInstantiated()
        {
            var bmp = new Bitmap();
            Assert.IsNotNull(bmp);
        }

        [Test]
        public void Bitmap_DefaultFields_AreZero()
        {
            var bmp = new Bitmap();
            Assert.AreEqual(0, bmp.w);
            Assert.AreEqual(0, bmp.h);
            Assert.AreEqual(0, bmp.stride);
        }
    }
}
