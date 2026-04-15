using NUnit.Framework;

namespace BelzontWE.Tests.Font.Sprites
{
    /// <summary>
    /// Tests for WETextureAtlas.WriteBC7CacheAndReplaceTextures (T5 — Disk Cache Write).
    /// All tests are ignored because they require a Unity GPU context and the native
    /// PipelinePlugin.dll BC7 encoder, which are only available at game runtime.
    /// </summary>
    [TestFixture]
    public class WETextureAtlasDiskCacheWriteTests
    {
        [Test]
        [Ignore("Requires Unity GPU context and PipelinePlugin.dll")]
        public void WriteBC7Cache_AfterApply_CreatesCacheFile()
        {
            // Arrange: build a real WETextureAtlas with at least one sprite, call Apply()
            // Act: call WriteBC7CacheAndReplaceTextures(tempPath, checksum)
            // Assert: file exists at tempPath and WEAtlasCacheFile.ReadFrom returns non-null
        }

        [Test]
        [Ignore("Requires Unity GPU context and PipelinePlugin.dll")]
        public void WriteBC7Cache_FileChecksumMatchesInput()
        {
            // Arrange: same as above
            // Act: write cache with a known checksum value
            // Assert: WEAtlasCacheFile.ReadFrom(path).Checksum == known value
        }

        [Test]
        [Ignore("Requires Unity GPU context and PipelinePlugin.dll")]
        public void WriteBC7Cache_AtlasTexturesReplacedWithBC7()
        {
            // Arrange: texture atlas with RGBA32 textures
            // Act: WriteBC7CacheAndReplaceTextures
            // Assert: all internal Texture2D layers are BC7 format (TextureFormat.BC7) after call
        }

        [Test]
        [Ignore("Requires Unity GPU context and PipelinePlugin.dll")]
        public void WriteBC7Cache_SetsIsWritableFalse()
        {
            // Arrange: atlas IsWritable == true
            // Act: WriteBC7CacheAndReplaceTextures
            // Assert: atlas IsWritable == false
        }

        [Test]
        [Ignore("Requires Unity GPU context and PipelinePlugin.dll")]
        public void WriteBC7Cache_BeforeApply_ThrowsInvalidOperationException()
        {
            // Arrange: atlas that has NOT had Apply() called
            // Act + Assert: WriteBC7CacheAndReplaceTextures throws InvalidOperationException
        }

        [Test]
        [Ignore("Requires Unity GPU context and PipelinePlugin.dll")]
        public void RegisterLocalAtlas_WritesBC7CacheFile()
        {
            // Integration test via WEAtlasesLibrary.RegisterLocalAtlas
            // Requires game systems to be running and a real image folder
        }
    }
}
