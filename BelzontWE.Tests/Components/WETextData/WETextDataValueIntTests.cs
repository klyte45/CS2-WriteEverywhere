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
    public class WETextDataValueIntTests
    {
        // ── Initial state ─────────────────────────────────────────────────

        [Test]
        public void InitialState_EffectiveValueIsZero()
        {
            var v = new WETextDataValueInt();
            Assert.AreEqual(0, v.EffectiveValue);
        }

        [Test]
        public void InitialState_InitializedEffectiveTextIsFalse()
        {
            var v = new WETextDataValueInt();
            Assert.IsFalse(v.InitializedEffectiveText);
        }

        [Test]
        public void InitialState_DefaultValueIsZero()
        {
            var v = new WETextDataValueInt();
            Assert.AreEqual(0, v.defaultValue);
        }

        // ── Formulae property round-trip via WEStringsBank ────────────────

        [Test]
        public void Formulae_Setter_RoundTripViaStringsBank()
        {
            var v = new WETextDataValueInt();
            v.Formulae = "test.formula.int";
            Assert.AreEqual("test.formula.int", v.Formulae);
        }

        [Test]
        public void Formulae_InitialState_IsEmptyString()
        {
            var v = new WETextDataValueInt();
            Assert.AreEqual("", v.Formulae);
        }

        // ── SetFormulae reset behaviour ───────────────────────────────────

        [Test]
        public void SetFormulae_EmptyString_ResetsToZeroIndex()
        {
            var v = new WETextDataValueInt();
            v.Formulae = "test.formula.int";
            v.SetFormulae("", out _);
            Assert.AreEqual("", v.Formulae);
        }

        [Test]
        public void SetFormulae_EmptyString_ReturnsZero()
        {
            var v = new WETextDataValueInt();
            var result = v.SetFormulae("", out _);
            Assert.AreEqual(0, result);
        }

        [Test]
        public void SetFormulae_Null_ResetsToZeroIndex()
        {
            var v = new WETextDataValueInt();
            v.Formulae = "test.formula.int";
            v.SetFormulae(null, out _);
            Assert.AreEqual("", v.Formulae);
        }

        [Test]
        public void SetFormulae_Null_ReturnsZero()
        {
            var v = new WETextDataValueInt();
            var result = v.SetFormulae(null, out _);
            Assert.AreEqual(0, result);
        }

        [Test]
        public void SetFormulae_Whitespace_ResetsToZeroIndex()
        {
            var v = new WETextDataValueInt();
            v.SetFormulae("   ", out _);
            Assert.AreEqual("", v.Formulae);
        }

        // ── defaultValue round-trip ───────────────────────────────────────

        [Test]
        public void DefaultValue_SetAndGet()
        {
            var v = new WETextDataValueInt { defaultValue = 42 };
            Assert.AreEqual(42, v.defaultValue);
        }

        // ── UpdateEffectiveValue(IECSReader) overload ─────────────────────────

        [Test]
        public void UpdateEffectiveValue_IECSReader_NoFormulae_SetsEffectiveToDefault()
        {
            var v = new WETextDataValueInt { defaultValue = 99 };
            v.UpdateEffectiveValue(new NullECSReader(), Entity.Null, null);
            Assert.AreEqual(99, v.EffectiveValue);
        }

        [Test]
        public void UpdateEffectiveValue_IECSReader_NoFormulae_SetsInitializedTrue()
        {
            var v = new WETextDataValueInt();
            v.UpdateEffectiveValue(new NullECSReader(), Entity.Null, null);
            Assert.IsTrue(v.InitializedEffectiveText);
        }

        [Test]
        public void UpdateEffectiveValue_IECSReader_NoFormulae_SecondCallReturnsFalse()
        {
            var v = new WETextDataValueInt { defaultValue = 7 };
            v.UpdateEffectiveValue(new NullECSReader(), Entity.Null, null);
            var changed = v.UpdateEffectiveValue(new NullECSReader(), Entity.Null, null);
            Assert.IsFalse(changed);
        }

        // ── UpdateEffectiveValue with injected formula (FE-05) ────────────────

        private static void SetLoadingDone(ref WETextDataValueInt v)
        {
            object boxed = v;
            typeof(WETextDataValueInt).GetField("loadingFnDone", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(boxed, true);
            v = (WETextDataValueInt)boxed;
        }

        private static void InjectForInt(string key, int val, out IDictionary cache)
        {
            var field = typeof(WEFormulaeHelper).GetField("cachedFnsInt", BindingFlags.Static | BindingFlags.NonPublic);
            cache = (IDictionary)field.GetValue(null);
            var baseType = typeof(WEFormulaeHelper).GetNestedType("BaseCache`1", BindingFlags.NonPublic).MakeGenericType(typeof(int));
            var inst = System.Activator.CreateInstance(baseType);
            baseType.GetMethod("SetData", BindingFlags.Instance | BindingFlags.Public).Invoke(inst, new object[] { (byte)0, (FormulaeFn<int>)((em, e, v2) => val), null });
            cache[key] = inst;
        }

        [Test]
        public void UpdateEffectiveValue_IECSReader_ValidFormula_ReturnsFormulaResult()
        {
            string key = "fe05_int_valid";
            InjectForInt(key, 77, out var cache);
            try
            {
                var v = new WETextDataValueInt { defaultValue = 1 };
                v.Formulae = key;
                SetLoadingDone(ref v);
                var reader = Substitute.For<IECSReader>();
                v.UpdateEffectiveValue(reader, Entity.Null, null);
                Assert.AreEqual(77, v.EffectiveValue);
            }
            finally { cache.Remove(key); }
        }

        [Test]
        public void UpdateEffectiveValue_IECSReader_UnknownFormula_ReturnsNullFnFallback()
        {
            var v = new WETextDataValueInt { defaultValue = 5 };
            v.Formulae = "fe05_int_nosuch";
            SetLoadingDone(ref v);
            var reader = Substitute.For<IECSReader>();
            v.UpdateEffectiveValue(reader, Entity.Null, null);
            Assert.AreEqual(int.MinValue, v.EffectiveValue, "Unknown formula should yield int.MinValue (nullFnFallback)");
        }

        [Test]
        public void UpdateEffectiveValue_IECSReader_ValidFormula_SetsInitializedTrue()
        {
            string key = "fe05_int_init";
            InjectForInt(key, 42, out var cache);
            try
            {
                var v = new WETextDataValueInt();
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
