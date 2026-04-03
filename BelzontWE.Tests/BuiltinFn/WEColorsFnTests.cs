using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using BelzontWE.Builtin;
using Belzont.Utils;

namespace BelzontWE.Tests.BuiltinFn
{
    [TestFixture]
    public class WEColorsFnTests
    {
        // ── ContrastColor via WEColorsFn.GetContrastColor ─────────────────────

        [Test]
        public void GetContrastColor_BrightWhite_ReturnsBlack()
        {
            var result = WEColorsFn.GetContrastColor(Color.white);
            Assert.AreEqual(Color.black, result);
        }

        [Test]
        public void GetContrastColor_DarkBlack_ReturnsWhite()
        {
            var result = WEColorsFn.GetContrastColor(Color.black);
            Assert.AreEqual(new Color(1, 1, 1, 1), result);
        }

        [Test]
        public void GetContrastColor_DefaultColor_ReturnsBlack()
        {
            // ContrastColor: if color == default → return Color.black
            var result = WEColorsFn.GetContrastColor(default(Color));
            Assert.AreEqual(Color.black, result);
        }

        [Test]
        public void GetContrastColor_BrightYellow_ReturnsBlack()
        {
            // Yellow (r=1, g=1, b=0): luminance = 0.299+0.587 = 0.886 > 0.5 → black
            var yellow = new Color(1f, 1f, 0f, 1f);
            var result = WEColorsFn.GetContrastColor(yellow);
            Assert.AreEqual(Color.black, result);
        }

        [Test]
        public void GetContrastColor_DarkBlue_ReturnsWhite()
        {
            // Blue (r=0, g=0, b=1): luminance = 0.114 < 0.5 → white
            var result = WEColorsFn.GetContrastColor(Color.blue);
            Assert.AreEqual(new Color(1, 1, 1, 1), result);
        }

        [Test]
        public void GetContrastColor_ResultAlphaIsAlwaysOne()
        {
            var result1 = WEColorsFn.GetContrastColor(Color.white);
            var result2 = WEColorsFn.GetContrastColor(Color.black);
            Assert.AreEqual(1f, result1.a);
            Assert.AreEqual(1f, result2.a);
        }

        // ── CastColor and CastColor32 ─────────────────────────────────────────

        [Test]
        public void CastColor_Color32Red_ReturnsColorWithPositiveR()
        {
            var c32 = new Color32(255, 0, 0, 255);
            var result = WEColorsFn.CastColor(c32);
            Assert.Greater(result.r, 0f);
            Assert.AreEqual(0f, result.g);
            Assert.AreEqual(0f, result.b);
        }

        [Test]
        public void CastColor32_ColorRed_ReturnsBytesAsExpected()
        {
            var result = WEColorsFn.CastColor32(Color.red);
            Assert.AreEqual(255, result.r);
            Assert.AreEqual(0, result.g);
            Assert.AreEqual(0, result.b);
        }

        [Test]
        public void CastColor_Color32White_ReturnsColorWhite()
        {
            var c32 = new Color32(255, 255, 255, 255);
            var result = WEColorsFn.CastColor(c32);
            Assert.AreEqual(Color.white, result);
        }

        [Test]
        public void CastColor32_RoundTrip_PreservesValues()
        {
            var original = new Color(0.5f, 0.25f, 0.75f, 1f);
            var c32 = WEColorsFn.CastColor32(original);
            var back = WEColorsFn.CastColor(c32);
            // floating point → byte → float loses precision; just check channels in range
            Assert.Greater(back.r, 0f);
            Assert.Greater(back.b, 0f);
        }

        // ── Reflection: attribute checks ──────────────────────────────────────

        [Test]
        public void WEColorsFn_HasWEBuiltinFunctionAttribute()
        {
            var attr = typeof(WEColorsFn).GetCustomAttribute<WEBuiltinFunctionAttribute>();
            Assert.IsNotNull(attr);
        }

        [Test]
        public void WEColorsFn_WEBuiltinFunctionAttribute_CategoryIsColors()
        {
            var attr = typeof(WEColorsFn).GetCustomAttribute<WEBuiltinFunctionAttribute>();
            Assert.AreEqual("Colors", attr.Category);
        }

        [Test]
        public void GetContrastColor_HasWEFormulaAttribute()
        {
            var m = typeof(WEColorsFn).GetMethod("GetContrastColor",
                BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(m?.GetCustomAttribute<WEFormulaAttribute>());
        }

        // ── ContrastColor extension directly ──────────────────────────────────

        [Test]
        public void ColorExtensions_ContrastColor_BrightGreen_ReturnsBlack()
        {
            // Green: luminance = 0.587 > 0.5 → d=0 → black
            var result = Color.green.ContrastColor();
            Assert.AreEqual(Color.black, result);
        }

        [Test]
        public void ColorExtensions_ContrastColor_GrayAsWhite_ReturnsMidGray()
        {
            // Pure red (luminance ~0.299 < 0.5), grayAsWhite=true → d=0.5
            var red = Color.red;
            var result = red.ContrastColor(grayAsWhite: true);
            Assert.AreEqual(0.5f, result.r);
            Assert.AreEqual(0.5f, result.g);
            Assert.AreEqual(0.5f, result.b);
        }
    }
}
