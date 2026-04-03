using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Entities;
using BelzontWE.Builtin;

namespace BelzontWE.Tests.BuiltinFn
{
    // WECalendarFn has no binding seams — both methods call TimeSystem directly.
    // TimeSystem is from Game.dll which is unavailable in the test runner.
    // Testable surface: class/method metadata via reflection.
    // Behavioural tests are [Ignore]d as best-effort stubs; full coverage requires
    // extracting a GetTime seam (planned for SR-05 / Sprint 7).

    [TestFixture]
    public class WECalendarFnTests
    {
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
    }
}
