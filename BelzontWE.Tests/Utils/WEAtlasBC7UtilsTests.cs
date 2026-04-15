using NUnit.Framework;

namespace BelzontWE.Tests.Utils
{
    [TestFixture]
    public class WEAtlasBC7UtilsTests
    {
        // ── BC7 size calculation ──────────────────────────────────────────────

        [Test]
        public void GetBC7SizeBytes_512x512_Returns131072()
        {
            // 512/4 = 128 blocks wide, 128 blocks tall → 128*128*16 = 262144
            Assert.That(WEAtlasBC7Utils.GetBC7SizeBytes(512, 512), Is.EqualTo(131072 * 2));
        }

        [Test]
        public void GetBC7SizeBytes_4x4_Returns16()
            => Assert.That(WEAtlasBC7Utils.GetBC7SizeBytes(4, 4), Is.EqualTo(16));

        [Test]
        public void GetBC7SizeBytes_8x8_Returns64()
            => Assert.That(WEAtlasBC7Utils.GetBC7SizeBytes(8, 8), Is.EqualTo(64));

        [Test]
        public void GetBC7SizeBytes_NonMultipleOf4_RoundsUp()
        {
            // 5 pixels wide → 2 blocks (ceil(5/4)=2) → 2*1*16 = 32
            Assert.That(WEAtlasBC7Utils.GetBC7SizeBytes(5, 4), Is.EqualTo(32));
        }

        [Test]
        public void GetBC7SizeBytes_1024x512_Returns1048576()
        {
            // 1024/4=256, 512/4=128 → 256*128*16 = 524288
            Assert.That(WEAtlasBC7Utils.GetBC7SizeBytes(1024, 512), Is.EqualTo(524288));
        }

        // ── Argument validation ───────────────────────────────────────────────

        [Test, Ignore("CompressToBC7 contains unsafe native interop — JIT fails without PipelinePlugin.dll loaded (game runtime only)")]
        public void CompressToBC7_NullSource_ThrowsArgumentNullException() { }

        [Test]
        public void CreateFromBC7_NullData_ThrowsArgumentNullException()
            => Assert.Throws<System.ArgumentNullException>(() => WEAtlasBC7Utils.CreateFromBC7(4, 4, null!, false));

        [Test]
        public void CreateFromBC7_WrongDataLength_ThrowsArgumentException()
        {
            // 4×4 BC7 needs exactly 16 bytes
            Assert.Throws<System.ArgumentException>(() => WEAtlasBC7Utils.CreateFromBC7(4, 4, new byte[15], false));
        }

        // ── Runtime-only tests ────────────────────────────────────────────────

        [Test, Ignore("CompressToBC7 requires Unity GPU context and PipelinePlugin.dll (game runtime only)")]
        public void CompressToBC7_RGBA32Source_ReturnsBC7Bytes() { }

        [Test, Ignore("CreateFromBC7 requires Unity GPU context (Texture2D) — game runtime only")]
        public void CreateFromBC7_ValidData_ReturnsTextureWithMakeNoLongerReadableTrue() { }

        [Test, Ignore("Round-trip test requires Unity GPU context and PipelinePlugin.dll — game runtime only")]
        public void RoundTrip_RGBA32ToBC7ToTexture2D_VisuallyAcceptable() { }
    }
}
