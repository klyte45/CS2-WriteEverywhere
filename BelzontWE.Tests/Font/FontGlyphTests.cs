using System;
using NUnit.Framework;
using BelzontWE.Font;

namespace BelzontWE.Tests.Font
{
    [TestFixture]
    public class FontGlyphTests
    {
        // ── PadFromBlur ────────────────────────────────────────────────────

        [Test]
        public void PadFromBlur_Zero_ReturnsTwo()
        {
            Assert.AreEqual(2, FontGlyph.PadFromBlur(0));
        }

        [Test]
        public void PadFromBlur_Three_ReturnsFive()
        {
            Assert.AreEqual(5, FontGlyph.PadFromBlur(3));
        }

        [Test]
        public void PadFromBlur_One_ReturnsThree()
        {
            Assert.AreEqual(3, FontGlyph.PadFromBlur(1));
        }

        [Test]
        public void Pad_UsesBlurField()
        {
            var glyph = new FontGlyph { Blur = 4 };
            Assert.AreEqual(6, glyph.Pad);
        }

        // ── xMax / yMax ───────────────────────────────────────────────────

        [Test]
        public void xMax_IsXPlusWidth()
        {
            var glyph = new FontGlyph { x = 10f, width = 20f };
            Assert.AreEqual(30f, glyph.xMax);
        }

        [Test]
        public void yMax_IsYPlusHeight()
        {
            var glyph = new FontGlyph { y = 5f, height = 15f };
            Assert.AreEqual(20f, glyph.yMax);
        }

        [Test]
        public void xMin_EqualsX()
        {
            var glyph = new FontGlyph { x = 7.5f };
            Assert.AreEqual(7.5f, glyph.xMin);
        }

        [Test]
        public void yMin_EqualsY()
        {
            var glyph = new FontGlyph { y = 3.2f };
            Assert.AreEqual(3.2f, glyph.yMin);
        }

        // ── Null sentinel ──────────────────────────────────────────────────

        [Test]
        public void Null_IsValid_IsFalse()
        {
            Assert.IsFalse(FontGlyph.Null.IsValid);
        }

        [Test]
        public void Null_IsValidSimple_IsFalse()
        {
            Assert.IsFalse(FontGlyph.Null.IsValidSimple);
        }

        // ── Font property (GCHandle) ──────────────────────────────────────

        [Test]
        public void Font_Setter_MakesIsValidTrue()
        {
            var glyph = new FontGlyph();
            var font = BelzontWE.Font.Font.FromMemory(TestFontFixture.Data);
            glyph.Font = font;
            Assert.IsTrue(glyph.IsValid);
            glyph.Dispose();
        }

        [Test]
        public void Font_Getter_ReturnsSameFont()
        {
            var glyph = new FontGlyph();
            var font = BelzontWE.Font.Font.FromMemory(TestFontFixture.Data);
            glyph.Font = font;
            Assert.AreSame(font, glyph.Font);
            glyph.Dispose();
        }

        [Test]
        public void Font_IsValidSimple_TrueAfterSet()
        {
            var glyph = new FontGlyph();
            var font = BelzontWE.Font.Font.FromMemory(TestFontFixture.Data);
            glyph.Font = font;
            Assert.IsTrue(glyph.IsValidSimple);
            glyph.Dispose();
        }

        [Test]
        public void Dispose_FreesGCHandle()
        {
            var glyph = new FontGlyph();
            var font = BelzontWE.Font.Font.FromMemory(TestFontFixture.Data);
            glyph.Font = font;
            glyph.Dispose();
            Assert.IsFalse(glyph.IsValidSimple);
        }

        // ── ToString ───────────────────────────────────────────────────────

        [Test]
        public void ToString_ContainsGlyphIndex()
        {
            var glyph = new FontGlyph { Index = 42, x = 1, y = 2, width = 3, height = 4 };
            StringAssert.Contains("42", glyph.ToString());
        }

        // ── Size ───────────────────────────────────────────────────────────

        [Test]
        public void Size_IsPositive()
        {
            Assert.Greater(FontGlyph.Size, 0);
        }
    }
}
