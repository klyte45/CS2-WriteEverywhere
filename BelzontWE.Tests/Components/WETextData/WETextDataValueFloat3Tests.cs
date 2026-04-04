using BelzontWE.Tests.Utils;
using BelzontWE.Utils;
using NSubstitute;
using NUnit.Framework;
using System.Collections;
using System.Reflection;
using Unity.Entities;
using Unity.Mathematics;
using static BelzontWE.WEFormulaeHelper;

namespace BelzontWE.Tests.Components
{
    [TestFixture]
    public class WETextDataValueFloat3Tests
    {
        // ── Initial state ─────────────────────────────────────────────────

        [Test]
        public void InitialState_EffectiveValueIsZero()
        {
            var v = new WETextDataValueFloat3();
            Assert.AreEqual(new float3(0, 0, 0), v.EffectiveValue);
        }

        [Test]
        public void InitialState_InitializedEffectiveTextIsFalse()
        {
            var v = new WETextDataValueFloat3();
            Assert.IsFalse(v.InitializedEffectiveText);
        }

        [Test]
        public void InitialState_DefaultValueIsZero()
        {
            var v = new WETextDataValueFloat3();
            Assert.AreEqual(new float3(0, 0, 0), v.defaultValue);
        }

        // ── Formulae property round-trip via WEStringsBank ────────────────

        [Test]
        public void Formulae_Setter_RoundTripViaStringsBank()
        {
            var v = new WETextDataValueFloat3();
            v.Formulae = "test.formula.float3";
            Assert.AreEqual("test.formula.float3", v.Formulae);
        }

        [Test]
        public void Formulae_InitialState_IsEmptyString()
        {
            var v = new WETextDataValueFloat3();
            Assert.AreEqual("", v.Formulae);
        }

        // ── SetFormulae reset behaviour ───────────────────────────────────

        [Test]
        public void SetFormulae_EmptyString_ResetsToZeroIndex()
        {
            var v = new WETextDataValueFloat3();
            v.Formulae = "test.formula.float3";
            v.SetFormulae("", out _);
            Assert.AreEqual("", v.Formulae);
        }

        [Test]
        public void SetFormulae_EmptyString_ReturnsZero()
        {
            var v = new WETextDataValueFloat3();
            var result = v.SetFormulae("", out _);
            Assert.AreEqual(0, result);
        }

        [Test]
        public void SetFormulae_Null_ResetsToZeroIndex()
        {
            var v = new WETextDataValueFloat3();
            v.Formulae = "test.formula.float3";
            v.SetFormulae(null, out _);
            Assert.AreEqual("", v.Formulae);
        }

        [Test]
        public void SetFormulae_Null_ReturnsZero()
        {
            var v = new WETextDataValueFloat3();
            var result = v.SetFormulae(null, out _);
            Assert.AreEqual(0, result);
        }

        [Test]
        public void SetFormulae_Whitespace_ResetsToZeroIndex()
        {
            var v = new WETextDataValueFloat3();
            v.SetFormulae("   ", out _);
            Assert.AreEqual("", v.Formulae);
        }

        // ── defaultValue round-trip ───────────────────────────────────────

        [Test]
        public void DefaultValue_SetAndGet()
        {
            var v = new WETextDataValueFloat3 { defaultValue = new float3(1, 2, 3) };
            Assert.AreEqual(new float3(1, 2, 3), v.defaultValue);
        }

        // ── UpdateEffectiveValue(IECSReader) overload ─────────────────────────

        [Test]
        public void UpdateEffectiveValue_IECSReader_NoFormulae_SetsEffectiveToDefault()
        {
            var v = new WETextDataValueFloat3 { defaultValue = new float3(1, 2, 3) };
            v.UpdateEffectiveValue(new NullECSReader(), Entity.Null, null);
            Assert.AreEqual(new float3(1, 2, 3), v.EffectiveValue);
        }

        [Test]
        public void UpdateEffectiveValue_IECSReader_NoFormulae_SetsInitializedTrue()
        {
            var v = new WETextDataValueFloat3();
            v.UpdateEffectiveValue(new NullECSReader(), Entity.Null, null);
            Assert.IsTrue(v.InitializedEffectiveText);
        }

        [Test]
        public void UpdateEffectiveValue_IECSReader_NoFormulae_SecondCallReturnsFalse()
        {
            var v = new WETextDataValueFloat3 { defaultValue = new float3(0, 0, 0) };
            v.UpdateEffectiveValue(new NullECSReader(), Entity.Null, null);
            var changed = v.UpdateEffectiveValue(new NullECSReader(), Entity.Null, null);
            Assert.IsFalse(changed);
        }

        // ── UpdateEffectiveValue with injected formula (FE-05 Float3) ─────────

        private static void SetLoadingDone(ref WETextDataValueFloat3 v)
        {
            object boxed = v;
            typeof(WETextDataValueFloat3).GetField("loadingFnDone", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(boxed, true);
            v = (WETextDataValueFloat3)boxed;
        }

        private static void InjectForFloat3(string key, float3 val, out IDictionary cache)
        {
            var field = typeof(WEFormulaeHelper).GetField("cachedFnsFloat3", BindingFlags.Static | BindingFlags.NonPublic);
            cache = (IDictionary)field.GetValue(null);
            var baseType = typeof(WEFormulaeHelper).GetNestedType("BaseCache`1", BindingFlags.NonPublic).MakeGenericType(typeof(float3));
            var inst = System.Activator.CreateInstance(baseType);
            baseType.GetMethod("SetData", BindingFlags.Instance | BindingFlags.Public).Invoke(inst, new object[] { (byte)0, (FormulaeFn<float3>)((em, e, v2) => val), null });
            cache[key] = inst;
        }

        [Test]
        public void UpdateEffectiveValue_IECSReader_ValidFormula_ReturnsFormulaResult()
        {
            string key = "fe05_float3_valid";
            var expected = new float3(1.1f, 2.2f, 3.3f);
            InjectForFloat3(key, expected, out var cache);
            try
            {
                var v = new WETextDataValueFloat3 { defaultValue = new float3(0, 0, 0) };
                v.Formulae = key;
                SetLoadingDone(ref v);
                var reader = Substitute.For<IECSReader>();
                v.UpdateEffectiveValue(reader, Entity.Null, null);
                Assert.AreEqual(expected, v.EffectiveValue);
            }
            finally { cache.Remove(key); }
        }

        [Test]
        public void UpdateEffectiveValue_IECSReader_UnknownFormula_ReturnsNullFnFallback()
        {
            var v = new WETextDataValueFloat3 { defaultValue = new float3(5, 5, 5) };
            v.Formulae = "fe05_float3_nosuch";
            SetLoadingDone(ref v);
            var reader = Substitute.For<IECSReader>();
            v.UpdateEffectiveValue(reader, Entity.Null, null);
            Assert.IsTrue(float.IsNaN(v.EffectiveValue.x), "Unknown formula should yield NaN fallback for x");
            Assert.IsTrue(float.IsNaN(v.EffectiveValue.y), "Unknown formula should yield NaN fallback for y");
            Assert.IsTrue(float.IsNaN(v.EffectiveValue.z), "Unknown formula should yield NaN fallback for z");
        }

        [Test]
        public void UpdateEffectiveValue_IECSReader_ValidFormula_SetsInitializedTrue()
        {
            string key = "fe05_float3_init";
            InjectForFloat3(key, new float3(1, 0, 0), out var cache);
            try
            {
                var v = new WETextDataValueFloat3();
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
