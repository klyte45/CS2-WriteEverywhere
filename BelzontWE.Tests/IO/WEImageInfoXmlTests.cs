using NUnit.Framework;
using BelzontWE.Sprites;
using UnityEngine;

namespace BelzontWE.Tests.IO
{
    [TestFixture]
    public class WEImageInfoXmlTests
    {
        [Test]
        public void PixelsPerMeters_DefaultIsHundred()
        {
            var info = new WEImageInfoXml();
            Assert.AreEqual(100f, info.pixelsPerMeters, 0.001f);
        }

        [Test]
        public void Borders_DefaultIsNull()
        {
            var info = new WEImageInfoXml();
            Assert.IsNull(info.borders);
        }

        [Test]
        public void PixelsPerMeters_SetAndGet()
        {
            var info = new WEImageInfoXml { pixelsPerMeters = 200f };
            Assert.AreEqual(200f, info.pixelsPerMeters, 0.001f);
        }

        [Test]
        public void Borders_AssignAndRead()
        {
            var borders = new WEImageInfoXml.BorderOffsets { left = 10, right = 20, top = 5, bottom = 15 };
            var info = new WEImageInfoXml { borders = borders };
            Assert.AreEqual(10, info.borders.left);
            Assert.AreEqual(20, info.borders.right);
            Assert.AreEqual(5, info.borders.top);
            Assert.AreEqual(15, info.borders.bottom);
        }

        [Test]
        public void BorderOffsets_DefaultAllZero()
        {
            var borders = new WEImageInfoXml.BorderOffsets();
            Assert.AreEqual(0, borders.left);
            Assert.AreEqual(0, borders.right);
            Assert.AreEqual(0, borders.top);
            Assert.AreEqual(0, borders.bottom);
        }

        [Test]
        public void ToWEBorder_CalculatesCorrectly()
        {
            var borders = new WEImageInfoXml.BorderOffsets { left = 10, right = 20, top = 5, bottom = 15 };
            var result = borders.ToWEBorder(100f, 200f);
            Assert.AreEqual(0.1f, result.x, 0.001f); // left/width
            Assert.AreEqual(0.2f, result.y, 0.001f); // right/width
            Assert.AreEqual(0.025f, result.z, 0.001f); // top/height
            Assert.AreEqual(0.075f, result.w, 0.001f); // bottom/height
        }

        [Test]
        public void ToWEBorder_AllZero_ReturnsZeroVector()
        {
            var borders = new WEImageInfoXml.BorderOffsets();
            var result = borders.ToWEBorder(100f, 100f);
            Assert.AreEqual(Vector4.zero, result);
        }

        [Test]
        public void ToWEBorder_SymmetricBorders_SymmetricResult()
        {
            var borders = new WEImageInfoXml.BorderOffsets { left = 10, right = 10, top = 10, bottom = 10 };
            var result = borders.ToWEBorder(100f, 100f);
            Assert.AreEqual(result.x, result.y, 0.001f);
            Assert.AreEqual(result.z, result.w, 0.001f);
        }
    }
}
