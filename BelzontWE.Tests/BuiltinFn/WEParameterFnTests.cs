using NUnit.Framework;
using System.Collections.Generic;
using Unity.Entities;
using BelzontWE.Builtin;

namespace BelzontWE.Tests.BuiltinFn
{
    [TestFixture]
    public class WEParameterFnTests
    {
        private static readonly Entity DummyEntity = Entity.Null;

        // ── PrintVariables ────────────────────────────────────────────────────

        [Test]
        public void PrintVariables_EmptyDict_ReturnsEmptyString()
        {
            var vars = new Dictionary<string, string>();
            Assert.AreEqual("", WEParameterFn.PrintVariables(DummyEntity, vars));
        }

        [Test]
        public void PrintVariables_OneEntry_ReturnsKeyEqualsValue()
        {
            var vars = new Dictionary<string, string> { ["color"] = "red" };
            Assert.AreEqual("color=red", WEParameterFn.PrintVariables(DummyEntity, vars));
        }

        [Test]
        public void PrintVariables_TwoEntries_JoinedBySemicolon()
        {
            var vars = new Dictionary<string, string> { ["a"] = "1", ["b"] = "2" };
            var result = WEParameterFn.PrintVariables(DummyEntity, vars);
            Assert.IsTrue(result.Contains("a=1"), $"Expected 'a=1' in '{result}'");
            Assert.IsTrue(result.Contains("b=2"), $"Expected 'b=2' in '{result}'");
            Assert.IsTrue(result.Contains(";"), $"Expected ';' separator in '{result}'");
        }

        // ── RelVarStr — missing key ────────────────────────────────────────────

        [Test]
        public void RelVarStr1_NoRelKey_ReturnsEmpty()
        {
            var vars = new Dictionary<string, string>();
            Assert.AreEqual("", WEParameterFn.RelVarStr1(DummyEntity, vars));
        }

        [Test]
        public void RelVarStr2_NoRelKey_ReturnsEmpty()
        {
            var vars = new Dictionary<string, string>();
            Assert.AreEqual("", WEParameterFn.RelVarStr2(DummyEntity, vars));
        }

        [Test]
        public void RelVarStr1_RelKeyExistsButTargetMissing_ReturnsEmpty()
        {
            // !!r1 points to "myKey", but "myKey" not in dict
            var vars = new Dictionary<string, string> { ["!!r1"] = "myKey" };
            Assert.AreEqual("", WEParameterFn.RelVarStr1(DummyEntity, vars));
        }

        [Test]
        public void RelVarStr1_RelKeyAndTargetExist_ReturnsTargetValue()
        {
            var vars = new Dictionary<string, string>
            {
                ["!!r1"] = "theKey",
                ["theKey"] = "theValue"
            };
            Assert.AreEqual("theValue", WEParameterFn.RelVarStr1(DummyEntity, vars));
        }

        [Test]
        public void RelVarStr3_RelKeyAndTargetExist_ReturnsValue()
        {
            var vars = new Dictionary<string, string>
            {
                ["!!r3"] = "key3",
                ["key3"] = "hello"
            };
            Assert.AreEqual("hello", WEParameterFn.RelVarStr3(DummyEntity, vars));
        }

        // ── RelVarInt ────────────────────────────────────────────────────────

        [Test]
        public void RelVarInt1_NoKey_ReturnsZero()
        {
            var vars = new Dictionary<string, string>();
            Assert.AreEqual(0, WEParameterFn.RelVarInt1(DummyEntity, vars));
        }

        [Test]
        public void RelVarInt1_ValueIsNonNumeric_ReturnsZero()
        {
            var vars = new Dictionary<string, string>
            {
                ["!!r1"] = "k",
                ["k"] = "notAnInt"
            };
            Assert.AreEqual(0, WEParameterFn.RelVarInt1(DummyEntity, vars));
        }

        [Test]
        public void RelVarInt1_ValueIsParseable_ReturnsInt()
        {
            var vars = new Dictionary<string, string>
            {
                ["!!r1"] = "num",
                ["num"] = "42"
            };
            Assert.AreEqual(42, WEParameterFn.RelVarInt1(DummyEntity, vars));
        }

        [Test]
        public void RelVarInt2_ValueIsParseable_ReturnsInt()
        {
            var vars = new Dictionary<string, string>
            {
                ["!!r2"] = "x",
                ["x"] = "99"
            };
            Assert.AreEqual(99, WEParameterFn.RelVarInt2(DummyEntity, vars));
        }

        // ── RelVar independence ────────────────────────────────────────────────

        [Test]
        public void RelVarStr1And2_AreIndependent()
        {
            var vars = new Dictionary<string, string>
            {
                ["!!r1"] = "k1",
                ["!!r2"] = "k2",
                ["k1"] = "val1",
                ["k2"] = "val2"
            };
            Assert.AreEqual("val1", WEParameterFn.RelVarStr1(DummyEntity, vars));
            Assert.AreEqual("val2", WEParameterFn.RelVarStr2(DummyEntity, vars));
        }

        // ── High-index RelVars ────────────────────────────────────────────────

        [Test]
        public void RelVarStr8_RelKeyExistsWithValue_ReturnsValue()
        {
            var vars = new Dictionary<string, string>
            {
                ["!!r8"] = "k8",
                ["k8"] = "eight"
            };
            Assert.AreEqual("eight", WEParameterFn.RelVarStr8(DummyEntity, vars));
        }

        [Test]
        public void RelVarInt8_ValueIsParseable_ReturnsInt()
        {
            var vars = new Dictionary<string, string>
            {
                ["!!r8"] = "m",
                ["m"] = "8"
            };
            Assert.AreEqual(8, WEParameterFn.RelVarInt8(DummyEntity, vars));
        }
    }
}
