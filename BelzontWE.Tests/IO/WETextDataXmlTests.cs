using NUnit.Framework;
using System.IO;
using System.Xml.Linq;
using System.Xml.Serialization;
using UnityEngine;

namespace BelzontWE.Tests.IO
{
    [TestFixture]
    public class WETextDataXmlTests
    {
        // ── Initial state ─────────────────────────────────────────────────

        [Test]
        public void ItemName_IsNullByDefault()
        {
            var xml = new WETextDataXml();
            Assert.IsNull(xml.itemName);
        }

        [Test]
        public void TextMesh_IsNullByDefault()
        {
            var xml = new WETextDataXml();
            Assert.IsNull(xml.textMesh);
        }

        [Test]
        public void DefaultStyle_IsNullByDefault()
        {
            var xml = new WETextDataXml();
            Assert.IsNull(xml.defaultStyle);
        }

        [Test]
        public void MatrixTransform_IsNullByDefault()
        {
            var xml = new WETextDataXml();
            Assert.IsNull(xml.matrixTransform);
        }

        [Test]
        public void LayoutMesh_IsNullByDefault()
        {
            var xml = new WETextDataXml();
            Assert.IsNull(xml.layoutMesh);
        }

        // ── itemName field ────────────────────────────────────────────────

        [Test]
        public void ItemName_AssignAndRead()
        {
            var xml = new WETextDataXml { itemName = "MyLabel" };
            Assert.AreEqual("MyLabel", xml.itemName);
        }

        // ── ShouldSerialize* — mesh types ─────────────────────────────────

        [Test]
        public void ShouldSerializeTextMesh_WhenNull_ReturnsFalse()
        {
            var xml = new WETextDataXml();
            Assert.IsFalse(xml.ShouldSerializetextMesh());
        }

        [Test]
        public void ShouldSerializeTextMesh_WhenSet_ReturnsTrue()
        {
            var xml = new WETextDataXml { textMesh = new WETextDataXml.MeshDataTextXml() };
            Assert.IsTrue(xml.ShouldSerializetextMesh());
        }

        [Test]
        public void ShouldSerializeImageMesh_WhenNull_ReturnsFalse()
        {
            var xml = new WETextDataXml();
            Assert.IsFalse(xml.ShouldSerializeimageMesh());
        }

        [Test]
        public void ShouldSerializeImageMesh_WhenSet_ReturnsTrue()
        {
            var xml = new WETextDataXml { imageMesh = new WETextDataXml.MeshDataImageXml() };
            Assert.IsTrue(xml.ShouldSerializeimageMesh());
        }

        [Test]
        public void ShouldSerializeLayoutMesh_WhenNull_ReturnsFalse()
        {
            var xml = new WETextDataXml();
            Assert.IsFalse(xml.ShouldSerializelayoutMesh());
        }

        [Test]
        public void ShouldSerializeLayoutMesh_WhenSet_ReturnsTrue()
        {
            var xml = new WETextDataXml { layoutMesh = new WETextDataXml.MeshDataPlaceholderXml() };
            Assert.IsTrue(xml.ShouldSerializelayoutMesh());
        }

        [Test]
        public void ShouldSerializeWhiteMesh_WhenNull_ReturnsFalse()
        {
            var xml = new WETextDataXml();
            Assert.IsFalse(xml.ShouldSerializewhiteMesh());
        }

        [Test]
        public void ShouldSerializeWhiteMesh_WhenSet_ReturnsTrue()
        {
            var xml = new WETextDataXml { whiteMesh = new WETextDataXml.MeshDataWhiteTextureXml() };
            Assert.IsTrue(xml.ShouldSerializewhiteMesh());
        }

        // ── ShouldSerialize* — style types ────────────────────────────────

        [Test]
        public void ShouldSerializeDefaultStyle_WhenSetAndNoMatrixOrLayout_ReturnsTrue()
        {
            var xml = new WETextDataXml { defaultStyle = new WETextDataXml.DefaultStyleXml() };
            Assert.IsTrue(xml.ShouldSerializedefaultStyle());
        }

        [Test]
        public void ShouldSerializeDefaultStyle_WhenMatrixTransformSet_ReturnsFalse()
        {
            var xml = new WETextDataXml
            {
                defaultStyle = new WETextDataXml.DefaultStyleXml(),
                matrixTransform = new WETextDataXml.MeshDataMatrixTransformXml()
            };
            Assert.IsFalse(xml.ShouldSerializedefaultStyle());
        }

        [Test]
        public void ShouldSerializeDefaultStyle_WhenLayoutMeshSet_ReturnsFalse()
        {
            var xml = new WETextDataXml
            {
                defaultStyle = new WETextDataXml.DefaultStyleXml(),
                layoutMesh = new WETextDataXml.MeshDataPlaceholderXml()
            };
            Assert.IsFalse(xml.ShouldSerializedefaultStyle());
        }

        [Test]
        public void ShouldSerializeGlassStyle_WhenSetAndNoMatrixOrLayout_ReturnsTrue()
        {
            var xml = new WETextDataXml { glassStyle = new WETextDataXml.GlassStyleXml() };
            Assert.IsTrue(xml.ShouldSerializeglassStyle());
        }

        [Test]
        public void ShouldSerializeGlassStyle_WhenMatrixTransformSet_ReturnsFalse()
        {
            var xml = new WETextDataXml
            {
                glassStyle = new WETextDataXml.GlassStyleXml(),
                matrixTransform = new WETextDataXml.MeshDataMatrixTransformXml()
            };
            Assert.IsFalse(xml.ShouldSerializeglassStyle());
        }

        [Test]
        public void ShouldSerializeDecalStyle_WhenSetAndNoMatrixOrLayout_ReturnsTrue()
        {
            var xml = new WETextDataXml { decalStyle = new WETextDataXml.DecalStyleXml() };
            Assert.IsTrue(xml.ShouldSerializedecalStyle());
        }

        [Test]
        public void ShouldSerializeDecalStyle_WhenLayoutMeshSet_ReturnsFalse()
        {
            var xml = new WETextDataXml
            {
                decalStyle = new WETextDataXml.DecalStyleXml(),
                layoutMesh = new WETextDataXml.MeshDataPlaceholderXml()
            };
            Assert.IsFalse(xml.ShouldSerializedecalStyle());
        }

        [Test]
        public void ShouldSerializeScaler_WhenMatrixTransformNull_ReturnsFalse()
        {
            var xml = new WETextDataXml();
            Assert.IsFalse(xml.ShouldSerializescaler());
        }

        [Test]
        public void ShouldSerializeScaler_WhenMatrixTransformSet_ReturnsTrue()
        {
            var xml = new WETextDataXml { matrixTransform = new WETextDataXml.MeshDataMatrixTransformXml() };
            Assert.IsTrue(xml.ShouldSerializescaler());
        }

        // ── XML serialization ─────────────────────────────────────────────

        [Test]
        public void XmlSerialization_ItemName_RoundTrip()
        {
            var input = new WETextDataXml { itemName = "RoundTripItem" };
            var serializer = new XmlSerializer(typeof(WETextDataXml));
            var sw = new StringWriter();
            serializer.Serialize(sw, input);
            var result = (WETextDataXml)serializer.Deserialize(new StringReader(sw.ToString()));
            Assert.AreEqual("RoundTripItem", result.itemName);
        }

        [Test]
        public void XmlSerialization_ProducesValidXml()
        {
            var serializer = new XmlSerializer(typeof(WETextDataXml));
            var sw = new StringWriter();
            serializer.Serialize(sw, new WETextDataXml { itemName = "Test" });
            Assert.DoesNotThrow(() => XDocument.Parse(sw.ToString()));
        }

        [Test]
        public void FromXML_GarbageString_ReturnsNullAndDoesNotThrowNullReferenceException()
        {
            WETextDataXmlTree result = null;
            Assert.DoesNotThrow(() => result = WETextDataXmlTree.FromXML("!!not valid xml!!"));
            Assert.IsNull(result);
        }

        // ── Sub-type initial states ────────────────────────────────────────

        [Test]
        public void DefaultStyleXml_DefaultDecalFlags_EqualsDefaultDecalFlagsConstant()
        {
            var style = new WETextDataXml.DefaultStyleXml();
            Assert.AreEqual(WETextDataMaterial.DEFAULT_DECAL_FLAGS, style.decalFlags);
        }

        [Test]
        public void GlassStyleXml_DefaultDecalFlags_EqualsDefaultDecalFlagsConstant()
        {
            var style = new WETextDataXml.GlassStyleXml();
            Assert.AreEqual(WETextDataMaterial.DEFAULT_DECAL_FLAGS, style.decalFlags);
        }

        [Test]
        public void FormulaeStringXml_ShouldSerializeFormulae_WhenNullFormulae_ReturnsFalse()
        {
            var f = new WETextDataXml.FormulaeStringXml();
            Assert.IsFalse(f.ShouldSerializeformulae());
        }

        [Test]
        public void FormulaeStringXml_ShouldSerializeFormulae_WhenFormulaeSet_ReturnsTrue()
        {
            var f = new WETextDataXml.FormulaeStringXml { formulae = "someFormula" };
            Assert.IsTrue(f.ShouldSerializeformulae());
        }

        [Test]
        public void MeshDataTextXml_FontName_RoundTrip()
        {
            var mesh = new WETextDataXml.MeshDataTextXml { fontName = "Arial" };
            Assert.AreEqual("Arial", mesh.fontName);
        }

        [Test]
        public void MeshDataImageXml_Atlas_RoundTrip()
        {
            var mesh = new WETextDataXml.MeshDataImageXml { atlas = "myAtlas" };
            Assert.AreEqual("myAtlas", mesh.atlas);
        }
    }
}
