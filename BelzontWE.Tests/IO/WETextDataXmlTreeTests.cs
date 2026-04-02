using NUnit.Framework;

namespace BelzontWE.Tests.IO
{
    [TestFixture]
    public class WETextDataXmlTreeTests
    {
        // â”€â”€ FromXML null / empty / invalid guard â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void FromXML_Null_ReturnsNull()
        {
            Assert.IsNull(WETextDataXmlTree.FromXML(null));
        }

        [Test]
        public void FromXML_EmptyString_ReturnsNull()
        {
            Assert.IsNull(WETextDataXmlTree.FromXML(""));
        }

        [Test]
        public void FromXML_NotXml_ReturnsNull()
        {
            Assert.IsNull(WETextDataXmlTree.FromXML("not xml at all!!!"));
        }

        [Test]
        public void FromXML_WrongRootElement_ReturnsNull()
        {
            Assert.IsNull(WETextDataXmlTree.FromXML("<SomeOtherRoot />"));
        }

        // â”€â”€ Default state â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void NewInstance_GuidIsNonDefault()
        {
            var tree = new WETextDataXmlTree();
            Assert.AreNotEqual(default(Colossal.Hash128), tree.Guid);
        }

        [Test]
        public void NewInstance_ChildrenEmptyByDefault()
        {
            var tree = new WETextDataXmlTree();
            Assert.AreEqual(0, tree.children.Length);
        }

        [Test]
        public void NewInstance_VariablesEmptyByDefault()
        {
            var tree = new WETextDataXmlTree();
            Assert.AreEqual(0, tree.variables.Length);
        }

        [Test]
        public void NewInstance_MetadatasEmptyByDefault()
        {
            var tree = new WETextDataXmlTree();
            Assert.AreEqual(0, tree.metadatas.Length);
        }

        [Test]
        public void NewInstance_MeshesToHideEmptyByDefault()
        {
            var tree = new WETextDataXmlTree();
            Assert.AreEqual(0, tree.MeshesToHide.Length);
        }

        // â”€â”€ ShouldSerializechildren â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void ShouldSerializeChildren_WithNullLayoutMesh_ReturnsTrue()
        {
            var tree = new WETextDataXmlTree
            {
                self = new WETextDataXml() // layoutMesh = null
            };
            Assert.IsTrue(tree.ShouldSerializechildren());
        }

        [Test]
        public void ShouldSerializeChildren_WithLayoutMeshSet_ReturnsFalse()
        {
            var tree = new WETextDataXmlTree
            {
                self = new WETextDataXml { layoutMesh = new WETextDataXml.MeshDataPlaceholderXml() }
            };
            Assert.IsFalse(tree.ShouldSerializechildren());
        }

        // â”€â”€ Field assignment â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void Children_AssignedArray_IsReadBack()
        {
            var tree = new WETextDataXmlTree();
            var child = new WETextDataXmlTree { self = new WETextDataXml { itemName = "c1" } };
            tree.children = new[] { child };
            Assert.AreEqual(1, tree.children.Length);
        }

        [Test]
        public void Variables_AssignedArray_IsReadBack()
        {
            var tree = new WETextDataXmlTree();
            tree.variables = new[] { new WETemplateVariable { key = "k", value = "v" } };
            Assert.AreEqual(1, tree.variables.Length);
            Assert.AreEqual("k", tree.variables[0].key);
            Assert.AreEqual("v", tree.variables[0].value);
        }

        [Test]
        public void Self_AssignedObject_IsReadBack()
        {
            var tree = new WETextDataXmlTree();
            var xml = new WETextDataXml { itemName = "assigned" };
            tree.self = xml;
            Assert.AreEqual("assigned", tree.self.itemName);
        }

        // â”€â”€ Guid uniqueness â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void TwoNewInstances_HaveDifferentGuids()
        {
            var t1 = new WETextDataXmlTree();
            var t2 = new WETextDataXmlTree();
            Assert.AreNotEqual(t1.Guid, t2.Guid);
        }

        // â”€â”€ MergeChildren â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void MergeChildren_CombinesArrays()
        {
            var base1 = new WETextDataXmlTree { self = new WETextDataXml() };
            base1.children = new[] { new WETextDataXmlTree { self = new WETextDataXml { itemName = "a" } } };
            var base2 = new WETextDataXmlTree { self = new WETextDataXml() };
            base2.children = new[] { new WETextDataXmlTree { self = new WETextDataXml { itemName = "b" } } };
            base1.MergeChildren(base2);
            Assert.AreEqual(2, base1.children.Length);
        }

        [Test]
        public void MergeChildren_RegeneratesGuid()
        {
            var t1 = new WETextDataXmlTree { self = new WETextDataXml() };
            var t2 = new WETextDataXmlTree { self = new WETextDataXml() };
            var originalGuid = t1.Guid;
            t1.MergeChildren(t2);
            Assert.AreNotEqual(originalGuid, t1.Guid);
        }

        // â”€â”€ WETemplateVariable round-trip via key/value â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void WETemplateVariable_KeyAndValueAssigned()
        {
            var v = new WETemplateVariable { key = "myKey", value = "myVal" };
            Assert.AreEqual("myKey", v.key);
            Assert.AreEqual("myVal", v.value);
        }

        [Test]
        public void WETemplateVariable_DefaultValues_AreEmptyStrings()
        {
            var v = new WETemplateVariable();
            Assert.AreEqual("", v.key);
            Assert.AreEqual("", v.value);
        }
    }
}
