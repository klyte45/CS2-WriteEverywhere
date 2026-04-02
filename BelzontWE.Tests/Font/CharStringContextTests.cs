using NUnit.Framework;
using BelzontWE.Font;
using static BelzontWE.Font.Common;

namespace BelzontWE.Tests.Font
{
    [TestFixture]
    public class CharStringContextTests
    {
        private static CharStringContext MakeDrawCtx(int capacity = 16)
        {
            var ctx = new CharStringContext();
            ctx.bounds = 0;
            ctx.pvertices = new stbtt_vertex[capacity];
            return ctx;
        }

        private static CharStringContext MakeBoundsCtx()
        {
            var ctx = new CharStringContext();
            ctx.bounds = 1;
            return ctx;
        }

        // ── stbtt__track_vertex ───────────────────────────────────────────────

        [Test]
        public void TrackVertex_FirstCall_SetsAllBounds()
        {
            var ctx = MakeBoundsCtx();
            ctx.stbtt__track_vertex(5, 10);
            Assert.AreEqual(5, ctx.min_x);
            Assert.AreEqual(5, ctx.max_x);
            Assert.AreEqual(10, ctx.min_y);
            Assert.AreEqual(10, ctx.max_y);
            Assert.AreEqual(1, ctx.started);
        }

        [Test]
        public void TrackVertex_ExpandsMax()
        {
            var ctx = MakeBoundsCtx();
            ctx.stbtt__track_vertex(0, 0);
            ctx.stbtt__track_vertex(10, 20);
            Assert.AreEqual(10, ctx.max_x);
            Assert.AreEqual(20, ctx.max_y);
        }

        [Test]
        public void TrackVertex_ExpandsMin()
        {
            var ctx = MakeBoundsCtx();
            ctx.stbtt__track_vertex(0, 0);
            ctx.stbtt__track_vertex(-5, -3);
            Assert.AreEqual(-5, ctx.min_x);
            Assert.AreEqual(-3, ctx.min_y);
        }

        [Test]
        public void TrackVertex_DoesNotShrinkBoundsAfterExpanding()
        {
            var ctx = MakeBoundsCtx();
            ctx.stbtt__track_vertex(10, 20);
            ctx.stbtt__track_vertex(5, 8);
            Assert.AreEqual(10, ctx.max_x);
            Assert.AreEqual(20, ctx.max_y);
            Assert.AreEqual(5, ctx.min_x);
            Assert.AreEqual(8, ctx.min_y);
        }

        // ── stbtt__csctx_v (bounds mode) ─────────────────────────────────────

        [Test]
        public void CsctxV_BoundsMode_TracksVertex()
        {
            var ctx = MakeBoundsCtx();
            ctx.stbtt__csctx_v((byte)STBTT_vline, 3, 4, 0, 0, 0, 0);
            Assert.AreEqual(3, ctx.max_x);
            Assert.AreEqual(4, ctx.max_y);
            Assert.AreEqual(1, ctx.num_vertices);
        }

        [Test]
        public void CsctxV_BoundsMode_CubicAlsoTracksControlPoints()
        {
            var ctx = MakeBoundsCtx();
            ctx.stbtt__track_vertex(0, 0); // establish started
            ctx.stbtt__csctx_v((byte)STBTT_vcubic, 10, 10, 5, 5, 8, 8);
            Assert.AreEqual(10, ctx.max_x);
            Assert.AreEqual(10, ctx.max_y);
        }

        // ── stbtt__csctx_v (draw mode) ────────────────────────────────────────

        [Test]
        public void CsctxV_DrawMode_AddsVertexToArray()
        {
            var ctx = MakeDrawCtx();
            ctx.stbtt__csctx_v((byte)STBTT_vline, 3, 7, 1, 2, 4, 5);
            Assert.AreEqual(1, ctx.num_vertices);
            Assert.AreEqual((byte)STBTT_vline, ctx.pvertices[0].type);
            Assert.AreEqual(3, ctx.pvertices[0].x);
            Assert.AreEqual(7, ctx.pvertices[0].y);
        }

        [Test]
        public void CsctxV_DrawMode_StorescxCyCx1Cy1ForCubic()
        {
            var ctx = MakeDrawCtx();
            ctx.stbtt__csctx_v((byte)STBTT_vcubic, 10, 20, 2, 3, 6, 7);
            var v = ctx.pvertices[0];
            Assert.AreEqual(2, v.cx);
            Assert.AreEqual(3, v.cy);
            Assert.AreEqual(6, v.cx1);
            Assert.AreEqual(7, v.cy1);
        }

        // ── stbtt__csctx_close_shape ──────────────────────────────────────────

        [Test]
        public void CloseShape_WhenPositionEqualsFirst_DoesNotAddVertex()
        {
            var ctx = MakeDrawCtx();
            ctx.x = 5;
            ctx.y = 10;
            ctx.first_x = 5;
            ctx.first_y = 10;
            ctx.stbtt__csctx_close_shape();
            Assert.AreEqual(0, ctx.num_vertices);
        }

        [Test]
        public void CloseShape_WhenPositionDiffersFromFirst_AddsLineVertex()
        {
            var ctx = MakeDrawCtx();
            ctx.x = 5;
            ctx.y = 10;
            ctx.first_x = 0;
            ctx.first_y = 0;
            ctx.stbtt__csctx_close_shape();
            Assert.AreEqual(1, ctx.num_vertices);
            Assert.AreEqual((byte)STBTT_vline, ctx.pvertices[0].type);
            Assert.AreEqual(0, ctx.pvertices[0].x);
            Assert.AreEqual(0, ctx.pvertices[0].y);
        }

        // ── stbtt__csctx_rmove_to ─────────────────────────────────────────────

        [Test]
        public void RmoveTo_UpdatesXAndY()
        {
            var ctx = MakeDrawCtx();
            ctx.x = 10;
            ctx.y = 5;
            ctx.stbtt__csctx_rmove_to(3, 2);
            Assert.AreEqual(13, ctx.x);
            Assert.AreEqual(7, ctx.y);
        }

        [Test]
        public void RmoveTo_SetsFirstXY()
        {
            var ctx = MakeDrawCtx();
            ctx.x = 1;
            ctx.y = 2;
            ctx.stbtt__csctx_rmove_to(4, 8);
            Assert.AreEqual(5, ctx.first_x);
            Assert.AreEqual(10, ctx.first_y);
        }

        [Test]
        public void RmoveTo_AddsVmoveVertex()
        {
            var ctx = MakeDrawCtx();
            ctx.stbtt__csctx_rmove_to(3, 4);
            // close_shape emits no vline since first_x/y = x/y after update
            // (close_shape is called before updating position, so they may differ — but initial all-zero: first=0,0, x=0,y=0, dx=3,dy=4: close first, then update)
            // The first close_shape: first_x=x=0, first_y=y=0 → no line added; then x=3, y=4, first_x=3, first_y=4 → vmove
            var vmoveIdx = 0;
            // find the vmove vertex
            for (int i = 0; i < ctx.num_vertices; i++)
            {
                if (ctx.pvertices[i].type == STBTT_vmove)
                {
                    vmoveIdx = i;
                    break;
                }
            }
            Assert.AreEqual((byte)STBTT_vmove, ctx.pvertices[vmoveIdx].type);
            Assert.AreEqual(3, ctx.pvertices[vmoveIdx].x);
            Assert.AreEqual(4, ctx.pvertices[vmoveIdx].y);
        }

        // ── stbtt__csctx_rline_to ─────────────────────────────────────────────

        [Test]
        public void RlineTo_UpdatesXAndY()
        {
            var ctx = MakeDrawCtx();
            ctx.x = 2;
            ctx.y = 3;
            ctx.stbtt__csctx_rline_to(1, 5);
            Assert.AreEqual(3, ctx.x);
            Assert.AreEqual(8, ctx.y);
        }

        [Test]
        public void RlineTo_AddsVlineVertex()
        {
            var ctx = MakeDrawCtx();
            ctx.x = 1;
            ctx.y = 1;
            ctx.stbtt__csctx_rline_to(2, 3);
            Assert.AreEqual(1, ctx.num_vertices);
            Assert.AreEqual((byte)STBTT_vline, ctx.pvertices[0].type);
            Assert.AreEqual(3, ctx.pvertices[0].x);
            Assert.AreEqual(4, ctx.pvertices[0].y);
        }

        // ── stbtt__csctx_rccurve_to ───────────────────────────────────────────

        [Test]
        public void RccurveTo_UpdatesXAndY()
        {
            var ctx = MakeDrawCtx();
            ctx.x = 0;
            ctx.y = 0;
            ctx.stbtt__csctx_rccurve_to(1, 2, 3, 4, 5, 6);
            // cx2 = x+dx1+dx2 = 0+1+3=4; x = cx2+dx3 = 4+5=9
            // cy2 = y+dy1+dy2 = 0+2+4=6; y = cy2+dy3 = 6+6=12
            Assert.AreEqual(9, ctx.x);
            Assert.AreEqual(12, ctx.y);
        }

        [Test]
        public void RccurveTo_AddsVcubicVertex()
        {
            var ctx = MakeDrawCtx();
            ctx.stbtt__csctx_rccurve_to(1, 2, 3, 4, 5, 6);
            Assert.AreEqual(1, ctx.num_vertices);
            Assert.AreEqual((byte)STBTT_vcubic, ctx.pvertices[0].type);
        }

        [Test]
        public void RccurveTo_StoresCorrectControlPoints()
        {
            var ctx = MakeDrawCtx();
            ctx.x = 0;
            ctx.y = 0;
            ctx.stbtt__csctx_rccurve_to(1, 2, 3, 4, 5, 6);
            var v = ctx.pvertices[0];
            // cx1 = x+dx1 = 0+1 = 1
            // cy1 = y+dy1 = 0+2 = 2
            // cx2 = cx1+dx2 = 1+3 = 4
            // cy2 = cy1+dy2 = 2+4 = 6
            Assert.AreEqual(1, v.cx);
            Assert.AreEqual(2, v.cy);
            Assert.AreEqual(4, v.cx1);
            Assert.AreEqual(6, v.cy1);
        }
    }
}
