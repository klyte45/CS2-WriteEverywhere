using BelzontWE.Tests.Utils;
using BelzontWE.Utils;
using NSubstitute;
using NUnit.Framework;
using System.Collections;
using System.Reflection;
using Unity.Entities;
using UnityEngine;
using static BelzontWE.WEFormulaeHelper;

namespace BelzontWE.Tests.Components
{
    [TestFixture]
    public class WETextDataValueColorTests
    {
        private static WEFormulaeEvalCore.EvalConfig<Color> GetColorConfig()
        {
            var field = typeof(WETextDataValueColor)
                .GetField("s_config", BindingFlags.NonPublic | BindingFlags.Static);
            return (WEFormulaeEvalCore.EvalConfig<Color>)field.GetValue(null);
        }

        // ── Initial state ─────────────────────────────────────────────────

        [Test]
        public void InitialState_InitializedEffectiveTextIsFalse()
        {
            var v = new WETextDataValueColor();
            Assert.IsFalse(v.InitializedEffectiveText);
        }

        [Test]
        public void InitialState_FormulaeIsEmpty()
        {
            var v = new WETextDataValueColor();
            Assert.AreEqual("", v.Formulae);
        }

        // ── Formulae round-trip ───────────────────────────────────────────

        [Test]
        public void Formulae_Setter_RoundTrip()
        {
            var v = new WETextDataValueColor();
            v.Formulae = "color.formulae.test";
            Assert.AreEqual("color.formulae.test", v.Formulae);
        }

        [Test]
        public void Formulae_SetTwice_ReturnsLastValue()
        {
            var v = new WETextDataValueColor();
            v.Formulae = "first.color";
            v.Formulae = "second.color";
            Assert.AreEqual("second.color", v.Formulae);
        }

        // ── defaultValue round-trip ───────────────────────────────────────

        [Test]
        public void DefaultValue_SetAndGet()
        {
            var v = new WETextDataValueColor { defaultValue = Color.red };
            Assert.AreEqual(Color.red, v.defaultValue);
        }

        // ── SetFormulae reset behaviour ────────────────────────────────────

        [Test]
        public void SetFormulae_EmptyString_ClearsFormulae()
        {
            var v = new WETextDataValueColor();
            v.Formulae = "color.formulae.test";
            v.SetFormulae("", out _);
            Assert.AreEqual("", v.Formulae);
        }

        [Test]
        public void SetFormulae_Null_ClearsFormulae()
        {
            var v = new WETextDataValueColor();
            v.Formulae = "color.formulae.test";
            v.SetFormulae(null, out _);
            Assert.AreEqual("", v.Formulae);
        }

        [Test]
        public void SetFormulae_EmptyString_ReturnsZero()
        {
            var v = new WETextDataValueColor();
            var result = v.SetFormulae("", out _);
            Assert.AreEqual(0, result);
        }

        // ── s_config fallback values ───────────────────────────────────────

        [Test]
        public void Config_NullFnFallback_IsCyan()
        {
            var config = GetColorConfig();
            Assert.AreEqual(Color.cyan, config.nullFnFallback);
        }

        [Test]
        public void Config_ErrorFallback_IsMagenta()
        {
            var config = GetColorConfig();
            Assert.AreEqual(Color.magenta, config.errorFallback);
        }
        // ── UpdateEffectiveValue(IECSReader) overload ─────────────────────────

        [Test]
        public void UpdateEffectiveValue_IECSReader_NoFormulae_SetsEffectiveToDefault()
        {
            var v = new WETextDataValueColor { defaultValue = Color.red };
            v.UpdateEffectiveValue(new NullECSReader(), Entity.Null, null);
            Assert.AreEqual(Color.red, v.EffectiveValue);
        }

        [Test]
        public void UpdateEffectiveValue_IECSReader_NoFormulae_SetsInitializedTrue()
        {
            var v = new WETextDataValueColor();
            v.UpdateEffectiveValue(new NullECSReader(), Entity.Null, null);
            Assert.IsTrue(v.InitializedEffectiveText);
        }

        [Test]
        public void UpdateEffectiveValue_IECSReader_NoFormulae_SecondCallReturnsFalse()
        {
            var v = new WETextDataValueColor { defaultValue = Color.blue };
            v.UpdateEffectiveValue(new NullECSReader(), Entity.Null, null);
            var changed = v.UpdateEffectiveValue(new NullECSReader(), Entity.Null, null);
            Assert.IsFalse(changed);
        }

        // ── UpdateEffectiveValue with injected formula (FE-05 Color) ──────────

        private static void SetLoadingDone(ref WETextDataValueColor v)
        {
            object boxed = v;
            typeof(WETextDataValueColor).GetField("loadingFnDone", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(boxed, true);
            v = (WETextDataValueColor)boxed;
        }

        private static void InjectForColor(string key, Color val, out IDictionary cache)
        {
            var field = typeof(WEFormulaeHelper).GetField("cachedFnsColor", BindingFlags.Static | BindingFlags.NonPublic);
            cache = (IDictionary)field.GetValue(null);
            var baseType = typeof(WEFormulaeHelper).GetNestedType("BaseCache`1", BindingFlags.NonPublic).MakeGenericType(typeof(Color));
            var inst = System.Activator.CreateInstance(baseType);
            baseType.GetMethod("SetData", BindingFlags.Instance | BindingFlags.Public).Invoke(inst, new object[] { (byte)0, (FormulaeFn<Color>)((em, e, v2) => val), null });
            cache[key] = inst;
        }

        [Test]
        public void UpdateEffectiveValue_IECSReader_ValidFormula_ReturnsFormulaResult()
        {
            string key = "fe05_color_valid";
            InjectForColor(key, Color.green, out var cache);
            try
            {
                var v = new WETextDataValueColor { defaultValue = Color.black };
                v.Formulae = key;
                SetLoadingDone(ref v);
                var reader = Substitute.For<IECSReader>();
                v.UpdateEffectiveValue(reader, Entity.Null, null);
                Assert.AreEqual(Color.green, v.EffectiveValue);
            }
            finally { cache.Remove(key); }
        }

        [Test]
        public void UpdateEffectiveValue_IECSReader_UnknownFormula_ReturnsNullFnFallback()
        {
            var v = new WETextDataValueColor { defaultValue = Color.red };
            v.Formulae = "fe05_color_nosuch";
            SetLoadingDone(ref v);
            var reader = Substitute.For<IECSReader>();
            v.UpdateEffectiveValue(reader, Entity.Null, null);
            Assert.AreEqual(Color.cyan, v.EffectiveValue, "Unknown formula should yield Color.cyan (nullFnFallback)");
        }

        [Test]
        public void UpdateEffectiveValue_IECSReader_ValidFormula_SetsInitializedTrue()
        {
            string key = "fe05_color_init";
            InjectForColor(key, Color.white, out var cache);
            try
            {
                var v = new WETextDataValueColor();
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
