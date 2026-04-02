using NUnit.Framework;
using BelzontWE.Font;

namespace BelzontWE.Tests.Font
{
    [TestFixture]
    public class FontAtlasTests
    {
        // ── Constructor ────────────────────────────────────────────────────

        [Test]
        public void Constructor_SetsWidthAndHeight()
        {
            var atlas = new FontAtlas(256, 128, 16);
            Assert.AreEqual(256, atlas.Width);
            Assert.AreEqual(128, atlas.Height);
        }

        [Test]
        public void Constructor_InitializesOneNode()
        {
            var atlas = new FontAtlas(256, 128, 16);
            Assert.AreEqual(1, atlas.NodesNumber);
        }

        [Test]
        public void Constructor_FirstNodeSpansFullWidth()
        {
            var atlas = new FontAtlas(256, 128, 16);
            Assert.AreEqual(0, atlas.Nodes[0].X);
            Assert.AreEqual(0, atlas.Nodes[0].Y);
            Assert.AreEqual(256, atlas.Nodes[0].Width);
        }

        [Test]
        public void Constructor_AllocatesNodesArray()
        {
            var atlas = new FontAtlas(256, 128, 8);
            Assert.AreEqual(8, atlas.Nodes.Length);
        }

        // ── InsertNode ─────────────────────────────────────────────────────

        [Test]
        public void InsertNode_IncreasesNodeCount()
        {
            var atlas = new FontAtlas(256, 128, 16);
            atlas.InsertNode(1, 10, 20, 30);
            Assert.AreEqual(2, atlas.NodesNumber);
        }

        [Test]
        public void InsertNode_SetsFieldsCorrectly()
        {
            var atlas = new FontAtlas(256, 128, 16);
            atlas.InsertNode(0, 5, 10, 15);
            Assert.AreEqual(5, atlas.Nodes[0].X);
            Assert.AreEqual(10, atlas.Nodes[0].Y);
            Assert.AreEqual(15, atlas.Nodes[0].Width);
        }

        [Test]
        public void InsertNode_ShiftsExistingNodes()
        {
            var atlas = new FontAtlas(256, 128, 16);
            // After constructor: Nodes[0] = {0, 0, 256}
            atlas.InsertNode(0, 5, 10, 15);
            // Original node should have shifted to index 1
            Assert.AreEqual(0, atlas.Nodes[1].X);
            Assert.AreEqual(0, atlas.Nodes[1].Y);
            Assert.AreEqual(256, atlas.Nodes[1].Width);
        }

        [Test]
        public void InsertNode_GrowsArray_WhenFull()
        {
            // Start with capacity 1, already has 1 node from constructor
            var atlas = new FontAtlas(256, 128, 1);
            Assert.AreEqual(1, atlas.Nodes.Length);
            // This insert forces array growth
            atlas.InsertNode(1, 10, 20, 30);
            Assert.GreaterOrEqual(atlas.Nodes.Length, 2);
            Assert.AreEqual(2, atlas.NodesNumber);
        }

        [Test]
        public void InsertNode_GrowsArray_PreservesExistingNodes()
        {
            var atlas = new FontAtlas(256, 128, 1);
            atlas.InsertNode(1, 10, 20, 30);
            // Original constructor node should still be present
            Assert.AreEqual(0, atlas.Nodes[0].X);
            Assert.AreEqual(256, atlas.Nodes[0].Width);
            // New node at index 1
            Assert.AreEqual(10, atlas.Nodes[1].X);
            Assert.AreEqual(20, atlas.Nodes[1].Y);
            Assert.AreEqual(30, atlas.Nodes[1].Width);
        }

        // ── RemoveNode ─────────────────────────────────────────────────────

        [Test]
        public void RemoveNode_DecreasesNodeCount()
        {
            var atlas = new FontAtlas(256, 128, 16);
            atlas.InsertNode(1, 10, 20, 30);
            Assert.AreEqual(2, atlas.NodesNumber);
            atlas.RemoveNode(1);
            Assert.AreEqual(1, atlas.NodesNumber);
        }

        [Test]
        public void RemoveNode_ShiftsElements()
        {
            var atlas = new FontAtlas(256, 128, 16);
            atlas.InsertNode(1, 10, 0, 30);
            atlas.InsertNode(2, 40, 0, 50);
            // Nodes: [0]={0,0,256}, [1]={10,0,30}, [2]={40,0,50}, count=3
            atlas.RemoveNode(1);
            // After remove: [0]={0,0,256}, [1]={40,0,50}, count=2
            Assert.AreEqual(40, atlas.Nodes[1].X);
            Assert.AreEqual(50, atlas.Nodes[1].Width);
        }

        [Test]
        public void RemoveNode_EmptyAtlas_DoesNothing()
        {
            var atlas = new FontAtlas(256, 128, 16);
            atlas.RemoveNode(0);
            // NodesNumber was 1, should become 0
            Assert.AreEqual(0, atlas.NodesNumber);
            // Removing again from empty should not crash
            atlas.RemoveNode(0);
            Assert.AreEqual(0, atlas.NodesNumber);
        }

        // ── Expand ─────────────────────────────────────────────────────────

        [Test]
        public void Expand_WidthOnly_InsertsSkylineNode()
        {
            var atlas = new FontAtlas(128, 128, 16);
            atlas.Expand(256, 128);
            Assert.AreEqual(256, atlas.Width);
            Assert.AreEqual(128, atlas.Height);
            // Should have added a node for the new strip
            Assert.AreEqual(2, atlas.NodesNumber);
        }

        [Test]
        public void Expand_WidthOnly_NewNodeCoversExtension()
        {
            var atlas = new FontAtlas(128, 128, 16);
            atlas.Expand(256, 128);
            // New node should be at X=128, Y=0, Width=128
            Assert.AreEqual(128, atlas.Nodes[1].X);
            Assert.AreEqual(0, atlas.Nodes[1].Y);
            Assert.AreEqual(128, atlas.Nodes[1].Width);
        }

        [Test]
        public void Expand_HeightOnly_DoesNotAddNode()
        {
            var atlas = new FontAtlas(128, 128, 16);
            atlas.Expand(128, 256);
            Assert.AreEqual(128, atlas.Width);
            Assert.AreEqual(256, atlas.Height);
            Assert.AreEqual(1, atlas.NodesNumber);
        }

        [Test]
        public void Expand_BothDimensions_InsertsNodeAndUpdatesSize()
        {
            var atlas = new FontAtlas(128, 128, 16);
            atlas.Expand(256, 256);
            Assert.AreEqual(256, atlas.Width);
            Assert.AreEqual(256, atlas.Height);
            Assert.AreEqual(2, atlas.NodesNumber);
        }

        // ── RectFits ───────────────────────────────────────────────────────

        [Test]
        public void RectFits_SmallRect_ReturnsY()
        {
            var atlas = new FontAtlas(256, 256, 16);
            int y = atlas.RectFits(0, 10, 10);
            Assert.AreEqual(0, y);
        }

        [Test]
        public void RectFits_TooWide_ReturnsNegativeOne()
        {
            var atlas = new FontAtlas(256, 256, 16);
            int y = atlas.RectFits(0, 300, 10);
            Assert.AreEqual(-1, y);
        }

        [Test]
        public void RectFits_TooTall_ReturnsNegativeOne()
        {
            var atlas = new FontAtlas(256, 256, 16);
            int y = atlas.RectFits(0, 10, 300);
            Assert.AreEqual(-1, y);
        }

        // ── AddSkylineLevel ────────────────────────────────────────────────

        [Test]
        public void AddSkylineLevel_InsertsAndTruncatesOverlapping()
        {
            var atlas = new FontAtlas(256, 256, 16);
            // Initial: Nodes[0] = {0,0,256}, count=1
            atlas.AddSkylineLevel(0, 0, 0, 50, 30);
            // After: node at idx 0 is {0, 30, 50}, then the original {0,0,256} gets shifted/shrunk
            Assert.Greater(atlas.NodesNumber, 0);
            Assert.AreEqual(0, atlas.Nodes[0].X);
            Assert.AreEqual(30, atlas.Nodes[0].Y);
            Assert.AreEqual(50, atlas.Nodes[0].Width);
        }

        [Test]
        public void AddSkylineLevel_MergesAdjacentSameY()
        {
            var atlas = new FontAtlas(256, 256, 16);
            // Fill the entire row at y=10
            atlas.AddSkylineLevel(0, 0, 0, 256, 10);
            // All nodes should merge into one since they all have Y=10
            Assert.AreEqual(1, atlas.NodesNumber);
            Assert.AreEqual(10, atlas.Nodes[0].Y);
        }

        // ── AddRect ────────────────────────────────────────────────────────

        [Test]
        public void AddRect_SmallRect_Succeeds()
        {
            var atlas = new FontAtlas(256, 256, 16);
            int rx = 0, ry = 0;
            bool result = atlas.AddRect(10, 10, ref rx, ref ry);
            Assert.IsTrue(result);
            Assert.AreEqual(0, rx);
            Assert.AreEqual(0, ry);
        }

        [Test]
        public void AddRect_Overflow_ReturnsFalse()
        {
            var atlas = new FontAtlas(16, 16, 16);
            int rx = 0, ry = 0;
            // Fill up the atlas
            bool overflowed = false;
            for (int i = 0; i < 100; i++)
            {
                if (!atlas.AddRect(8, 8, ref rx, ref ry))
                {
                    overflowed = true;
                    break;
                }
            }
            Assert.IsTrue(overflowed, "Expected atlas overflow");
        }

        [Test]
        public void AddRect_MultipleRects_AllocatesDifferentPositions()
        {
            var atlas = new FontAtlas(256, 256, 64);
            int rx1 = 0, ry1 = 0, rx2 = 0, ry2 = 0;
            atlas.AddRect(10, 10, ref rx1, ref ry1);
            atlas.AddRect(10, 10, ref rx2, ref ry2);
            // Two rects should not occupy the same position
            Assert.IsFalse(rx1 == rx2 && ry1 == ry2, "Two rects allocated at the same position");
        }

        // ── Version ────────────────────────────────────────────────────────

        [Test]
        public void Version_StartsAtZero()
        {
            var atlas = new FontAtlas(256, 256, 16);
            Assert.AreEqual(0u, atlas.Version);
        }
    }
}
