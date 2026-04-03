using NUnit.Framework;

namespace BelzontWE.Tests.Utils
{
    // Tests for WEFormulaeEvalCore tokenizer methods (SR-06).
    // These exercise the internal TokenizeFormula / IsMethodCallToken / ClassifyToken
    // methods without requiring IL generation or a running ECS world.

    [TestFixture]
    public class WEFormulaeEvalCoreTests
    {
        // ── TokenizeFormula ────────────────────────────────────────────────────

        [Test]
        public void TokenizeFormula_NullInput_ReturnsNull()
        {
            var result = WEFormulaeEvalCore.TokenizeFormula(null);
            Assert.IsNull(result);
        }

        [Test]
        public void TokenizeFormula_EmptyString_ReturnsSingleEmptyToken()
        {
            var result = WEFormulaeEvalCore.TokenizeFormula("");
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("", result[0]);
        }

        [Test]
        public void TokenizeFormula_SingleSegment_ReturnsSingleToken()
        {
            var result = WEFormulaeEvalCore.TokenizeFormula("ComponentType;field");
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("ComponentType;field", result[0]);
        }

        [Test]
        public void TokenizeFormula_TwoSegments_ReturnsTwoTokens()
        {
            var result = WEFormulaeEvalCore.TokenizeFormula("TypeA;field/TypeB;value");
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual("TypeA;field", result[0]);
            Assert.AreEqual("TypeB;value", result[1]);
        }

        [Test]
        public void TokenizeFormula_ThreeSegmentChain_ReturnsThreeTokens()
        {
            var result = WEFormulaeEvalCore.TokenizeFormula("A;f1/B;f2/C;f3");
            Assert.AreEqual(3, result.Length);
            Assert.AreEqual("A;f1", result[0]);
            Assert.AreEqual("B;f2", result[1]);
            Assert.AreEqual("C;f3", result[2]);
        }

        [Test]
        public void TokenizeFormula_MethodCallSegment_ReturnedUnmodified()
        {
            var result = WEFormulaeEvalCore.TokenizeFormula("&WEVehicleFn;GetVehiclePlate");
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("&WEVehicleFn;GetVehiclePlate", result[0]);
        }

        [Test]
        public void TokenizeFormula_MixedChain_ComponentThenMethod()
        {
            var result = WEFormulaeEvalCore.TokenizeFormula("TypeA;field/&WEVehicleFn;GetVehiclePlate");
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual("TypeA;field", result[0]);
            Assert.AreEqual("&WEVehicleFn;GetVehiclePlate", result[1]);
        }

        [Test]
        public void TokenizeFormula_WhitespaceFormula_TreatedAsLiteral()
        {
            var result = WEFormulaeEvalCore.TokenizeFormula("   ");
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("   ", result[0]);
        }

        [Test]
        public void TokenizeFormula_MalformedNoBrackets_ReturnsToken()
        {
            // Malformed (no semicolon) — tokenizer returns it as-is; IL layer handles error
            var result = WEFormulaeEvalCore.TokenizeFormula("malformed");
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("malformed", result[0]);
        }

        // ── IsMethodCallToken ──────────────────────────────────────────────────

        [Test]
        public void IsMethodCallToken_AmpersandPrefix_ReturnsTrue()
        {
            Assert.IsTrue(WEFormulaeEvalCore.IsMethodCallToken("&WEVehicleFn;GetVehiclePlate"));
        }

        [Test]
        public void IsMethodCallToken_NoAmpersand_ReturnsFalse()
        {
            Assert.IsFalse(WEFormulaeEvalCore.IsMethodCallToken("ComponentType;field"));
        }

        [Test]
        public void IsMethodCallToken_NullToken_ReturnsFalse()
        {
            Assert.IsFalse(WEFormulaeEvalCore.IsMethodCallToken(null));
        }

        [Test]
        public void IsMethodCallToken_EmptyString_ReturnsFalse()
        {
            Assert.IsFalse(WEFormulaeEvalCore.IsMethodCallToken(""));
        }

        // ── ClassifyToken ──────────────────────────────────────────────────────

        [Test]
        public void ClassifyToken_MethodCallToken_ReturnsMethod()
        {
            Assert.AreEqual("method", WEFormulaeEvalCore.ClassifyToken("&Fn;method"));
        }

        [Test]
        public void ClassifyToken_ComponentToken_ReturnsComponent()
        {
            Assert.AreEqual("component", WEFormulaeEvalCore.ClassifyToken("ComponentType;field"));
        }

        [Test]
        public void ClassifyToken_NullToken_ReturnsNull()
        {
            Assert.IsNull(WEFormulaeEvalCore.ClassifyToken(null));
        }
    }
}
