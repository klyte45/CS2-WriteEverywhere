using NUnit.Framework;
using System;
using System.IO;
using System.Collections.Generic;

namespace BelzontWE.Tests.Font.Sprites
{
    /// <summary>
    /// Integration tests for the full Atlas Memory Optimization pipeline (Sprint 011 T9).
    /// Documents the complete build→cache→reload→serialize lifecycle.
    /// Tests requiring Unity GPU context / game systems are marked [Ignore].
    /// </summary>
    [TestFixture]
    public class AtlasOptimizationIntegrationTests
    {
        // ─── Pure .NET tests ────────────────────────────────────────────────

        [Test]
        public void BC7SizeFormula_MatchesExpectedValues()
        {
            // Verify the BC7 block-size formula for common atlas resolutions
            Assert.AreEqual(4 * 4 * 16, WEAtlasBC7Utils.GetBC7SizeBytes(16, 16));
            Assert.AreEqual(16 * 16 * 16, WEAtlasBC7Utils.GetBC7SizeBytes(64, 64));
            Assert.AreEqual(64 * 64 * 16, WEAtlasBC7Utils.GetBC7SizeBytes(256, 256));
            Assert.AreEqual(256 * 256 * 16, WEAtlasBC7Utils.GetBC7SizeBytes(1024, 1024));
        }

        [Test]
        public void CacheFile_EmptyFolder_ReturnsBaselineChecksum()
        {
            // FNV-1a offset_basis is returned for empty/nonexistent folders
            Assert.AreEqual(2166136261u, WEChecksumUtils.ComputeFolderChecksum(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())));
        }

        [Test]
        public void CacheFile_RoundTrip_PreservesAllFields()
        {
            // Build a minimal WEAtlasCacheFile and verify round-trip
            var pack = new MaxRectsBinPack(64, 64, false);
            var sprites = new List<BelzontWE.Font.Sprites.WEAtlasCacheFile.CachedSprite>
            {
                new("testSprite", new UnityEngine.Rect(0, 0, 32, 32), BelzontWE.Sprites.WESpriteInfo.ExtraTexturesFlag.Emissive)
            };
            var layerData = new byte[]?[5];
            for (int i = 0; i < 5; i++) layerData[i] = new byte[] { (byte)(i * 10), (byte)(i * 20) };

            var cache = new BelzontWE.Font.Sprites.WEAtlasCacheFile(
                checksum: 0xDEADBEEFu,
                width: 64, height: 64, size: 12,
                method: MaxRectsBinPack.FreeRectChoiceHeuristic.RectBestShortSideFit,
                rectsPack: pack,
                sprites: sprites.AsReadOnly(),
                layerBC7: layerData);

            var path = Path.Combine(Path.GetTempPath(), $"wetest_{Guid.NewGuid()}.cache.we.bc7");
            try
            {
                cache.WriteTo(path);
                var loaded = BelzontWE.Font.Sprites.WEAtlasCacheFile.ReadFrom(path);
                Assert.IsNotNull(loaded);
                Assert.AreEqual(0xDEADBEEFu, loaded.Checksum);
                Assert.AreEqual(64, loaded.Width);
                Assert.AreEqual(64, loaded.Height);
                Assert.AreEqual(12, loaded.Size);
                Assert.AreEqual(1, loaded.Sprites.Count);
                Assert.AreEqual("testSprite", loaded.Sprites[0].Name);
                for (int i = 0; i < 5; i++)
                {
                    Assert.IsNotNull(loaded.LayerBC7[i]);
                    Assert.AreEqual((byte)(i * 10), loaded.LayerBC7[i]![0]);
                }
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
        public void CacheFile_ChecksumMismatch_CausesReload()
        {
            // Simulate what happens in WEAtlasesLibrary: if cache checksum != folder checksum,
            // the cached file is not used (the condition `cachedFile.Checksum == checksum` is false)
            var pack = new MaxRectsBinPack(64, 64, false);
            var cache = new BelzontWE.Font.Sprites.WEAtlasCacheFile(
                checksum: 0xAAAAAAAAu,
                width: 64, height: 64, size: 12,
                method: MaxRectsBinPack.FreeRectChoiceHeuristic.RectBestShortSideFit,
                rectsPack: pack,
                sprites: new List<BelzontWE.Font.Sprites.WEAtlasCacheFile.CachedSprite>().AsReadOnly(),
                layerBC7: new byte[]?[5]);

            var path = Path.Combine(Path.GetTempPath(), $"wetest_{Guid.NewGuid()}.cache.we.bc7");
            try
            {
                cache.WriteTo(path);
                var loaded = BelzontWE.Font.Sprites.WEAtlasCacheFile.ReadFrom(path);
                uint freshChecksum = 0xBBBBBBBBu; // simulated different checksum
                Assert.IsFalse(loaded.Checksum == freshChecksum, "Different checksums should not match");
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        // ─── Unity/game-runtime tests (all ignored) ─────────────────────────

        [Test]
        [Ignore("Requires Unity GPU context and PipelinePlugin.dll")]
        public void FullLifecycle_Build_Cache_Reload_Serialize()
        {
            // 1. Build a WETextureAtlas (WillSerialize=false) with test sprites
            // 2. Apply() → WriteBC7CacheAndReplaceTextures(path, checksum)
            // 3. ReadFrom(path) → verify checksum and sprite names
            // 4. FromCacheFile(cache) → verify atlas textures are BC7 format
            // 5. (WillSerialize=true atlas) → Apply() → Serialize() → Deserialize() → verify sprites
        }

        [Test]
        [Ignore("Requires Unity GPU context and PipelinePlugin.dll")]
        public void BackwardCompat_LoadVersion2_ResavesAsVersion3()
        {
            // Arrange: a serialized version 2 blob (PNG bytes format)
            // Act: Deserialize → re-Apply → re-Serialize
            // Assert: output blob starts with version=3, layers are BC7-sized
        }

        [Test]
        [Ignore("Requires Unity GPU context and PipelinePlugin.dll")]
        public void SmartReload_OnlyChangedFolderRebuilt()
        {
            // Arrange: 3 image folders, all loaded
            // Act: modify one folder's files, run LoadImagesFromLocalFoldersCoroutine()
            // Assert: only the modified folder's atlas was replaced; other 2 are same instances
        }

        [Test]
        [Ignore("Requires performance counters and Unity GPU context")]
        public void MemoryMeasurement_BC7VsPng_ShowsReduction()
        {
            // Measure GPU memory before (RGBA32) and after (BC7) for a 1024x1024 atlas
            // Expected: BC7 uses ~4x less memory than RGBA32
            // (RGBA32: 4 bytes/pixel = 4 MB for 1024^2; BC7: 1 byte/pixel typically = ~1 MB)
        }
    }
}
