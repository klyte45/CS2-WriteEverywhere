using NUnit.Framework;

namespace BelzontWE.Tests.Font.Sprites
{
    /// <summary>
    /// Tests for savegame BC7 serialization (T7) — WETextureAtlas version 3 format.
    /// All tests are ignored because they require a Unity GPU context and PipelinePlugin.dll.
    /// </summary>
    [TestFixture]
    public class WETextureAtlasBC7SerializationTests
    {
        [Test]
        [Ignore("Requires Unity GPU context and PipelinePlugin.dll")]
        public void Serialize_WillSerializeAtlas_WritesBC7Bytes()
        {
            // Arrange: create a WillSerialize atlas, insert a sprite, Apply()
            // Act: Serialize to a stream
            // Assert: first bytes are version == 3; layer bytes are BC7-sized (GetBC7SizeBytes match)
        }

        [Test]
        [Ignore("Requires Unity GPU context and PipelinePlugin.dll")]
        public void Deserialize_Version3_CreatesBC7Textures()
        {
            // Arrange: serialize a v3 atlas
            // Act: Deserialize it via Deserialize<TReader>
            // Assert: internal textures have TextureFormat.BC7, IsWritable == false
        }

        [Test]
        [Ignore("Requires Unity GPU context and PipelinePlugin.dll")]
        public void Deserialize_Version2_CreatesRGBA32Textures()
        {
            // Arrange: a previously-saved version 2 blob (PNG bytes)
            // Act: Deserialize
            // Assert: textures are RGBA32, IsWritable == true
        }

        [Test]
        [Ignore("Requires Unity GPU context and PipelinePlugin.dll")]
        public void Apply_WillSerialize_SetsIsWritableFalse()
        {
            // Arrange: atlas with WillSerialize=true, sprites inserted
            // Act: Apply()
            // Assert: IsWritable == false (BC7 path taken)
        }

        [Test]
        [Ignore("Requires Unity GPU context and PipelinePlugin.dll")]
        public void Apply_NotWillSerialize_LeavesIsWritableTrue()
        {
            // Arrange: atlas with WillSerialize=false
            // Act: Apply()
            // Assert: IsWritable == true (no BC7 compression)
        }

        [Test]
        [Ignore("Requires Unity GPU context and PipelinePlugin.dll")]
        public void RoundTrip_SaveLoad_SpritesPreserved()
        {
            // Full round-trip: build atlas, serialize, deserialize, verify sprites dictionary
        }
    }
}
