using NUnit.Framework;
using System.Reflection;
using UnityEngine;

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
    }
}
