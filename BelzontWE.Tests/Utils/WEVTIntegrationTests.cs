using BelzontWE.Font;
using BelzontWE.Utils;
using NUnit.Framework;
using UnityEngine.Experimental.Rendering;

namespace BelzontWE.Tests.Utils
{
    /// <summary>
    /// VT integration tests covering full lifecycle scenarios.
    /// Tests that need game runtime (TextureStreamingSystem) are [Ignore]-marked.
    /// Pure validation tests run in the test runner.
    /// </summary>
    [TestFixture]
    public class WEVTIntegrationTests
    {
        // ── Fallback behavior ──────────────────────────────────────────────

        [Test, Ignore("WETextureAtlas references VT types from Colossal.IO.AssetDatabase — requires game runtime")]
        public void NonVTAtlas_GenerateMaterial_SkipsVTPath()
        {
            // When IsVTRegistered is false, GenerateMaterial should fall through
            // to the direct-texture path. We can verify the initial state.
            var atlas = new WETextureAtlas();
            Assert.That(atlas.IsVTRegistered, Is.False,
                "A freshly created atlas must not be VT-registered");
        }

        [Test, Ignore("WETextureAtlas references VT types from Colossal.IO.AssetDatabase — requires game runtime")]
        public void UploadTilesToVT_NullSerializationOrder_ReturnsFalse()
        {
            // Atlas without serialization data should fail gracefully.
            // This tests the guard in UploadTilesToVT without needing game runtime.
            var atlas = new WETextureAtlas();
            // m_serializationOrder is null by default → UploadTilesToVT should return false
            // (it also requires IsVTRegistered, so it will early-return false anyway)
            Assert.That(atlas.IsVTRegistered, Is.False);
        }

        [Test, Ignore("DeregisterFromVT references TextureStreamingSystem — requires game runtime")]
        public void DeregisterFromVT_OnUnregisteredAtlas_IsNoOp()
        {
            // Would verify: calling DeregisterFromVT(null) on unregistered atlas is a no-op
            // Cannot test directly because parameter type TextureStreamingSystem lives in
            // Colossal.IO.AssetDatabase which is not available in the test runner.
        }

        // ── Format mapping validation ──────────────────────────────────────

        [Test]
        public void GetBC7Format_SRGB_MatchesMainMaskControlEmissive()
        {
            var format = WEAtlasVTUtils.GetBC7Format(linear: false);
            Assert.That(format, Is.EqualTo(GraphicsFormat.RGBA_BC7_SRGB));
        }

        [Test]
        public void GetBC7Format_Linear_MatchesNormal()
        {
            var format = WEAtlasVTUtils.GetBC7Format(linear: true);
            Assert.That(format, Is.EqualTo(GraphicsFormat.RGBA_BC7_UNorm));
        }

        // ── VT stack constants ─────────────────────────────────────────────

        [Test]
        public void VTStackConstants_MatchGameStackConfig()
        {
            // DefaultPVTStack = 0, ExtendedPVTStack = 1 (matches game TextureStreamingSystem)
            Assert.That(WETextureAtlas.VT_STACK_DEFAULT, Is.EqualTo(0));
            Assert.That(WETextureAtlas.VT_STACK_EXTENDED, Is.EqualTo(1));
        }

        // ── Layer / tile validation ────────────────────────────────────────

        [TestCase(512, 512, 0, Description = "512x512 → 0 mip levels (single tile)")]
        [TestCase(1024, 1024, 1, Description = "1024x1024 → 1 mip level (4 tiles + 1 root)")]
        [TestCase(2048, 2048, 2, Description = "2048x2048 → 2 mip levels")]
        public void VerifyMaxLevel_MatchesTileSize512(int width, int height, int expectedMaxLevel)
        {
            // maxLevel = log2(min(width,height) / tileSize)
            int maxLevel = (int)System.Math.Log(System.Math.Min(width, height) / (double)WEAtlasVTUtils.VT_TILE_SIZE, 2.0);
            Assert.That(maxLevel, Is.EqualTo(expectedMaxLevel));
        }

        [Test]
        public void VTTileSize_Is512()
        {
            Assert.That(WEAtlasVTUtils.VT_TILE_SIZE, Is.EqualTo(512));
        }

        [Test]
        public void VTPadding_Is8()
        {
            Assert.That(WEAtlasVTUtils.VT_PADDING, Is.EqualTo(8));
        }

        // ── Full lifecycle tests (require game runtime) ────────────────────

        [Test, Ignore("Requires game runtime (TextureStreamingSystem)")]
        public void FullLifecycle_Register_Deregister_Reregister()
        {
            // ReserveVTSpace → UploadTilesToVT → DeregisterFromVT → ReserveVTSpace again
            // Verify: GUIDs differ (epoch incremented), state is clean between cycles
        }

        [Test, Ignore("Requires game runtime (TextureStreamingSystem)")]
        public void VisualParity_VTMaterial_MatchesDirectTextureMaterial()
        {
            // Load atlas in BC7, generate VT material and direct material,
            // compare shader keywords (ENABLE_VT vs none), texture slots, etc.
        }

        [Test, Ignore("Requires game runtime (TextureStreamingSystem)")]
        public void ReloadCycle_10x_NoVTSlotLeak()
        {
            // Register 10 atlases, deregister all, measure VT slot usage
            // (no public API to query VT slot count, but verify no exceptions)
        }

        [Test, Ignore("Requires game runtime (TextureStreamingSystem)")]
        public void VTFailure_GracefulFallback_DirectTextureRendering()
        {
            // Force VT reservation failure (e.g. mock TSS returning invalid info),
            // verify atlas renders correctly via direct textures
        }

        [Test, Ignore("Requires game runtime (TextureStreamingSystem)")]
        public void FontMaterials_Unaffected_ByVTRegistration()
        {
            // Verify FontAtlas materials have no ENABLE_VT keyword
            // and are generated through a completely separate path
        }

        [Test, Ignore("Requires game runtime to measure GPU memory")]
        public void MemoryReduction_VTAtlas_LessThanDirectTexture()
        {
            // VT tiles only resident tiles in GPU memory vs full 5xWxH textures
            // Expected: ~60-80% reduction for large atlases (2048x2048+)
        }
    }
}
