using NUnit.Framework;
using static BelzontWE.WETypeMathOperationDesc;

namespace BelzontWE.Tests.IO
{
    [TestFixture]
    public class WETypeMathOperationDescTests
    {
        // ── WEDescType constant ───────────────────────────────────────────────

        [Test]
        public void WEDescType_ReturnsMathOperation()
        {
            var desc = new WETypeMathOperationDesc();
            Assert.That(desc.WEDescType, Is.EqualTo("MATH_OPERATION"));
        }

        [Test]
        public void SupportsMathOp_AlwaysTrue()
        {
            var desc = new WETypeMathOperationDesc();
            Assert.That(desc.supportsMathOp, Is.True);
        }

        // ── Inner enum contracts ──────────────────────────────────────────────

        [Test]
        public void WEFormulaeMathOperation_HasTwelveValues()
            => Assert.That(System.Enum.GetValues(typeof(WEFormulaeMathOperation)).Length, Is.EqualTo(12));

        [Test]
        public void EnforceType_HasThreeValues()
            => Assert.That(System.Enum.GetValues(typeof(EnforceType)).Length, Is.EqualTo(3));

        [Test] public void ADD_HasValue_Zero() => Assert.That((int)WEFormulaeMathOperation.ADD, Is.EqualTo(0));
        [Test] public void NOT_HasValue_Eleven() => Assert.That((int)WEFormulaeMathOperation.NOT, Is.EqualTo(11));
        [Test] public void EnforceType_None_IsZero() => Assert.That((int)EnforceType.None, Is.EqualTo(0));
        [Test] public void EnforceType_Float_IsOne() => Assert.That((int)EnforceType.Float, Is.EqualTo(1));
        [Test] public void EnforceType_Double_IsTwo() => Assert.That((int)EnforceType.Double, Is.EqualTo(2));

        // ── From factory ─────────────────────────────────────────────────────

        [Test]
        public void From_SetsOperation()
        {
            var desc = WETypeMathOperationDesc.From(WEFormulaeMathOperation.ADD, 1.5f, EnforceType.Float, true);
            Assert.That(desc.operation, Is.EqualTo(WEFormulaeMathOperation.ADD));
        }

        [Test]
        public void From_SetsValueAsString()
        {
            var desc = WETypeMathOperationDesc.From(WEFormulaeMathOperation.MULTIPLY, 3.14f, EnforceType.None, false);
            Assert.That(desc.value, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void From_SetsEnforceType()
        {
            var desc = WETypeMathOperationDesc.From(WEFormulaeMathOperation.ADD, 0f, EnforceType.Double, false);
            Assert.That(desc.enforceType, Is.EqualTo(EnforceType.Double));
        }

        [Test]
        public void From_SetsIsDecimalResult()
        {
            var desc = WETypeMathOperationDesc.From(WEFormulaeMathOperation.ADD, 0f, EnforceType.None, true);
            Assert.That(desc.isDecimalResult, Is.True);
        }

        [Test]
        public void From_IsDecimalFalse_StoredCorrectly()
        {
            var desc = WETypeMathOperationDesc.From(WEFormulaeMathOperation.ADD, 0f, EnforceType.None, false);
            Assert.That(desc.isDecimalResult, Is.False);
        }
    }

    [TestFixture]
    public class WEXmlMetadataTests
    {
        [Test]
        public void DefaultInstance_AllFieldsNull()
        {
            var meta = new WEXmlMetadata();
            Assert.That(meta.dll, Is.Null);
            Assert.That(meta.refName, Is.Null);
            Assert.That(meta.content, Is.Null);
        }

        [Test]
        public void FieldAssignment_RoundTrips()
        {
            var meta = new WEXmlMetadata
            {
                dll = "mscorlib",
                refName = "System.String",
                content = "some content"
            };
            Assert.That(meta.dll, Is.EqualTo("mscorlib"));
            Assert.That(meta.refName, Is.EqualTo("System.String"));
            Assert.That(meta.content, Is.EqualTo("some content"));
        }
    }

    [TestFixture]
    public class WETextItemResumeTests
    {
        [Test]
        public void DefaultInstance_NameIsNull()
        {
            var item = new WETextItemResume();
            Assert.That(item.name, Is.Null);
        }

        [Test]
        public void DefaultInstance_TypeIsZero()
        {
            var item = new WETextItemResume();
            Assert.That(item.type, Is.EqualTo(0));
        }

        [Test]
        public void DefaultInstance_ChildrenIsNull()
        {
            var item = new WETextItemResume();
            Assert.That(item.children, Is.Null);
        }

        [Test]
        public void FieldAssignment_NameAndType_RoundTrip()
        {
            var item = new WETextItemResume { name = "sign01", type = 1 };
            Assert.That(item.name, Is.EqualTo("sign01"));
            Assert.That(item.type, Is.EqualTo(1));
        }

        [Test]
        public void Children_CanBeSetToArray()
        {
            var child = new WETextItemResume { name = "child", type = 2 };
            var parent = new WETextItemResume { name = "parent", children = new[] { child } };
            Assert.That(parent.children, Has.Length.EqualTo(1));
            Assert.That(parent.children[0].name, Is.EqualTo("child"));
        }
    }
}
