using NUnit.Framework;

namespace BelzontWE.Tests.IO
{
    [TestFixture]
    public class WESelflessTextDataTreeTests
    {
        // ── FromXML guard tests ───────────────────────────────────────────────

        [Test]
        public void FromXML_Null_ReturnsNull()
        {
            Assert.IsNull(WESelflessTextDataTree.FromXML(null));
        }

        [Test]
        public void FromXML_EmptyString_ReturnsNull()
        {
            Assert.IsNull(WESelflessTextDataTree.FromXML(""));
        }

        [Test]
        public void FromXML_GarbageText_ReturnsNull()
        {
            Assert.IsNull(WESelflessTextDataTree.FromXML("not valid xml at all!!!"));
        }

        [Test]
        public void FromXML_WrongRootElement_ReturnsNull()
        {
            Assert.IsNull(WESelflessTextDataTree.FromXML("<NotWELayout/>"));
        }

        [Test]
        public void FromXML_ValidEmptyWELayout_ReturnsNonNull()
        {
            var result = WESelflessTextDataTree.FromXML("<WELayout/>");
            Assert.IsNotNull(result);
        }

        [Test]
        public void FromXML_ValidEmptyWELayout_ChildrenIsNull()
        {
            var result = WESelflessTextDataTree.FromXML("<WELayout/>");
            Assert.IsNull(result.children);
        }

        // ── Default state ─────────────────────────────────────────────────────

        [Test]
        public void DefaultInstance_ChildrenIsNull()
        {
            var tree = new WESelflessTextDataTree();
            Assert.IsNull(tree.children);
        }

        // ── Equality ──────────────────────────────────────────────────────────

        [Test]
        public void Equals_Null_ReturnsFalse()
        {
            var tree = new WESelflessTextDataTree();
            Assert.IsFalse(tree.Equals(null));
        }

        [Test]
        public void Equals_SameReference_ReturnsTrue()
        {
            var tree = new WESelflessTextDataTree();
            Assert.IsTrue(tree.Equals(tree));
        }

        [Test]
        public void Equals_TwoDefaultInstances_ReturnsTrueByDefaultEquality()
        {
            var a = new WESelflessTextDataTree();
            var b = new WESelflessTextDataTree();
            Assert.IsTrue(a.Equals(b));
        }

        [Test]
        public void EqualsObject_Null_ReturnsFalse()
        {
            var tree = new WESelflessTextDataTree { children = null };
            Assert.IsFalse(tree.Equals((object)null));
        }

        [Test]
        public void OperatorEqual_BothDefault_ReturnsTrue()
        {
            var a = new WESelflessTextDataTree();
            var b = new WESelflessTextDataTree();
            Assert.IsTrue(a == b);
        }

        [Test]
        public void OperatorNotEqual_BothDefault_ReturnsFalse()
        {
            var a = new WESelflessTextDataTree();
            var b = new WESelflessTextDataTree();
            Assert.IsFalse(a != b);
        }

        [Test]
        public void OperatorEqual_LeftNull_ReturnsFalse()
        {
            var b = new WESelflessTextDataTree();
            Assert.IsFalse(null == b);
        }

        [Test]
        public void OperatorEqual_BothNull_ReturnsTrue()
        {
            WESelflessTextDataTree a = null;
            WESelflessTextDataTree b = null;
            Assert.IsTrue(a == b);
        }
    }
}
