using NUnit.Framework;
using System.IO;
using System.Xml.Serialization;
using BelzontWE.Sprites;
using UnityEngine;

namespace BelzontWE.Tests.Font.Sprites
{
    [TestFixture]
    public class WEImageInfoXmlTests
    {
        // ── Default state ─────────────────────────────────────────────────────

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

        // ── BorderOffsets ─────────────────────────────────────────────────────

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
        public void BorderOffsets_SetAndGet()
        {
            var borders = new WEImageInfoXml.BorderOffsets { left = 10, right = 20, top = 5, bottom = 15 };
            Assert.AreEqual(10, borders.left);
            Assert.AreEqual(20, borders.right);
            Assert.AreEqual(5, borders.top);
            Assert.AreEqual(15, borders.bottom);
        }

        [Test]
        public void ToWEBorder_CalculatesLeftRightRelativeToWidth()
        {
            var borders = new WEImageInfoXml.BorderOffsets { left = 10, right = 20, top = 0, bottom = 0 };
            var result = borders.ToWEBorder(100f, 100f);
            Assert.AreEqual(0.1f, result.x, 0.001f);
            Assert.AreEqual(0.2f, result.y, 0.001f);
        }

        [Test]
        public void ToWEBorder_CalculatesTopBottomRelativeToHeight()
        {
            var borders = new WEImageInfoXml.BorderOffsets { left = 0, right = 0, top = 25, bottom = 50 };
            var result = borders.ToWEBorder(100f, 200f);
            Assert.AreEqual(0.125f, result.z, 0.001f);
            Assert.AreEqual(0.25f, result.w, 0.001f);
        }

        [Test]
        public void ToWEBorder_AllZero_ReturnsZeroVector()
        {
            var borders = new WEImageInfoXml.BorderOffsets();
            var result = borders.ToWEBorder(100f, 100f);
            Assert.AreEqual(Vector4.zero, result);
        }

        // ── XML round-trip ────────────────────────────────────────────────────

        [Test]
        public void XmlRoundTrip_PixelsPerMeters_Preserved()
        {
            var input = new WEImageInfoXml { pixelsPerMeters = 250f };
            var serializer = new XmlSerializer(typeof(WEImageInfoXml));
            var sw = new StringWriter();
            serializer.Serialize(sw, input);
            var output = (WEImageInfoXml)serializer.Deserialize(new StringReader(sw.ToString()));
            Assert.AreEqual(250f, output.pixelsPerMeters, 0.001f);
        }

        [Test]
        public void XmlRoundTrip_NullBorders_RemainsNull()
        {
            var input = new WEImageInfoXml();
            var serializer = new XmlSerializer(typeof(WEImageInfoXml));
            var sw = new StringWriter();
            serializer.Serialize(sw, input);
            var output = (WEImageInfoXml)serializer.Deserialize(new StringReader(sw.ToString()));
            Assert.IsNull(output.borders);
        }

        [Test]
        public void XmlRoundTrip_BorderValues_Preserved()
        {
            var input = new WEImageInfoXml
            {
                pixelsPerMeters = 100f,
                borders = new WEImageInfoXml.BorderOffsets { left = 4, right = 6, top = 2, bottom = 8 }
            };
            var serializer = new XmlSerializer(typeof(WEImageInfoXml));
            var sw = new StringWriter();
            serializer.Serialize(sw, input);
            var output = (WEImageInfoXml)serializer.Deserialize(new StringReader(sw.ToString()));
            Assert.IsNotNull(output.borders);
            Assert.AreEqual(4, output.borders.left);
            Assert.AreEqual(6, output.borders.right);
            Assert.AreEqual(2, output.borders.top);
            Assert.AreEqual(8, output.borders.bottom);
        }
    }
}
