using NUnit.Framework;
using System;
using System.Globalization;
using BelzontWE.Builtin;

namespace BelzontWE.Tests.BuiltinFn
{
    [TestFixture]
    public class WENumberFormattingFnTests
    {
        // The default GetCurrentCulture lambda references WEModData.FormatCulture
        // which transitively loads Game.dll (not available in the test runner).
        // SetUp overrides it to InvariantCulture so DoIntReduction can be called.

        private Func<CultureInfo> _originalCulture;

        [SetUp]
        public void SetUp()
        {
            _originalCulture = WENumberFormattingFn.GetCurrentCulture;
            WENumberFormattingFn.GetCurrentCulture = () => CultureInfo.InvariantCulture;
        }

        [TearDown]
        public void TearDown()
        {
            WENumberFormattingFn.GetCurrentCulture = _originalCulture;
        }

        // ── Locale seam ────────────────────────────────────────────────────────

        [Test]
        public void GetCurrentCulture_DefaultBinding_IsNotNull()
        {
            // The field is always populated (never null at class load time)
            Assert.IsNotNull(_originalCulture);
        }

        [Test]
        public void GetCurrentCulture_CanBeOverridden()
        {
            var de = CultureInfo.GetCultureInfo("de-DE");
            WENumberFormattingFn.GetCurrentCulture = () => de;
            Assert.AreEqual(de, WENumberFormattingFn.GetCurrentCulture());
        }

        [Test]
        public void GetCurrentCulture_SetToInvariant_ReturnsInvariant()
        {
            // SetUp already pointed it to InvariantCulture
            Assert.AreEqual(CultureInfo.InvariantCulture, WENumberFormattingFn.GetCurrentCulture());
        }

        // ── To4DigitsValue(float) via InvariantCulture ────────────────────────

        [Test]
        public void To4DigitsValue_Float_NearTen_ReturnsExpected()
        {
            // 9.5 → "9.500"[..5] = "9.500" + "" = "9.500"
            Assert.AreEqual("9.500", WENumberFormattingFn.To4DigitsValue(9.5f));
        }

        [Test]
        public void To4DigitsValue_Float_NearHundred_ReturnsExpected()
        {
            // 99.5 → "99.500"[..5] = "99.50" + "" = "99.50"
            Assert.AreEqual("99.50", WENumberFormattingFn.To4DigitsValue(99.5f));
        }

        [Test]
        public void To4DigitsValue_Float_NearThousand_ReturnsExpected()
        {
            // 999.9 → "999.900"[..5] = "999.9" + "" = "999.9"
            Assert.AreEqual("999.9", WENumberFormattingFn.To4DigitsValue(999.9f));
        }

        [Test]
        public void To4DigitsValue_Float_TenThousand_AppliesKSuffix()
        {
            // 10000 → reduce to 10.0 order=1 → "10.000"[..5] = "10.00" + "k" = "10.00k"
            Assert.AreEqual("10.00k", WENumberFormattingFn.To4DigitsValue(10000.0f));
        }

        [Test]
        public void To4DigitsValue_Float_TenMillion_AppliesMSuffix()
        {
            // 10000000 → order=2, floatReduced=10.0 → "10.000"[..5] = "10.00" + "M"
            Assert.AreEqual("10.00M", WENumberFormattingFn.To4DigitsValue(10000000.0f));
        }

        // ── To3DigitsValue(float) via InvariantCulture ────────────────────────

        [Test]
        public void To3DigitsValue_Float_NearTen_ReturnsExpected()
        {
            // 9.5 → "9.500"[..4] = "9.50" + "" = "9.50"
            Assert.AreEqual("9.50", WENumberFormattingFn.To3DigitsValue(9.5f));
        }

        [Test]
        public void To3DigitsValue_Float_NearHundred_ReturnsExpected()
        {
            // 99.5 → "99.500"[..4] = "99.5" + "" = "99.5"
            Assert.AreEqual("99.5", WENumberFormattingFn.To3DigitsValue(99.5f));
        }

        [Test]
        public void To3DigitsValue_Float_Thousand_AppliesKSuffix()
        {
            // 1000 ≥ 10^3=1000 → order=1, floatReduced=1.0 → "1.000"[..4] = "1.00" + "k"
            Assert.AreEqual("1.00k", WENumberFormattingFn.To3DigitsValue(1000.0f));
        }

        [Test]
        public void To3DigitsValue_Float_TenMillion_AppliesMSuffix()
        {
            // 10000000 → order=2, floatReduced=10.0 → "10.000"[..4] = "10.0" + "M"
            Assert.AreEqual("10.0M", WENumberFormattingFn.To3DigitsValue(10000000.0f));
        }

        // ── Integer and other numeric overloads ───────────────────────────────

        [Test]
        public void To4DigitsValue_Int_BehavesLikeFloat()
        {
            Assert.AreEqual(WENumberFormattingFn.To4DigitsValue(99.0f),
                            WENumberFormattingFn.To4DigitsValue((int)99));
        }

        [Test]
        public void To4DigitsValue_Long_BehavesLikeFloat()
        {
            Assert.AreEqual(WENumberFormattingFn.To4DigitsValue(99.0f),
                            WENumberFormattingFn.To4DigitsValue((long)99));
        }

        [Test]
        public void To4DigitsValue_Short_BehavesLikeFloat()
        {
            Assert.AreEqual(WENumberFormattingFn.To4DigitsValue(99.0f),
                            WENumberFormattingFn.To4DigitsValue((short)99));
        }

        // ── Locale seam affects decimal separator ─────────────────────────────

        [Test]
        public void To4DigitsValue_WithGermanLocale_UsesCommaSeparator()
        {
            WENumberFormattingFn.GetCurrentCulture = () => CultureInfo.GetCultureInfo("de-DE");
            // de-DE uses comma: 9.5 → "9,500"[..5] = "9,500"
            var result = WENumberFormattingFn.To4DigitsValue(9.5f);
            Assert.AreEqual("9,500", result);
        }

        [Test]
        public void To4DigitsValue_InvariantVsGerman_ProduceDifferentOutput()
        {
            var invResult = WENumberFormattingFn.To4DigitsValue(9.5f);
            WENumberFormattingFn.GetCurrentCulture = () => CultureInfo.GetCultureInfo("de-DE");
            var deResult = WENumberFormattingFn.To4DigitsValue(9.5f);
            Assert.AreNotEqual(invResult, deResult);
        }
    }
}

