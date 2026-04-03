using NUnit.Framework;
using BelzontWE;
using BelzontWE.Utils;

namespace BelzontWE.Tests.Utils
{
    [TestFixture]
    public class WEXmlExtensionsTests
    {
        // ── BuildXmlFromComponents: main absence/presence ─────────────────────

        [Test]
        public void BuildXmlFromComponents_NoMain_ReturnsNull()
        {
            var result = WEXmlExtensions.BuildXmlFromComponents(
                hasMain: false, default,
                hasMat: false, default,
                hasMesh: false, default,
                hasTransf: false, default);
            Assert.IsNull(result);
        }

        [Test]
        public void BuildXmlFromComponents_WithMain_ReturnsNotNull()
        {
            var main = new WETextDataMain { ItemName = "test" };
            var result = WEXmlExtensions.BuildXmlFromComponents(
                hasMain: true, main,
                hasMat: false, default,
                hasMesh: false, default,
                hasTransf: false, default);
            Assert.IsNotNull(result);
        }

        [Test]
        public void BuildXmlFromComponents_WithMain_ItemNamePreserved()
        {
            var main = new WETextDataMain { ItemName = "MyItem" };
            var result = WEXmlExtensions.BuildXmlFromComponents(
                hasMain: true, main,
                hasMat: false, default,
                hasMesh: false, default,
                hasTransf: false, default);
            Assert.AreEqual("MyItem", result.itemName);
        }

        // ── Material shader routing ────────────────────────────────────────────

        [Test]
        public void BuildXmlFromComponents_NoMaterial_AllMaterialStylesNull()
        {
            var main = new WETextDataMain { ItemName = "x" };
            var result = WEXmlExtensions.BuildXmlFromComponents(
                hasMain: true, main,
                hasMat: false, default,
                hasMesh: false, default,
                hasTransf: false, default);
            Assert.IsNull(result.defaultStyle);
            Assert.IsNull(result.glassStyle);
            Assert.IsNull(result.decalStyle);
        }

        [Test]
        public void BuildXmlFromComponents_DefaultShader_SetsDefaultStyle()
        {
            var main = new WETextDataMain { ItemName = "x" };
            var mat = new WETextDataMaterial { Shader = WEShader.Default };
            var result = WEXmlExtensions.BuildXmlFromComponents(
                hasMain: true, main,
                hasMat: true, mat,
                hasMesh: false, default,
                hasTransf: false, default);
            Assert.IsNotNull(result.defaultStyle);
            Assert.IsNull(result.glassStyle);
            Assert.IsNull(result.decalStyle);
        }

        [Test]
        public void BuildXmlFromComponents_GlassShader_SetsGlassStyle()
        {
            var main = new WETextDataMain { ItemName = "x" };
            var mat = new WETextDataMaterial { Shader = WEShader.Glass };
            var result = WEXmlExtensions.BuildXmlFromComponents(
                hasMain: true, main,
                hasMat: true, mat,
                hasMesh: false, default,
                hasTransf: false, default);
            Assert.IsNull(result.defaultStyle);
            Assert.IsNotNull(result.glassStyle);
            Assert.IsNull(result.decalStyle);
        }

        [Test]
        public void BuildXmlFromComponents_DecalShader_SetsDecalStyle()
        {
            var main = new WETextDataMain { ItemName = "x" };
            var mat = new WETextDataMaterial { Shader = WEShader.Decal };
            var result = WEXmlExtensions.BuildXmlFromComponents(
                hasMain: true, main,
                hasMat: true, mat,
                hasMesh: false, default,
                hasTransf: false, default);
            Assert.IsNull(result.defaultStyle);
            Assert.IsNull(result.glassStyle);
            Assert.IsNotNull(result.decalStyle);
        }

        // ── Mesh type routing ─────────────────────────────────────────────────

        [Test]
        public void BuildXmlFromComponents_NoMesh_AllMeshStylesNull()
        {
            var main = new WETextDataMain { ItemName = "x" };
            var result = WEXmlExtensions.BuildXmlFromComponents(
                hasMain: true, main,
                hasMat: false, default,
                hasMesh: false, default,
                hasTransf: false, default);
            Assert.IsNull(result.textMesh);
            Assert.IsNull(result.imageMesh);
            Assert.IsNull(result.layoutMesh);
        }

        [Test]
        public void BuildXmlFromComponents_TextMesh_SetsTextMesh()
        {
            var main = new WETextDataMain { ItemName = "x" };
            var mesh = new WETextDataMesh { TextType = WESimulationTextType.Text };
            var result = WEXmlExtensions.BuildXmlFromComponents(
                hasMain: true, main,
                hasMat: false, default,
                hasMesh: true, mesh,
                hasTransf: false, default);
            Assert.IsNotNull(result.textMesh);
            Assert.IsNull(result.imageMesh);
        }

        [Test]
        public void BuildXmlFromComponents_ImageMesh_SetsImageMesh()
        {
            var main = new WETextDataMain { ItemName = "x" };
            var mesh = new WETextDataMesh { TextType = WESimulationTextType.Image };
            var result = WEXmlExtensions.BuildXmlFromComponents(
                hasMain: true, main,
                hasMat: false, default,
                hasMesh: true, mesh,
                hasTransf: false, default);
            Assert.IsNull(result.textMesh);
            Assert.IsNotNull(result.imageMesh);
        }

        // ── Transform ─────────────────────────────────────────────────────────

        [Test]
        public void BuildXmlFromComponents_NoTransform_TransformIsDefault()
        {
            var main = new WETextDataMain { ItemName = "x" };
            var result = WEXmlExtensions.BuildXmlFromComponents(
                hasMain: true, main,
                hasMat: false, default,
                hasMesh: false, default,
                hasTransf: false, default);
            // When hasTransf=false, result.transform = default (null ref or all-zero struct)
            Assert.IsNull(result.transform);
        }

        [Test, Ignore("WETextDataTransform.ToXml() calls Quaternion.eulerAngles (Unity ECall) — requires Unity native runtime")]
        public void BuildXmlFromComponents_WithTransform_TransformIsNotNull()
        {
            var main = new WETextDataMain { ItemName = "x" };
            var transf = new WETextDataTransform(); // default struct — non-null ToXml result
            var result = WEXmlExtensions.BuildXmlFromComponents(
                hasMain: true, main,
                hasMat: false, default,
                hasMesh: false, default,
                hasTransf: true, transf);
            Assert.IsNotNull(result.transform);
        }
    }
}
