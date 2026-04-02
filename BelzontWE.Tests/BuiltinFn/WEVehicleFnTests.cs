using NUnit.Framework;
using System;
using Unity.Entities;
using BelzontWE.Builtin;

namespace BelzontWE.Tests.BuiltinFn
{
    [TestFixture]
    public class WEVehicleFnTests
    {
        // ── Constants ─────────────────────────────────────────────────────────

        [Test]
        public void LETTERS_HasLength26()
        {
            Assert.AreEqual(26, WEVehicleFn.LETTERS.Length);
        }

        [Test]
        public void LETTERS_ContainsOnlyUppercase()
        {
            foreach (var c in WEVehicleFn.LETTERS)
            {
                Assert.IsTrue(char.IsUpper(c), $"Expected uppercase, got '{c}'");
            }
        }

        [Test]
        public void NUMBERS_HasLength10()
        {
            Assert.AreEqual(10, WEVehicleFn.NUMBERS.Length);
        }

        [Test]
        public void NUMBERS_ContainsDigitsZeroToNine()
        {
            Assert.AreEqual("0123456789", WEVehicleFn.NUMBERS);
        }

        [Test]
        public void DIGITS_ORDER_HasLength7()
        {
            Assert.AreEqual(7, WEVehicleFn.DIGITS_ORDER.Length);
        }

        [Test]
        public void DIGITS_ORDER_FirstTwoAreNumbers()
        {
            Assert.AreEqual(WEVehicleFn.NUMBERS, WEVehicleFn.DIGITS_ORDER[0]);
            Assert.AreEqual(WEVehicleFn.NUMBERS, WEVehicleFn.DIGITS_ORDER[1]);
        }

        [Test]
        public void DIGITS_ORDER_ThirdIsLetters()
        {
            Assert.AreEqual(WEVehicleFn.LETTERS, WEVehicleFn.DIGITS_ORDER[2]);
        }

        // ── SerialNumber binding logic ────────────────────────────────────────

        [Test]
        public void GetSerialNumber_DefaultBinding_EntityNullIndex_ReturnsZeroPadded()
        {
            var result = WEVehicleFn.GetSerialNumber_binding(Entity.Null);
            Assert.AreEqual("00000", result);
        }

        [Test]
        public void GetSerialNumber_DefaultBinding_Index42_Returns00042()
        {
            var entity = new Entity { Index = 42, Version = 1 };
            var result = WEVehicleFn.GetSerialNumber_binding(entity);
            Assert.AreEqual("00042", result);
        }

        [Test]
        public void GetSerialNumber_DefaultBinding_Index100005_WrapsAround()
        {
            var entity = new Entity { Index = 100005, Version = 1 };
            var result = WEVehicleFn.GetSerialNumber_binding(entity);
            // 100005 % 100000 = 5 → "00005"
            Assert.AreEqual("00005", result);
        }

        [Test]
        public void GetSerialNumber_DefaultBinding_ResultIsAlways5Chars()
        {
            foreach (var index in new[] { 0, 1, 9999, 99999, 100000 })
            {
                var entity = new Entity { Index = index, Version = 1 };
                var result = WEVehicleFn.GetSerialNumber_binding(entity);
                Assert.AreEqual(5, result.Length, $"Expected 5-char result for index {index}, got '{result}'");
            }
        }

        // ── Binding null fallback ─────────────────────────────────────────────

        [Test]
        public void GetSerialNumber_WhenBindingNull_ReturnsPlaceholder()
        {
            var original = WEVehicleFn.GetSerialNumber_binding;
            try
            {
                WEVehicleFn.GetSerialNumber_binding = null;
                var result = WEVehicleFn.GetSerialNumber(Entity.Null);
                Assert.AreEqual("<???>", result);
            }
            finally
            {
                WEVehicleFn.GetSerialNumber_binding = original;
            }
        }

        [Test]
        public void GetTargetDestinationStatic_WhenBindingNull_ReturnsPlaceholder()
        {
            var original = WEVehicleFn.GetTargetDestinationStatic_binding;
            try
            {
                WEVehicleFn.GetTargetDestinationStatic_binding = null;
                var result = WEVehicleFn.GetTargetDestinationStatic(Entity.Null);
                Assert.AreEqual("<???>", result);
            }
            finally
            {
                WEVehicleFn.GetTargetDestinationStatic_binding = original;
            }
        }

        [Test]
        public void GetVehiclePlate_WhenBindingNull_ReturnsPlaceholder()
        {
            var original = WEVehicleFn.GetVehiclePlate_binding;
            try
            {
                WEVehicleFn.GetVehiclePlate_binding = null;
                var result = WEVehicleFn.GetVehiclePlate(Entity.Null);
                Assert.AreEqual("<???>", result);
            }
            finally
            {
                WEVehicleFn.GetVehiclePlate_binding = original;
            }
        }

        // ── Custom binding injection ──────────────────────────────────────────

        [Test]
        public void GetSerialNumber_CustomBinding_ReturnsCustomValue()
        {
            var original = WEVehicleFn.GetSerialNumber_binding;
            try
            {
                WEVehicleFn.GetSerialNumber_binding = (_) => "CUSTOM";
                var result = WEVehicleFn.GetSerialNumber(Entity.Null);
                Assert.AreEqual("CUSTOM", result);
            }
            finally
            {
                WEVehicleFn.GetSerialNumber_binding = original;
            }
        }

        [Test]
        public void GetVehiclePlateLine1_WithCustomPlateBinding_ReturnFirstHalf()
        {
            var originalPlate = WEVehicleFn.GetVehiclePlate_binding;
            try
            {
                WEVehicleFn.GetVehiclePlate_binding = (_) => "ABCD1234";
                var result = WEVehicleFn.GetVehiclePlateLine1_binding(Entity.Null);
                Assert.AreEqual("ABCD", result);
            }
            finally
            {
                WEVehicleFn.GetVehiclePlate_binding = originalPlate;
            }
        }

        [Test]
        public void GetVehiclePlateLine2_WithCustomPlateBinding_ReturnsSecondHalf()
        {
            var originalPlate = WEVehicleFn.GetVehiclePlate_binding;
            try
            {
                WEVehicleFn.GetVehiclePlate_binding = (_) => "ABCD1234";
                var result = WEVehicleFn.GetVehiclePlateLine2_binding(Entity.Null);
                Assert.AreEqual("1234", result);
            }
            finally
            {
                WEVehicleFn.GetVehiclePlate_binding = originalPlate;
            }
        }
    }
}
