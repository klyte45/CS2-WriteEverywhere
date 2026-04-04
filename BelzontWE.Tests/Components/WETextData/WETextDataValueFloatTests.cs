using NUnit.Framework;
using BelzontWE.Tests.Utils;
using BelzontWE.Utils;
using NSubstitute;
using System.Collections;
using System.Reflection;
using Unity.Entities;
using static BelzontWE.WEFormulaeHelper;

namespace BelzontWE.Tests.Components
{
    [TestFixture]
    public class WETextDataValueFloatTests
    {
        // ── Initial state ─────────────────────────────────────────────────

        [Test]
        public void InitialState_EffectiveValueIsZero()
        {
            var v = new WETextDataValueFloat();
            Assert.AreEqual(0f, v.EffectiveValue);
        }

        [Test]
        public void InitialState_InitializedEffectiveTextIsFalse()
        {
            var v = new WETextDataValueFloat();
            Assert.IsFalse(v.InitializedEffectiveText);
        }

        [Test]
        public void InitialState_DefaultValueIsZero()
        {
            var v = new WETextDataValueFloat();
            Assert.AreEqual(0f, v.defaultValue);
        }

        // ── Formulae property round-trip via WEStringsBank ────────────────

        [Test]
        public void Formulae_Setter_RoundTripViaStringsBank()
        {
            var v = new WETextDataValueFloat();
            v.Formulae = "test.formula.float";
            Assert.AreEqual("test.formula.float", v.Formulae);
        }

        [Test]
        public void Formulae_SetDifferentValues_RoundTripsCorrectly()
        {
            var v = new WETextDataValueFloat();
            v.Formulae = "alpha";
            v.Formulae = "beta";
            Assert.AreEqual("beta", v.Formulae);
        }

        [Test]
        public void Formulae_InitialState_IsEmptyString()
        {
            var v = new WETextDataValueFloat();
            Assert.AreEqual("", v.Formulae);
        }

        // ── SetFormulae reset behaviour ───────────────────────────────────

        [Test]
        public void SetFormulae_EmptyString_ResetsToZeroIndex()
        {
            var v = new WETextDataValueFloat();
            v.Formulae = "test.formula.float";
            v.SetFormulae("", out _);
            Assert.AreEqual("", v.Formulae);
        }

        [Test]
        public void SetFormulae_EmptyString_ReturnsZero()
        {
            var v = new WETextDataValueFloat();
            var result = v.SetFormulae("", out _);
            Assert.AreEqual(0, result);
        }

        [Test]
        public void SetFormulae_Null_ResetsToZeroIndex()
        {
            var v = new WETextDataValueFloat();
            v.Formulae = "test.formula.float";
            v.SetFormulae(null, out _);
            Assert.AreEqual("", v.Formulae);
        }

        [Test]
        public void SetFormulae_Null_ReturnsZero()
        {
            var v = new WETextDataValueFloat();
            var result = v.SetFormulae(null, out _);
            Assert.AreEqual(0, result);
        }

        [Test]
        public void SetFormulae_Whitespace_ResetsToZeroIndex()
        {
            var v = new WETextDataValueFloat();
            v.SetFormulae("   ", out _);
            Assert.AreEqual("", v.Formulae);
        }

        // ── defaultValue round-trip ───────────────────────────────────────

        [Test]
        public void DefaultValue_SetAndGet()
        {
            var v = new WETextDataValueFloat { defaultValue = 3.14f };
            Assert.AreEqual(3.14f, v.defaultValue, 0.0001f);
        }

        // ── UpdateEffectiveValue(IECSReader) overload ─────────────────────────

        [Test]
        public void UpdateEffectiveValue_IECSReader_NoFormulae_SetsEffectiveToDefault()
        {
            var v = new WETextDataValueFloat { defaultValue = 5.5f };
            v.UpdateEffectiveValue(new NullECSReader(), Entity.Null, null);
            Assert.AreEqual(5.5f, v.EffectiveValue, 0.0001f);
        }

        [Test]
        public void UpdateEffectiveValue_IECSReader_NoFormulae_SetsInitializedTrue()
        {
            var v = new WETextDataValueFloat();
            v.UpdateEffectiveValue(new NullECSReader(), Entity.Null, null);
            Assert.IsTrue(v.InitializedEffectiveText);
        }

        [Test]
        public void UpdateEffectiveValue_IECSReader_NoFormulae_SecondCallReturnsFalse()
        {
            var v = new WETextDataValueFloat { defaultValue = 1f };
            v.UpdateEffectiveValue(new NullECSReader(), Entity.Null, null);
            var changed = v.UpdateEffectiveValue(new NullECSReader(), Entity.Null, null);
            Assert.IsFalse(changed);
        }

        // ── UpdateEffectiveValue with injected formula (FE-05) ────────────────

        private static void SetLoadingDone(ref WETextDataValueFloat v)
        {
            object boxed = v;
            typeof(WETextDataValueFloat).GetField("loadingFnDone", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(boxed, true);
            v = (WETextDataValueFloat)boxed;
        }

        private static void InjectForFloat(string key, float val, out IDictionary cache)
        {
            var field = typeof(WEFormulaeHelper).GetField("cachedFnsFloat", BindingFlags.Static | BindingFlags.NonPublic);
            cache = (IDictionary)field.GetValue(null);
            var baseType = typeof(WEFormulaeHelper).GetNestedType("BaseCache`1", BindingFlags.NonPublic).MakeGenericType(typeof(float));
            var inst = System.Activator.CreateInstance(baseType);
            baseType.GetMethod("SetData", BindingFlags.Instance | BindingFlags.Public).Invoke(inst, new object[] { (byte)0, (FormulaeFn<float>)((em, e, v2) => val), null });
            cache[key] = inst;
        }

        [Test]
        public void UpdateEffectiveValue_IECSReader_ValidFormula_ReturnsFormulaResult()
        {
            string key = "fe05_float_valid";
            InjectForFloat(key, 99.5f, out var cache);
            try
            {
                var v = new WETextDataValueFloat { defaultValue = 1f };
                v.Formulae = key;
                SetLoadingDone(ref v);
                var reader = Substitute.For<IECSReader>();
                v.UpdateEffectiveValue(reader, Entity.Null, null);
                Assert.AreEqual(99.5f, v.EffectiveValue, 0.001f);
            }
            finally { cache.Remove(key); }
        }

        [Test]
        public void UpdateEffectiveValue_IECSReader_UnknownFormula_ReturnsNullFnFallback()
        {
            var v = new WETextDataValueFloat { defaultValue = 5f };
            v.Formulae = "fe05_float_nosuch";
            SetLoadingDone(ref v);
            var reader = Substitute.For<IECSReader>();
            v.UpdateEffectiveValue(reader, Entity.Null, null);
            Assert.IsTrue(float.IsNaN(v.EffectiveValue), "Unknown formula should yield NaN (nullFnFallback)");
        }

        [Test]
        public void UpdateEffectiveValue_IECSReader_ValidFormula_SetsInitializedTrue()
        {
            string key = "fe05_float_init";
            InjectForFloat(key, 2.0f, out var cache);
            try
            {
                var v = new WETextDataValueFloat();
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
