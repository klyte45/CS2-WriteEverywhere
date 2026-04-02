using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace BelzontWE.Tests.IO
{
    [TestFixture]
    public class MaxRectsBinPackTests
    {
        // ── Constructor and Init ──────────────────────────────────────────────

        [Test]
        public void Constructor_WithDimensions_SetsBinWidth()
        {
            var pack = new MaxRectsBinPack(100, 200);
            Assert.AreEqual(100, pack.binWidth);
        }

        [Test]
        public void Constructor_WithDimensions_SetsBinHeight()
        {
            var pack = new MaxRectsBinPack(100, 200);
            Assert.AreEqual(200, pack.binHeight);
        }

        [Test]
        public void Constructor_Default_BinWidthZero()
        {
            var pack = new MaxRectsBinPack();
            Assert.AreEqual(0, pack.binWidth);
        }

        [Test]
        public void Constructor_WithRotations_SetsAllowRotations()
        {
            var pack = new MaxRectsBinPack(100, 100, true);
            Assert.IsTrue(pack.allowRotations);
        }

        [Test]
        public void Init_SetsDimensions()
        {
            var pack = new MaxRectsBinPack();
            pack.Init(256, 128);
            Assert.AreEqual(256, pack.binWidth);
            Assert.AreEqual(128, pack.binHeight);
        }

        [Test]
        public void Init_ClearsUsedRectangles()
        {
            var pack = new MaxRectsBinPack(100, 100);
            pack.Insert(50, 50, MaxRectsBinPack.FreeRectChoiceHeuristic.RectBestShortSideFit);
            pack.Init(100, 100);
            Assert.AreEqual(0, pack.usedRectangles.Count);
        }

        [Test]
        public void Init_SetsFreeRectangleToFullBin()
        {
            var pack = new MaxRectsBinPack();
            pack.Init(100, 200);
            Assert.AreEqual(1, pack.freeRectangles.Count);
            Assert.AreEqual(100, pack.freeRectangles[0].width, 0.001f);
            Assert.AreEqual(200, pack.freeRectangles[0].height, 0.001f);
        }

        // ── Insert single rect ────────────────────────────────────────────────

        [Test]
        public void Insert_SmallRect_ReturnsNonZeroRect()
        {
            var pack = new MaxRectsBinPack(100, 100);
            var result = pack.Insert(50, 50, MaxRectsBinPack.FreeRectChoiceHeuristic.RectBestShortSideFit);
            Assert.AreNotEqual(0f, result.width);
        }

        [Test]
        public void Insert_RectFitsExactly_Occupancy100Percent()
        {
            var pack = new MaxRectsBinPack(100, 100, false);
            pack.Insert(100, 100, MaxRectsBinPack.FreeRectChoiceHeuristic.RectBestShortSideFit);
            Assert.AreEqual(1.0f, pack.Occupancy(), 0.001f);
        }

        [Test]
        public void Insert_TooBigRect_ReturnsZeroHeightRect()
        {
            var pack = new MaxRectsBinPack(50, 50);
            var result = pack.Insert(100, 100, MaxRectsBinPack.FreeRectChoiceHeuristic.RectBestShortSideFit);
            Assert.AreEqual(0f, result.height);
        }

        [Test]
        public void Insert_AddsToUsedRectangles()
        {
            var pack = new MaxRectsBinPack(100, 100);
            pack.Insert(30, 30, MaxRectsBinPack.FreeRectChoiceHeuristic.RectBestAreaFit);
            Assert.AreEqual(1, pack.usedRectangles.Count);
        }

        [Test]
        public void Insert_TwoRects_RectsDontOverlap()
        {
            var pack = new MaxRectsBinPack(100, 100, false);
            var r1 = pack.Insert(50, 50, MaxRectsBinPack.FreeRectChoiceHeuristic.RectBestShortSideFit);
            var r2 = pack.Insert(50, 50, MaxRectsBinPack.FreeRectChoiceHeuristic.RectBestShortSideFit);
            // Rects should not overlap (position differs)
            bool overlapX = r1.x < r2.x + r2.width && r1.x + r1.width > r2.x;
            bool overlapY = r1.y < r2.y + r2.height && r1.y + r1.height > r2.y;
            Assert.IsFalse(overlapX && overlapY);
        }

        // ── Occupancy ─────────────────────────────────────────────────────────

        [Test]
        public void Occupancy_EmptyBin_IsZero()
        {
            var pack = new MaxRectsBinPack(100, 100);
            Assert.AreEqual(0f, pack.Occupancy(), 0.001f);
        }

        [Test]
        public void Occupancy_AfterHalfFill_IsApproxHalf()
        {
            var pack = new MaxRectsBinPack(100, 100, false);
            pack.Insert(100, 50, MaxRectsBinPack.FreeRectChoiceHeuristic.RectBestShortSideFit);
            Assert.AreEqual(0.5f, pack.Occupancy(), 0.001f);
        }

        // ── Heuristic variants ────────────────────────────────────────────────

        [Test]
        public void Insert_AllHeuristics_ReturnNonZeroRect()
        {
            var heuristics = new[]
            {
                MaxRectsBinPack.FreeRectChoiceHeuristic.RectBestShortSideFit,
                MaxRectsBinPack.FreeRectChoiceHeuristic.RectBestLongSideFit,
                MaxRectsBinPack.FreeRectChoiceHeuristic.RectBestAreaFit,
                MaxRectsBinPack.FreeRectChoiceHeuristic.RectBottomLeftRule,
                MaxRectsBinPack.FreeRectChoiceHeuristic.RectContactPointRule,
            };
            foreach (var h in heuristics)
            {
                var pack = new MaxRectsBinPack(100, 100);
                var result = pack.Insert(20, 20, h);
                Assert.AreNotEqual(0f, result.width, $"Heuristic {h} returned zero-width rect");
            }
        }
    }
}
