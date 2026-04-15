using NUnit.Framework;
using System;
using UnityEngine.Experimental.Rendering;

namespace BelzontWE.Tests.Utils
{
    [TestFixture]
    public class WEAtlasVTUtilsTests
    {
        // ── Format mapping ────────────────────────────────────────────────────

        [Test]
        public void GetBC7Format_LinearTrue_ReturnsUNorm()
            => Assert.That(WEAtlasVTUtils.GetBC7Format(true), Is.EqualTo(GraphicsFormat.RGBA_BC7_UNorm));

        [Test]
        public void GetBC7Format_LinearFalse_ReturnsSRGB()
            => Assert.That(WEAtlasVTUtils.GetBC7Format(false), Is.EqualTo(GraphicsFormat.RGBA_BC7_SRGB));

        // ── IsPowerOf2 ───────────────────────────────────────────────────────

        [TestCase(1, true)]
        [TestCase(2, true)]
        [TestCase(4, true)]
        [TestCase(512, true)]
        [TestCase(1024, true)]
        [TestCase(4096, true)]
        [TestCase(0, false)]
        [TestCase(-1, false)]
        [TestCase(3, false)]
        [TestCase(768, false)]
        [TestCase(1000, false)]
        public void IsPowerOf2_ReturnsExpected(int value, bool expected)
            => Assert.That(WEAtlasVTUtils.IsPowerOf2(value), Is.EqualTo(expected));

        // ── Argument validation (ValidatePreprocessInputs) ───────────────────

        [Test]
        public void Validate_NullData_ThrowsArgumentNullException()
            => Assert.Throws<ArgumentNullException>(() => WEAtlasVTUtils.ValidatePreprocessInputs(null!, 512, 512));

        [Test]
        public void Validate_WidthTooSmall_ThrowsArgumentOutOfRangeException()
            => Assert.Throws<ArgumentOutOfRangeException>(() => WEAtlasVTUtils.ValidatePreprocessInputs(new byte[16], 256, 512));

        [Test]
        public void Validate_HeightTooSmall_ThrowsArgumentOutOfRangeException()
            => Assert.Throws<ArgumentOutOfRangeException>(() => WEAtlasVTUtils.ValidatePreprocessInputs(new byte[16], 512, 256));

        [Test]
        public void Validate_NonPowerOf2Width_ThrowsArgumentException()
        {
            int bc7Size = WEAtlasBC7Utils.GetBC7SizeBytes(768, 512);
            Assert.Throws<ArgumentException>(() => WEAtlasVTUtils.ValidatePreprocessInputs(new byte[bc7Size], 768, 512));
        }

        [Test]
        public void Validate_NonPowerOf2Height_ThrowsArgumentException()
        {
            int bc7Size = WEAtlasBC7Utils.GetBC7SizeBytes(512, 768);
            Assert.Throws<ArgumentException>(() => WEAtlasVTUtils.ValidatePreprocessInputs(new byte[bc7Size], 512, 768));
        }

        [Test]
        public void Validate_WrongDataLength_ThrowsArgumentException()
            => Assert.Throws<ArgumentException>(() => WEAtlasVTUtils.ValidatePreprocessInputs(new byte[100], 512, 512));

        [Test]
        public void Validate_CorrectInputs_DoesNotThrow()
        {
            int bc7Size = WEAtlasBC7Utils.GetBC7SizeBytes(512, 512);
            Assert.DoesNotThrow(() => WEAtlasVTUtils.ValidatePreprocessInputs(new byte[bc7Size], 512, 512));
        }

        [Test]
        public void Validate_1024x1024_CorrectInputs_DoesNotThrow()
        {
            int bc7Size = WEAtlasBC7Utils.GetBC7SizeBytes(1024, 1024);
            Assert.DoesNotThrow(() => WEAtlasVTUtils.ValidatePreprocessInputs(new byte[bc7Size], 1024, 1024));
        }

        // ── Tests requiring game runtime (Colossal.IO.AssetDatabase) ─────────

        [Test, Ignore("GetPreprocessedByteCount uses AtlassingUtils from Colossal.IO.AssetDatabase — requires game DLLs loaded")]
        public void GetPreprocessedByteCount_512x512_IsPositive()
        {
            int result = WEAtlasVTUtils.GetPreprocessedByteCount(512, 512, GraphicsFormat.RGBA_BC7_SRGB);
            Assert.That(result, Is.GreaterThan(0));
        }

        [Test, Ignore("GetPreprocessedByteCount uses AtlassingUtils from Colossal.IO.AssetDatabase — requires game DLLs loaded")]
        public void GetPreprocessedByteCount_1024x1024_LargerThan512()
        {
            int small = WEAtlasVTUtils.GetPreprocessedByteCount(512, 512, GraphicsFormat.RGBA_BC7_SRGB);
            int large = WEAtlasVTUtils.GetPreprocessedByteCount(1024, 1024, GraphicsFormat.RGBA_BC7_SRGB);
            Assert.That(large, Is.GreaterThan(small));
        }

        [Test, Ignore("GetPreprocessedByteCount uses AtlassingUtils from Colossal.IO.AssetDatabase — requires game DLLs loaded")]
        public void GetPreprocessedByteCount_SRGBAndUNorm_SameSize()
        {
            int srgb = WEAtlasVTUtils.GetPreprocessedByteCount(512, 512, GraphicsFormat.RGBA_BC7_SRGB);
            int unorm = WEAtlasVTUtils.GetPreprocessedByteCount(512, 512, GraphicsFormat.RGBA_BC7_UNorm);
            Assert.That(srgb, Is.EqualTo(unorm));
        }

        [Test, Ignore("GetTileCount uses AtlassingUtils from Colossal.IO.AssetDatabase — requires game DLLs loaded")]
        public void GetTileCount_512x512_Returns1()
        {
            int count = WEAtlasVTUtils.GetTileCount(512, 512, 0);
            Assert.That(count, Is.EqualTo(1));
        }

        [Test, Ignore("GetTileCount uses AtlassingUtils from Colossal.IO.AssetDatabase — requires game DLLs loaded")]
        public void GetTileCount_1024x1024_ReturnsMoreThan1()
        {
            int count = WEAtlasVTUtils.GetTileCount(1024, 1024, 1);
            Assert.That(count, Is.GreaterThan(1));
        }

        [Test, Ignore("PreprocessForVT calls AtlassingUtils.PreProcessData — requires Unity runtime and game DLLs loaded")]
        public void PreprocessForVT_512x512_ProducesExpectedByteCount()
        {
            // Would verify: PreprocessForVT(bc7_512x512, 512, 512, RGBA_BC7_SRGB)
            // returns a NativeArray whose Length == GetPreprocessedByteCount(512, 512, RGBA_BC7_SRGB).
        }

        [Test, Ignore("PreprocessForVT calls AtlassingUtils.PreProcessData — requires Unity runtime and game DLLs loaded")]
        public void PreprocessForVT_1024x1024_ProducesExpectedByteCount()
        {
            // Would verify: PreprocessForVT(bc7_1024x1024, 1024, 1024, RGBA_BC7_SRGB)
            // returns a NativeArray whose Length == GetPreprocessedByteCount(1024, 1024, RGBA_BC7_SRGB).
        }

        [Test, Ignore("PreprocessForVT calls AtlassingUtils.PreProcessData — requires Unity runtime and game DLLs loaded")]
        public void PreprocessForVT_4096x4096_ProducesExpectedByteCount()
        {
            // Would verify: max atlas size (4096x4096) produces correct tiled output.
        }

        [Test, Ignore("PreprocessForVT calls AtlassingUtils.PreProcessData — requires Unity runtime and game DLLs loaded")]
        public void PreprocessForVT_AllFiveLayers_ProduceValidData()
        {
            // Would verify: processing all 5 layers (main=SRGB, emissive=SRGB,
            // control=UNorm, mask=UNorm, normal=UNorm) each produce non-empty output
            // with the expected byte count for a 1024x1024 atlas.
        }
    }
}
