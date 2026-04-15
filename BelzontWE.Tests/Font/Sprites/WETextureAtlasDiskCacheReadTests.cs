using NUnit.Framework;

namespace BelzontWE.Tests.Font.Sprites
{
    /// <summary>
    /// Tests for the disk-cache fast-load path (T6) — WETextureAtlas.FromCacheFile and
    /// the cache-read branch in WEAtlasesLibrary.LoadImagesFromLocalFoldersCoroutine.
    /// All tests are ignored because they require a Unity GPU context and PipelinePlugin.dll.
    /// </summary>
    [TestFixture]
    public class WETextureAtlasDiskCacheReadTests
    {
        [Test]
        [Ignore("Requires Unity GPU context and PipelinePlugin.dll")]
        public void FromCacheFile_ValidCache_ReturnsAtlasWithCorrectDimensions()
        {
            // Arrange: build a WEAtlasCacheFile with known width/height/size
            // Act: WETextureAtlas.FromCacheFile(cache)
            // Assert: atlas.Width == expected, atlas.Height == expected, atlas.Size == expected
        }

        [Test]
        [Ignore("Requires Unity GPU context and PipelinePlugin.dll")]
        public void FromCacheFile_ValidCache_SpriteDictMatchesCachedSprites()
        {
            // Arrange: cache file with 3 known sprites
            // Act: FromCacheFile
            // Assert: atlas.Sprites.Count == 3, each sprite has matching Region and ExtraTextures
        }

        [Test]
        [Ignore("Requires Unity GPU context and PipelinePlugin.dll")]
        public void FromCacheFile_SetsIsAppliedTrue()
        {
            // Assert: atlas.IsApplied == true
        }

        [Test]
        [Ignore("Requires Unity GPU context and PipelinePlugin.dll")]
        public void FromCacheFile_SetsIsWritableFalse()
        {
            // Assert: atlas.IsWritable == false
        }

        [Test]
        [Ignore("Requires Unity GPU context and PipelinePlugin.dll")]
        public void LoadImages_UsesCachedAtlas_WhenChecksumMatches()
        {
            // Integration: write cache, reload with matching checksum, verify LoadFromCache branch
        }

        [Test]
        [Ignore("Requires Unity GPU context and PipelinePlugin.dll")]
        public void LoadImages_RebuildsFull_WhenChecksumMismatches()
        {
            // Integration: write cache with old checksum, reload after folder change, verify re-build
        }
    }
}
