using BelzontWE.Font;
using NUnit.Framework;

namespace BelzontWE.Tests.Font.Sprites
{
    [TestFixture]
    public class WETextureAtlasVTTests
    {
        // ── Constants ─────────────────────────────────────────────────────────

        [Test]
        public void VT_STACK_DEFAULT_Is0()
            => Assert.That(WETextureAtlas.VT_STACK_DEFAULT, Is.EqualTo(0));

        [Test]
        public void VT_STACK_EXTENDED_Is1()
            => Assert.That(WETextureAtlas.VT_STACK_EXTENDED, Is.EqualTo(1));

        // ── Initial state ─────────────────────────────────────────────────────

        [Test, Ignore("WETextureAtlas references VT types from Colossal.IO.AssetDatabase — requires game runtime")]
        public void NewAtlas_IsVTRegistered_IsFalse()
        {
            var atlas = new WETextureAtlas();
            Assert.That(atlas.IsVTRegistered, Is.False);
        }

        // ── ReserveVTSpace (requires game runtime) ───────────────────────────

        [Test, Ignore("ReserveVTSpace requires TextureStreamingSystem from game runtime")]
        public void ReserveVTSpace_SetsIsVTRegistered()
        {
            // Would verify: after ReserveVTSpace(tss), IsVTRegistered == true
        }

        [Test, Ignore("ReserveVTSpace requires TextureStreamingSystem from game runtime")]
        public void ReserveVTSpace_StackGlobalIndex_IsNonNegative()
        {
            // Would verify: VTAtlasInfoStack0.stackGlobalIndex >= 0
            //               VTAtlasInfoStack1.stackGlobalIndex >= 0
        }

        [Test, Ignore("ReserveVTSpace requires TextureStreamingSystem from game runtime")]
        public void ReserveVTSpace_IndexInStack_IsNonNegative()
        {
            // Would verify: VTAtlasInfoStack0.indexInStack >= 0
            //               VTAtlasInfoStack1.indexInStack >= 0
        }

        [Test, Ignore("ReserveVTSpace requires TextureStreamingSystem from game runtime")]
        public void ReserveVTSpace_ParamBlocks_AreNonDefault()
        {
            // Would verify: VTParamBlock0 != default and VTParamBlock1 != default
        }

        [Test, Ignore("ReserveVTSpace requires TextureStreamingSystem from game runtime")]
        public void ReserveVTSpace_CalledTwice_ReturnsTrueWithoutDoubleReservation()
        {
            // Would verify: calling ReserveVTSpace twice returns true both times
            // and does not reserve a second set of rects
        }
    }
}
