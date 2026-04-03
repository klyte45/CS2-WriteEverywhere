using NUnit.Framework;
using BelzontWE.Tests.Utils;
using Unity.Entities;

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
    }
}
