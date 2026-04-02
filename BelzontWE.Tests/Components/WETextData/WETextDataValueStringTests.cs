using NUnit.Framework;
using System.Reflection;

namespace BelzontWE.Tests.Components
{
    [TestFixture]
    public class WETextDataValueStringTests
    {
        private static WEFormulaeEvalCore.EvalConfig<string> GetStringConfig()
        {
            var field = typeof(WETextDataValueString)
                .GetField("s_config", BindingFlags.NonPublic | BindingFlags.Static);
            return (WEFormulaeEvalCore.EvalConfig<string>)field.GetValue(null);
        }

        // ── Initial state ─────────────────────────────────────────────────

        [Test]
        public void InitialState_IsEmptyIsTrue()
        {
            var v = new WETextDataValueString();
            Assert.IsTrue(v.IsEmpty);
        }

        [Test]
        public void InitialState_DefaultValueIsEmpty()
        {
            var v = new WETextDataValueString();
            Assert.AreEqual("", v.DefaultValue);
        }

        [Test]
        public void InitialState_FormulaeIsEmpty()
        {
            var v = new WETextDataValueString();
            Assert.AreEqual("", v.Formulae);
        }

        [Test]
        public void InitialState_InitializedEffectiveTextIsFalse()
        {
            var v = new WETextDataValueString();
            Assert.IsFalse(v.InitializedEffectiveText);
        }

        // ── DefaultValue round-trip via WEStringsBank ──────────────────────

        [Test]
        public void DefaultValue_Setter_RoundTrip()
        {
            var v = new WETextDataValueString();
            v.DefaultValue = "hello world";
            Assert.AreEqual("hello world", v.DefaultValue);
        }

        [Test]
        public void DefaultValue_SetTwice_ReturnsLastValue()
        {
            var v = new WETextDataValueString();
            v.DefaultValue = "first";
            v.DefaultValue = "second";
            Assert.AreEqual("second", v.DefaultValue);
        }

        // ── IsEmpty behaviour ─────────────────────────────────────────────

        [Test]
        public void IsEmpty_AfterDefaultValueSet_IsFalse()
        {
            var v = new WETextDataValueString();
            v.DefaultValue = "some text";
            Assert.IsFalse(v.IsEmpty);
        }

        [Test]
        public void IsEmpty_AfterFormulaeSet_IsFalse()
        {
            var v = new WETextDataValueString();
            v.Formulae = "some.formulae";
            Assert.IsFalse(v.IsEmpty);
        }

        [Test]
        public void IsEmpty_EmptyStringDefaultValue_StaysTrue()
        {
            var v = new WETextDataValueString();
            v.DefaultValue = "";
            Assert.IsTrue(v.IsEmpty);
        }

        // ── SetFormulae reset behaviour ────────────────────────────────────

        [Test]
        public void SetFormulae_EmptyString_ClearsBankIndex()
        {
            var v = new WETextDataValueString();
            v.Formulae = "some.formulae";
            v.SetFormulae("", out _);
            Assert.AreEqual("", v.Formulae);
        }

        [Test]
        public void SetFormulae_Null_ClearsBankIndex()
        {
            var v = new WETextDataValueString();
            v.Formulae = "some.formulae";
            v.SetFormulae(null, out _);
            Assert.AreEqual("", v.Formulae);
        }

        [Test]
        public void SetFormulae_EmptyString_ReturnsZero()
        {
            var v = new WETextDataValueString();
            var result = v.SetFormulae("", out _);
            Assert.AreEqual(0, result);
        }

        // ── s_config fallback values ───────────────────────────────────────

        [Test]
        public void Config_ErrorFallback_IsErrorTag()
        {
            var config = GetStringConfig();
            Assert.AreEqual("<ERROR>", config.errorFallback);
        }

        [Test]
        public void Config_NullFnFallback_IsInvalidFn2Tag()
        {
            var config = GetStringConfig();
            Assert.AreEqual("<InvalidFn2>", config.nullFnFallback);
        }
    }
}
