using NUnit.Framework;
using Unity.Entities;
using Unity.Collections;

namespace BelzontWE.Tests.Components
{
    [TestFixture]
    public class WETextDataMeshTests
    {
        // ── TextType setter dirty flags ────────────────────────────────────

        [Test]
        public void TextType_Setter_SetsDirtyTrue()
        {
            var m = new WETextDataMesh();
            m.TextType = WESimulationTextType.Text;
            Assert.IsTrue(m.IsDirty());
        }

        [Test]
        public void TextType_Setter_SetsTemplateDirtyTrue()
        {
            var m = new WETextDataMesh();
            m.TextType = WESimulationTextType.Text;
            Assert.IsTrue(m.IsTemplateDirty());
        }

        [Test]
        public void TextType_Setter_StoresValue()
        {
            var m = new WETextDataMesh();
            m.TextType = WESimulationTextType.Image;
            Assert.AreEqual(WESimulationTextType.Image, m.TextType);
        }

        // ── Atlas setter dirty flags ───────────────────────────────────────

        [Test]
        public void Atlas_Setter_SetsDirtyTrue()
        {
            var m = new WETextDataMesh();
            m.Atlas = new FixedString64Bytes("myAtlas");
            Assert.IsTrue(m.IsDirty());
        }

        [Test]
        public void Atlas_Setter_SetsTemplateDirtyTrue()
        {
            var m = new WETextDataMesh();
            m.Atlas = new FixedString64Bytes("myAtlas");
            Assert.IsTrue(m.IsTemplateDirty());
        }

        [Test]
        public void Atlas_Setter_StoresValue()
        {
            var m = new WETextDataMesh();
            m.Atlas = new FixedString64Bytes("atlasName");
            Assert.AreEqual(new FixedString64Bytes("atlasName"), m.Atlas);
        }

        // ── FontName setter dirty flags ────────────────────────────────────

        [Test]
        public void FontName_Setter_SetsDirtyTrue()
        {
            var m = new WETextDataMesh();
            m.FontName = new FixedString64Bytes("myFont");
            Assert.IsTrue(m.IsDirty());
        }

        [Test]
        public void FontName_Setter_SetsTemplateDirtyTrue()
        {
            var m = new WETextDataMesh();
            m.FontName = new FixedString64Bytes("myFont");
            Assert.IsTrue(m.IsTemplateDirty());
        }

        [Test]
        public void FontName_Setter_StoresValue()
        {
            var m = new WETextDataMesh();
            m.FontName = new FixedString64Bytes("fontABC");
            Assert.AreEqual(new FixedString64Bytes("fontABC"), m.FontName);
        }

        // ── ResetBri ──────────────────────────────────────────────────────

        [Test]
        public void ResetBri_SetsHasBRIFalse()
        {
            var m = new WETextDataMesh();
            m.ResetBri();
            Assert.IsFalse(m.HasBRI);
        }

        [Test]
        public void ResetBri_SetsMinLodToZero()
        {
            var m = new WETextDataMesh();
            m.MinLod = 5;
            m.ResetBri();
            Assert.AreEqual(0, m.MinLod);
        }

        // ── ClearTemplateDirty ────────────────────────────────────────────

        [Test]
        public void ClearTemplateDirty_AfterSetDirty_ReturnsFalse()
        {
            var m = new WETextDataMesh();
            m.TextType = WESimulationTextType.Text;
            m.ClearTemplateDirty();
            Assert.IsFalse(m.IsTemplateDirty());
        }

        // ── CreateDefault ─────────────────────────────────────────────────

        [Test]
        public void CreateDefault_ValueDataDefaultValueIsNewText()
        {
            var m = WETextDataMesh.CreateDefault(Entity.Null);
            Assert.AreEqual("NEW TEXT", m.ValueData.DefaultValue);
        }

        [Test]
        public void CreateDefault_IsNotDirty()
        {
            var m = WETextDataMesh.CreateDefault(Entity.Null);
            Assert.IsFalse(m.IsDirty());
        }
    }
}
