using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Unity.Entities;
using BelzontWE.Builtin;

namespace BelzontWE.Tests.BuiltinFn
{
    // WECalendarFn has binding seams since SR-05: GetNormalizedTime_binding,
    // GetCurrentDateTime_binding, and GetDateTimeFormatter_binding allow time
    // and formatter to be injected in tests without a running game TimeSystem.

    [TestFixture]
    public class WECalendarFnTests
    {
        private Func<float> _originalNormTime;
        private Func<DateTime> _originalDateTime;
        private Func<System.Globalization.DateTimeFormatInfo> _originalFormatter;

        [SetUp]
        public void SetUp()
        {
            _originalNormTime = WECalendarFn.GetNormalizedTime_binding;
            _originalDateTime = WECalendarFn.GetCurrentDateTime_binding;
            _originalFormatter = WECalendarFn.GetDateTimeFormatter_binding;
        }

        [TearDown]
        public void TearDown()
        {
            WECalendarFn.GetNormalizedTime_binding = _originalNormTime;
            WECalendarFn.GetCurrentDateTime_binding = _originalDateTime;
            WECalendarFn.GetDateTimeFormatter_binding = _originalFormatter;
        }
        // ── Reflection: class and attribute ────────────────────────────────────

        [Test]
        public void WECalendarFn_HasWEBuiltinFunctionAttribute()
        {
            var attr = typeof(WECalendarFn).GetCustomAttribute<WEBuiltinFunctionAttribute>();
            Assert.IsNotNull(attr);
        }

        [Test]
        public void WECalendarFn_WEBuiltinFunctionAttribute_CategoryIsCalendar()
        {
            var attr = typeof(WECalendarFn).GetCustomAttribute<WEBuiltinFunctionAttribute>();
            Assert.AreEqual("Calendar", attr.Category);
        }

        [Test]
        public void WECalendarFn_IsStaticClass()
        {
            var t = typeof(WECalendarFn);
            Assert.IsTrue(t.IsAbstract && t.IsSealed, "Expected a static (abstract+sealed) class");
        }

        // ── Reflection: method signatures ──────────────────────────────────────

        [Test]
        public void GetTimeStringWeLocale_MethodExists()
        {
            var m = typeof(WECalendarFn).GetMethod("GetTimeStringWeLocale",
                BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(m);
        }

        [Test]
        public void GetFormattedDateWeLocale_MethodExists()
        {
            var m = typeof(WECalendarFn).GetMethod("GetFormattedDateWeLocale",
                BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(m);
        }

        [Test]
        public void GetTimeStringWeLocale_HasWEFormulaAttribute()
        {
            var m = typeof(WECalendarFn).GetMethod("GetTimeStringWeLocale",
                BindingFlags.Public | BindingFlags.Static);
            var attr = m.GetCustomAttribute<WEFormulaAttribute>();
            Assert.IsNotNull(attr);
        }

        [Test]
        public void GetFormattedDateWeLocale_HasWEFormulaAttribute()
        {
            var m = typeof(WECalendarFn).GetMethod("GetFormattedDateWeLocale",
                BindingFlags.Public | BindingFlags.Static);
            var attr = m.GetCustomAttribute<WEFormulaAttribute>();
            Assert.IsNotNull(attr);
        }

        [Test]
        public void GetTimeStringWeLocale_AcceptsTwoParameters()
        {
            var m = typeof(WECalendarFn).GetMethod("GetTimeStringWeLocale",
                BindingFlags.Public | BindingFlags.Static);
            Assert.AreEqual(2, m.GetParameters().Length);
        }

        [Test]
        public void GetFormattedDateWeLocale_AcceptsTwoParameters()
        {
            var m = typeof(WECalendarFn).GetMethod("GetFormattedDateWeLocale",
                BindingFlags.Public | BindingFlags.Static);
            Assert.AreEqual(2, m.GetParameters().Length);
        }

        // ── Behavioural stubs (require game runtime / binding seam) ──────────

        [Test, Ignore("Requires TimeSystem from Game.dll — add GetTime_binding seam (SR-05) for testability")]
        public void GetTimeStringWeLocale_24HFormat_NoAmPmInPattern_Returns24HTime()
        {
            var result = WECalendarFn.GetTimeStringWeLocale(Entity.Null, new Dictionary<string, string>());
            StringAssert.IsMatch(@"^\d{2}:\d{2}$", result);
        }

        [Test, Ignore("Requires TimeSystem from Game.dll — add GetTime_binding seam (SR-05) for testability")]
        public void GetTimeStringWeLocale_12HFormat_WithCustomAmPm_UsesCustomLabel()
        {
            var vars = new Dictionary<string, string> { ["am"] = "AM", ["pm"] = "PM" };
            var result = WECalendarFn.GetTimeStringWeLocale(Entity.Null, vars);
            Assert.IsNotNull(result);
        }

        [Test, Ignore("Requires TimeSystem from Game.dll — add GetTime_binding seam (SR-05) for testability")]
        public void GetFormattedDateWeLocale_CustomDateFormat_UsesProvidedFormat()
        {
            var vars = new Dictionary<string, string> { ["dateFormat"] = "yyyy-MM" };
            var result = WECalendarFn.GetFormattedDateWeLocale(Entity.Null, vars);
            Assert.IsNotNull(result);
        }

        // ── Time seam tests (SR-05) ───────────────────────────────────────────

        [Test]
        public void GetTimeStringWeLocale_24HFormat_Noon_Returns1200()
        {
            WECalendarFn.GetNormalizedTime_binding = () => 0.5f; // 0.5 * 24 = 12.0
            WECalendarFn.GetDateTimeFormatter_binding = () => CultureInfo.InvariantCulture.DateTimeFormat;
            var result = WECalendarFn.GetTimeStringWeLocale(Entity.Null, new Dictionary<string, string>());
            Assert.AreEqual("12:00", result);
        }

        [Test]
        public void GetTimeStringWeLocale_24HFormat_Midnight_Returns0000()
        {
            WECalendarFn.GetNormalizedTime_binding = () => 0f; // 0 * 24 = 0.0
            WECalendarFn.GetDateTimeFormatter_binding = () => CultureInfo.InvariantCulture.DateTimeFormat;
            var result = WECalendarFn.GetTimeStringWeLocale(Entity.Null, new Dictionary<string, string>());
            Assert.AreEqual("00:00", result);
        }

        [Test]
        public void GetTimeStringWeLocale_24HFormat_HalfPastTwo_Returns1430()
        {
            // normalizedTime 0.6042 * 24 = 14.5 = 14:30
            WECalendarFn.GetNormalizedTime_binding = () => 0.604167f;
            WECalendarFn.GetDateTimeFormatter_binding = () => CultureInfo.InvariantCulture.DateTimeFormat;
            var result = WECalendarFn.GetTimeStringWeLocale(Entity.Null, new Dictionary<string, string>());
            Assert.AreEqual("14:30", result);
        }

        [Test]
        public void GetTimeStringWeLocale_12HFormat_Noon_ReturnsPMLabel()
        {
            WECalendarFn.GetNormalizedTime_binding = () => 0.5f; // noon
            var fmt = (DateTimeFormatInfo)CultureInfo.GetCultureInfo("en-US").DateTimeFormat.Clone();
            WECalendarFn.GetDateTimeFormatter_binding = () => fmt;
            var vars = new Dictionary<string, string> { ["pm"] = "pm" };
            var result = WECalendarFn.GetTimeStringWeLocale(Entity.Null, vars);
            StringAssert.EndsWith("pm", result);
        }

        [Test]
        public void GetTimeStringWeLocale_12HFormat_Morning_ReturnsAMLabel()
        {
            WECalendarFn.GetNormalizedTime_binding = () => 0.25f; // 6:00
            var fmt = (DateTimeFormatInfo)CultureInfo.GetCultureInfo("en-US").DateTimeFormat.Clone();
            WECalendarFn.GetDateTimeFormatter_binding = () => fmt;
            var vars = new Dictionary<string, string> { ["am"] = "am" };
            var result = WECalendarFn.GetTimeStringWeLocale(Entity.Null, vars);
            StringAssert.EndsWith("am", result);
        }

        [Test]
        public void GetFormattedDateWeLocale_CustomFormat_ReturnsFormattedDate()
        {
            WECalendarFn.GetCurrentDateTime_binding = () => new DateTime(2025, 1, 1);
            WECalendarFn.GetDateTimeFormatter_binding = () => CultureInfo.InvariantCulture.DateTimeFormat;
            var vars = new Dictionary<string, string> { ["dateFormat"] = "yyyy-MM" };
            var result = WECalendarFn.GetFormattedDateWeLocale(Entity.Null, vars);
            Assert.AreEqual("2025-01", result);
        }

        [Test]
        public void GetFormattedDateWeLocale_NoFormatVar_UsesDefaultMMMSepYyyy()
        {
            WECalendarFn.GetCurrentDateTime_binding = () => new DateTime(2025, 3, 3);
            var fmt = CultureInfo.InvariantCulture.DateTimeFormat;
            WECalendarFn.GetDateTimeFormatter_binding = () => fmt;
            var result = WECalendarFn.GetFormattedDateWeLocale(Entity.Null, new Dictionary<string, string>());
            // Default format: "MMM/yyyy" (InvariantCulture separator is "/")
            // time.AddMonths(3-3) = 2025-03-03, format = "Mar/2025"
            Assert.AreEqual($"Mar{fmt.DateSeparator}2025", result);
        }
    }
}
