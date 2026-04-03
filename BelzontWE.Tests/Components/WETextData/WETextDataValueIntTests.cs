using BelzontWE.Tests.Utils;
using NUnit.Framework;
using Unity.Entities;

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
    }
}
