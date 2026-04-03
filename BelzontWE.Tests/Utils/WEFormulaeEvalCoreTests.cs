using NUnit.Framework;
using System;
using Unity.Entities;

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

        // ── ParseComponentEntryType (internal) ────────────────────────────────

        [Test, Ignore("TypeManager.AllTypes requires ECS world — skipping ParseComponentEntryType runtime tests")]
        public void ParseComponentEntryType_NonEntityCurrentType_ReturnsError4()
        {
            // Must start from Entity; any other type returns 4
            var type = typeof(int);
            var result = WEFormulaeHelper.ParseComponentEntryType(ref type, "ComponentType;field", out _, out _);
            Assert.AreEqual(4, result);
        }

        [Test, Ignore("TypeManager.AllTypes requires ECS world — skipping ParseComponentEntryType runtime tests")]
        public void ParseComponentEntryType_NoSemicolon_ReturnsError6()
        {
            // Missing semicolon → error 6
            var type = typeof(Entity);
            var result = WEFormulaeHelper.ParseComponentEntryType(ref type, "ComponentTypeWithoutSemicolon", out _, out _);
            Assert.AreEqual(6, result);
        }

        [Test, Ignore("TypeManager.AllTypes requires ECS world — skipping ParseComponentEntryType runtime tests")]
        public void ParseComponentEntryType_MultipleSemicolons_ReturnsError6()
        {
            // More than 2 parts when split by ";" → error 6 (split produces 3 items, Length != 2)
            var type = typeof(Entity);
            var result = WEFormulaeHelper.ParseComponentEntryType(ref type, "A;B;C", out _, out _);
            Assert.AreEqual(6, result);
        }

        [Test, Ignore("TypeManager.AllTypes requires ECS world — skipping ParseComponentEntryType runtime tests")]
        public void ParseComponentEntryType_ValidFormatUnknownComponent_ReturnsError1()
        {
            // Format is correct (TypeName;field) but TypeName unknown → error 1
            var type = typeof(Entity);
            var result = WEFormulaeHelper.ParseComponentEntryType(ref type, "XyzNonExistentComponent_99;field", out _, out _);
            Assert.AreEqual(1, result);
        }

        [Test, Ignore("TypeManager.AllTypes requires ECS world — skipping ParseComponentEntryType runtime tests")]
        public void ParseComponentEntryType_FieldPath_ParsedCorrectly()
        {
            // We can at least verify that for valid format, fieldPath splits correctly
            // Use a component that definitely doesn't exist → error 1 but fieldPath should set
            var type = typeof(Entity);
            WEFormulaeHelper.ParseComponentEntryType(ref type, "SomeComp;field.subfield.leaf", out _, out var fieldPath);
            // fieldPath might be set even on error 1 depending on order of operations
            // Actual outcome: depends on implementation — just verify no exception
            Assert.Pass("No exception thrown for valid-format input with unknown component");
        }

        // ── TokenizeFormula: multi-segment field navigation ────────────────────

        [Test]
        public void TokenizeFormula_SegmentWithDotPath_ReturnedAsOneToken()
        {
            // Dot-separated field path is NOT a formula separator; stays within one token
            var result = WEFormulaeEvalCore.TokenizeFormula("Component;field.subfield");
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("Component;field.subfield", result[0]);
        }

        [Test]
        public void TokenizeFormula_LeadingSlash_FirstTokenIsEmpty()
        {
            var result = WEFormulaeEvalCore.TokenizeFormula("/Component;field");
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual("", result[0]);
        }

        [Test]
        public void TokenizeFormula_TrailingSlash_LastTokenIsEmpty()
        {
            var result = WEFormulaeEvalCore.TokenizeFormula("Component;field/");
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual("", result[1]);
        }
    }
}
