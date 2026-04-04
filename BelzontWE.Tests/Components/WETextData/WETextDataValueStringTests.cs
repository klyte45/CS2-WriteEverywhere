using BelzontWE.Tests.Utils;
using BelzontWE.Utils;
using NSubstitute;
using NUnit.Framework;
using System.Collections;
using System.Reflection;
using Unity.Entities;
using static BelzontWE.WEFormulaeHelper;

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

        // ── UpdateEffectiveValue(IECSReader) overload ─────────────────────────

        [Test]
        public void UpdateEffectiveValue_IECSReader_NoFormulae_SetsEffectiveToDefault()
        {
            var v = new WETextDataValueString();
            v.DefaultValue = "hello";
            v.UpdateEffectiveValue(new NullECSReader(), Entity.Null, null);
            Assert.AreEqual("hello", v.EffectiveValue.ToString());
        }

        [Test]
        public void UpdateEffectiveValue_IECSReader_NoFormulae_SetsInitializedTrue()
        {
            var v = new WETextDataValueString();
            v.UpdateEffectiveValue(new NullECSReader(), Entity.Null, null);
            Assert.IsTrue(v.InitializedEffectiveText);
        }

        [Test]
        public void UpdateEffectiveValue_IECSReader_NoFormulae_SecondCallReturnsFalse()
        {
            var v = new WETextDataValueString();
            v.DefaultValue = "world";
            v.UpdateEffectiveValue(new NullECSReader(), Entity.Null, null);
            var changed = v.UpdateEffectiveValue(new NullECSReader(), Entity.Null, null);
            Assert.IsFalse(changed);
        }

        // ── UpdateEffectiveValue with injected formula (FE-05) ────────────────

        private static void SetLoadingDone(ref WETextDataValueString v)
        {
            object boxed = v;
            typeof(WETextDataValueString).GetField("loadingFnDone", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(boxed, true);
            v = (WETextDataValueString)boxed;
        }

        private static void InjectForString(string key, string val, out IDictionary cache)
        {
            var field = typeof(WEFormulaeHelper).GetField("cachedFnsString", BindingFlags.Static | BindingFlags.NonPublic);
            cache = (IDictionary)field.GetValue(null);
            var baseType = typeof(WEFormulaeHelper).GetNestedType("BaseCache`1", BindingFlags.NonPublic).MakeGenericType(typeof(string));
            var inst = System.Activator.CreateInstance(baseType);
            baseType.GetMethod("SetData", BindingFlags.Instance | BindingFlags.Public).Invoke(inst, new object[] { (byte)0, (FormulaeFn<string>)((em, e, v2) => val), null });
            cache[key] = inst;
        }

        [Test]
        public void UpdateEffectiveValue_IECSReader_ValidFormula_ReturnsFormulaResult()
        {
            string key = "fe05_str_valid";
            InjectForString(key, "hello world", out var cache);
            try
            {
                var v = new WETextDataValueString();
                v.Formulae = key;
                SetLoadingDone(ref v);
                var reader = Substitute.For<IECSReader>();
                v.UpdateEffectiveValue(reader, Entity.Null, null);
                Assert.AreEqual("hello world", v.EffectiveValue.ToString());
            }
            finally { cache.Remove(key); }
        }

        [Test]
        public void UpdateEffectiveValue_IECSReader_UnknownFormula_ReturnsNullFnFallback()
        {
            var v = new WETextDataValueString();
            v.Formulae = "fe05_str_nosuch";
            SetLoadingDone(ref v);
            var reader = Substitute.For<IECSReader>();
            v.UpdateEffectiveValue(reader, Entity.Null, null);
            Assert.AreEqual("<InvalidFn2>", v.EffectiveValue.ToString(), "Unknown formula should yield '<InvalidFn2>' (nullFnFallback)");
        }

        [Test]
        public void UpdateEffectiveValue_IECSReader_ValidFormula_SetsInitializedTrue()
        {
            string key = "fe05_str_init";
            InjectForString(key, "init_check", out var cache);
            try
            {
                var v = new WETextDataValueString();
                v.Formulae = key;
                SetLoadingDone(ref v);
                var reader = Substitute.For<IECSReader>();
                v.UpdateEffectiveValue(reader, Entity.Null, null);
                Assert.IsTrue(v.InitializedEffectiveText);
            }
            finally { cache.Remove(key); }
        }
    }
}
