using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using NUnit.Framework;
using BelzontWE.Font;

namespace BelzontWE.Tests.Font
{
    // ── FontGlyphBounds Tests ──────────────────────────────────────────────

    [TestFixture]
    public class FontGlyphBoundsTests
    {
        [Test]
        public void DefaultValues_AreZero()
        {
            var b = new FontGlyphBounds();
            Assert.AreEqual(0f, b.X0);
            Assert.AreEqual(0f, b.Y0);
            Assert.AreEqual(0f, b.X1);
            Assert.AreEqual(0f, b.Y1);
        }

        [Test]
        public void FieldAssignment_RoundTrips()
        {
            var b = new FontGlyphBounds { X0 = 1.5f, Y0 = 2.5f, X1 = 3.5f, Y1 = 4.5f };
            Assert.AreEqual(1.5f, b.X0);
            Assert.AreEqual(2.5f, b.Y0);
            Assert.AreEqual(3.5f, b.X1);
            Assert.AreEqual(4.5f, b.Y1);
        }

        [Test]
        public void ToString_ContainsAllFourValues()
        {
            var b = new FontGlyphBounds { X0 = 1f, Y0 = 2f, X1 = 3f, Y1 = 4f };
            var s = b.ToString();
            StringAssert.Contains("1", s);
            StringAssert.Contains("2", s);
            StringAssert.Contains("3", s);
            StringAssert.Contains("4", s);
        }

        [Test]
        public void ToString_MatchesExpectedFormat()
        {
            var b = new FontGlyphBounds { X0 = 10f, Y0 = 20f, X1 = 30f, Y1 = 40f };
            var s = b.ToString();
            StringAssert.Contains("[x[", s);
            StringAssert.Contains("];y[", s);
        }
    }

    // ── FontAtlasNode Tests ────────────────────────────────────────────────

    [TestFixture]
    public class FontAtlasNodeTests
    {
        [Test]
        public void DefaultValues_AreZero()
        {
            var n = new FontAtlasNode();
            Assert.AreEqual(0, n.X);
            Assert.AreEqual(0, n.Y);
            Assert.AreEqual(0, n.Width);
        }

        [Test]
        public void FieldAssignment_RoundTrips()
        {
            var n = new FontAtlasNode { X = 10, Y = 20, Width = 30 };
            Assert.AreEqual(10, n.X);
            Assert.AreEqual(20, n.Y);
            Assert.AreEqual(30, n.Width);
        }
    }

    // ── FontCreationException Tests ────────────────────────────────────────

    [TestFixture]
    public class FontCreationExceptionTests
    {
        [Test]
        public void DefaultConstructor_CreatesException()
        {
            var ex = new FontCreationException();
            Assert.IsNotNull(ex);
        }

        [Test]
        public void MessageConstructor_SetsMessage()
        {
            var ex = new FontCreationException("test error");
            Assert.AreEqual("test error", ex.Message);
        }

        [Test]
        public void InnerExceptionConstructor_SetsInnerException()
        {
            var inner = new InvalidOperationException("inner");
            var ex = new FontCreationException("outer", inner);
            Assert.AreSame(inner, ex.InnerException);
            Assert.AreEqual("outer", ex.Message);
        }

        [Test]
        public void IsException_Derived()
        {
            var ex = new FontCreationException("test");
            Assert.IsInstanceOf<Exception>(ex);
        }

#pragma warning disable SYSLIB0011 // BinaryFormatter is obsolete
        [Test]
        public void Serialization_RoundTrips()
        {
            var original = new FontCreationException("serialized msg");
            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, original);
            ms.Position = 0;
            var deserialized = (FontCreationException)formatter.Deserialize(ms);
            Assert.AreEqual("serialized msg", deserialized.Message);
        }
#pragma warning restore SYSLIB0011
    }

    // ── Font (integration via Factory) Tests ───────────────────────────────

    [TestFixture]
    public class FontTests
    {
        [Test]
        public void FromMemory_WithValidTTF_ReturnsFont()
        {
            var font = BelzontWE.Font.Font.FromMemory(TestFontFixture.Data);
            Assert.IsNotNull(font);
        }

        [Test]
        public void FromMemory_PropertiesAreZeroBeforeRecalculate()
        {
            var font = BelzontWE.Font.Font.FromMemory(TestFontFixture.Data);
            Assert.AreEqual(0f, font.Ascent);
            Assert.AreEqual(0f, font.Descent);
            Assert.AreEqual(0f, font.LineHeight);
            Assert.AreEqual(0f, font.Scale);
            Assert.AreEqual(0f, font.Capital);
        }

        [Test]
        public void Recalculate_AscentIsPositive()
        {
            var font = BelzontWE.Font.Font.FromMemory(TestFontFixture.Data);
            font.Recalculate(32f);
            Assert.Greater(font.Ascent, 0f);
        }

        [Test]
        public void Recalculate_DescentIsNonPositive()
        {
            var font = BelzontWE.Font.Font.FromMemory(TestFontFixture.Data);
            font.Recalculate(32f);
            Assert.LessOrEqual(font.Descent, 0f);
        }

        [Test]
        public void Recalculate_LineHeightIsPositive()
        {
            var font = BelzontWE.Font.Font.FromMemory(TestFontFixture.Data);
            font.Recalculate(32f);
            Assert.Greater(font.LineHeight, 0f);
        }

        [Test]
        public void Recalculate_ScaleIsPositive()
        {
            var font = BelzontWE.Font.Font.FromMemory(TestFontFixture.Data);
            font.Recalculate(32f);
            Assert.Greater(font.Scale, 0f);
        }

        [Test]
        public void Recalculate_CapitalIsPositive()
        {
            var font = BelzontWE.Font.Font.FromMemory(TestFontFixture.Data);
            font.Recalculate(32f);
            Assert.Greater(font.Capital, 0f);
        }

        [Test]
        public void FromMemory_WithGarbageData_ThrowsFontCreationException()
        {
            var garbage = new byte[128];
            Assert.Throws<FontCreationException>(() => BelzontWE.Font.Font.FromMemory(garbage));
        }

        [Test]
        public void GetGlyphIndex_LetterA_ReturnsPositive()
        {
            var font = BelzontWE.Font.Font.FromMemory(TestFontFixture.Data);
            Assert.Greater(font.GetGlyphIndex('A'), 0);
        }

        [Test]
        public void BuildGlyphBitmap_LetterA_HasNonZeroBounds()
        {
            var font = BelzontWE.Font.Font.FromMemory(TestFontFixture.Data);
            font.Recalculate(32f);
            var glyphIdx = font.GetGlyphIndex('A');
            int advance = 0, lsb = 0, x0 = 0, y0 = 0, x1 = 0, y1 = 0;
            font.BuildGlyphBitmap(glyphIdx, font.Scale, ref advance, ref lsb, ref x0, ref y0, ref x1, ref y1);
            Assert.Greater(advance, 0);
            Assert.Greater(x1 - x0, 0); // has width
            Assert.Greater(y1 - y0, 0); // has height
        }

        [Test]
        public void Recalculate_ChangesScale()
        {
            var font = BelzontWE.Font.Font.FromMemory(TestFontFixture.Data);
            var originalScale = font.Scale;
            font.Recalculate(48f);
            Assert.AreNotEqual(originalScale, font.Scale);
        }

        [Test]
        public void Recalculate_LargerSize_LargerScale()
        {
            var font = BelzontWE.Font.Font.FromMemory(TestFontFixture.Data);
            font.Recalculate(16f);
            var scale16 = font.Scale;
            font.Recalculate(32f);
            var scale32 = font.Scale;
            Assert.Greater(scale32, scale16);
        }

        [Test]
        public void RecalculateBasedOnHeight_ProducesPositiveMetrics()
        {
            var font = BelzontWE.Font.Font.FromMemory(TestFontFixture.Data);
            font.RecalculateBasedOnHeight(24f);
            Assert.Greater(font.Scale, 0f);
            Assert.Greater(font.Ascent, 0f);
        }

        [Test]
        public void RenderGlyphBitmap_LetterA_WritesNonZeroPixels()
        {
            var font = BelzontWE.Font.Font.FromMemory(TestFontFixture.Data);
            font.Recalculate(32f);
            var glyphIdx = font.GetGlyphIndex('A');
            int advance = 0, lsb = 0, x0 = 0, y0 = 0, x1 = 0, y1 = 0;
            font.BuildGlyphBitmap(glyphIdx, font.Scale, ref advance, ref lsb, ref x0, ref y0, ref x1, ref y1);
            var w = x1 - x0;
            var h = y1 - y0;
            var pixelData = new byte[w * h];
            var output = new FakePtr<byte>(pixelData);
            font.RenderGlyphBitmap(output, w, h, w, glyphIdx);
            bool anyNonZero = false;
            foreach (var b in pixelData) if (b != 0) { anyNonZero = true; break; }
            Assert.IsTrue(anyNonZero, "Expected at least some non-zero pixels after rendering 'A'");
        }
    }
}
