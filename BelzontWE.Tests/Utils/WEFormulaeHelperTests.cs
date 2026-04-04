using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static BelzontWE.WEFormulaeHelper;

namespace BelzontWE.Tests.Utils
{
    // Tests for WEFormulaeHelper.cs: method discovery, cache management, delegate retrieval,
    // GetRegisteredFormulaeCount, type-separated storage, idempotent injection (FE-04).
    //
    // Uses reflection to inject/remove cached functions (same pattern as dispatch + integration tests)
    // because SetFormulae calls BasicIMod.DebugMode which loads Game.dll — unavailable in test runner.

    [TestFixture]
    public class WEFormulaeHelperTests
    {
        private static readonly BindingFlags AllStatic = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

        private readonly List<(string key, string fieldName)> _injectedKeys = new List<(string, string)>();

        private static IDictionary GetCacheDictionary(string fieldName)
        {
            var field = typeof(WEFormulaeHelper).GetField(fieldName, AllStatic);
            Assert.IsNotNull(field, $"Could not find field '{fieldName}' via reflection");
            return (IDictionary)field.GetValue(null);
        }

        private void InjectCachedFunction<T>(string formulaKey, FormulaeFn<T> fn, byte resultCode = 0)
        {
            string fieldName;
            if (typeof(T) == typeof(string)) fieldName = "cachedFnsString";
            else if (typeof(T) == typeof(float)) fieldName = "cachedFnsFloat";
            else if (typeof(T) == typeof(int)) fieldName = "cachedFnsInt";
            else if (typeof(T) == typeof(float3)) fieldName = "cachedFnsFloat3";
            else if (typeof(T) == typeof(Color)) fieldName = "cachedFnsColor";
            else throw new NotSupportedException($"No cache dictionary for type {typeof(T).Name}");

            var dic = GetCacheDictionary(fieldName);
            var baseCacheType = typeof(WEFormulaeHelper).GetNestedType("BaseCache`1", BindingFlags.NonPublic);
            var concreteType = baseCacheType.MakeGenericType(typeof(T));
            var cache = Activator.CreateInstance(concreteType);
            var setDataMethod = concreteType.GetMethod("SetData", BindingFlags.Instance | BindingFlags.Public);
            setDataMethod.Invoke(cache, new object[] { resultCode, fn, null });
            dic[formulaKey] = cache;
            _injectedKeys.Add((formulaKey, fieldName));
        }

        [TearDown]
        public void Cleanup()
        {
            foreach (var (key, fieldName) in _injectedKeys)
            {
                var dic = GetCacheDictionary(fieldName);
                if (dic.Contains(key)) dic.Remove(key);
            }
            _injectedKeys.Clear();
        }

        // ── GetRegisteredFormulaeCount ─────────────────────────────────────────

        [Test]
        public void GetRegisteredFormulaeCount_AfterInjection_ReturnsIncreasedCount()
        {
            int before = WEFormulaeHelper.GetRegisteredFormulaeCount();

            InjectCachedFunction<string>("helper_count_01", (em, e, vars) => "x");

            int after = WEFormulaeHelper.GetRegisteredFormulaeCount();
            Assert.AreEqual(before + 1, after, "Count should increase by 1 after injection");
        }

        [Test]
        public void GetRegisteredFormulaeCount_MultipleTypes_CountsAll()
        {
            int before = WEFormulaeHelper.GetRegisteredFormulaeCount();

            InjectCachedFunction<string>("helper_count_mt_01", (em, e, vars) => "s");
            InjectCachedFunction<int>("helper_count_mt_02", (em, e, vars) => 1);
            InjectCachedFunction<float>("helper_count_mt_03", (em, e, vars) => 1.0f);

            int after = WEFormulaeHelper.GetRegisteredFormulaeCount();
            Assert.AreEqual(before + 3, after, "Count should increase by 3 for 3 types");
        }

        // ── Calling Cached Delegate Returns Correct Value ──────────────────────

        [Test]
        public void GetCachedFn_String_ReturnsInjectedDelegate()
        {
            FormulaeFn<string> fn = (em, e, vars) => "cached_string";
            InjectCachedFunction("helper_delegate_str_01", fn);

            var retrieved = WEFormulaeHelper.GetCachedFn<string>("helper_delegate_str_01");
            Assert.IsNotNull(retrieved, "Retrieved delegate should not be null");

            var result = retrieved(default, default, null);
            Assert.AreEqual("cached_string", result);
        }

        [Test]
        public void GetCachedFn_Int_ReturnsInjectedDelegate()
        {
            FormulaeFn<int> fn = (em, e, vars) => 99;
            InjectCachedFunction("helper_delegate_int_01", fn);

            var retrieved = WEFormulaeHelper.GetCachedFn<int>("helper_delegate_int_01");
            Assert.IsNotNull(retrieved);

            var result = retrieved(default, default, null);
            Assert.AreEqual(99, result);
        }

        [Test]
        public void GetCachedFn_Float_ReturnsInjectedDelegate()
        {
            FormulaeFn<float> fn = (em, e, vars) => 2.718f;
            InjectCachedFunction("helper_delegate_float_01", fn);

            var retrieved = WEFormulaeHelper.GetCachedFn<float>("helper_delegate_float_01");
            Assert.IsNotNull(retrieved);

            var result = retrieved(default, default, null);
            Assert.AreEqual(2.718f, result, 0.001f);
        }

        // ── Different Return Types Stored Separately ───────────────────────────

        [Test]
        public void GetCachedFn_SameKey_DifferentTypes_StoredSeparately()
        {
            string key = "helper_type_sep_01";
            InjectCachedFunction<string>(key, (em, e, vars) => "string_value");
            InjectCachedFunction<int>(key, (em, e, vars) => 42);

            var strFn = WEFormulaeHelper.GetCachedFn<string>(key);
            var intFn = WEFormulaeHelper.GetCachedFn<int>(key);

            Assert.IsNotNull(strFn, "String cache should have the key");
            Assert.IsNotNull(intFn, "Int cache should have the key");

            Assert.AreEqual("string_value", strFn(default, default, null));
            Assert.AreEqual(42, intFn(default, default, null));
        }

        // ── Idempotent Injection ───────────────────────────────────────────────

        [Test]
        public void InjectSameKey_Twice_IsIdempotent_NoCountIncrease()
        {
            string key = "helper_idempotent_01";
            InjectCachedFunction<string>(key, (em, e, vars) => "first");
            int countAfterFirst = WEFormulaeHelper.GetRegisteredFormulaeCount();

            InjectCachedFunction<string>(key, (em, e, vars) => "second");
            int countAfterSecond = WEFormulaeHelper.GetRegisteredFormulaeCount();

            Assert.AreEqual(countAfterFirst, countAfterSecond,
                "Re-injecting same key should not increase count (dictionary overwrites)");

            // Verify latest value is used
            var fn = WEFormulaeHelper.GetCachedFn<string>(key);
            Assert.AreEqual("second", fn(default, default, null),
                "Second injection should overwrite the first");
        }

        // ── Unknown Key Returns Null ───────────────────────────────────────────

        [Test]
        public void GetCachedFn_UnknownKey_ReturnsNull()
        {
            var fn = WEFormulaeHelper.GetCachedFn<string>("helper_nonexistent_key_xyz");
            Assert.IsNull(fn, "Unknown key should return null");
        }

        // ── FilterAvailableMethodsForFormulae ──────────────────────────────────

        [Test]
        public void FilterAvailableMethodsForFormulae_DiscoversByClassName()
        {
            WEFormulaeHelper.ResetMethodCache();
            var methods = WEFormulaeHelper.FilterAvailableMethodsForFormulae(
                typeof(Entity), "TestFormulaeClass", null);

            // TestFormulaeClass has 4 methods with Entity first param: GetGreeting, GetGreetingWithVars, GetFixedInt, GetFixedFloat
            Assert.GreaterOrEqual(methods.Count(), 4,
                "Should discover at least 4 Entity-param methods in TestFormulaeClass");
        }

        [Test]
        public void FilterAvailableMethodsForFormulae_FiltersByMethodName()
        {
            WEFormulaeHelper.ResetMethodCache();
            var methods = WEFormulaeHelper.FilterAvailableMethodsForFormulae(
                typeof(Entity), "TestFormulaeClass", "GetFixedInt");

            Assert.AreEqual(1, methods.Count(), "Should find exactly one method named GetFixedInt");
            Assert.AreEqual(typeof(int), methods.First().ReturnType);
        }

        [Test]
        public void FilterAvailableMethodsForFormulae_DiscoveredNamesMatchKnownNames()
        {
            WEFormulaeHelper.ResetMethodCache();
            var methods = WEFormulaeHelper.FilterAvailableMethodsForFormulae(
                typeof(Entity), "TestFormulaeClass", null);

            var names = methods.Select(m => m.Name).ToList();
            CollectionAssert.Contains(names, "GetGreeting", "Should find GetGreeting");
            CollectionAssert.Contains(names, "GetFixedInt", "Should find GetFixedInt");
            CollectionAssert.Contains(names, "GetFixedFloat", "Should find GetFixedFloat");
        }

        [Test]
        public void FilterAvailableMethodsForFormulae_StringParamType_ReturnsStringChainingMethods()
        {
            // Verifies "GetFormulaForType(typeof(string))" concept: formulae whose first param is string
            // (used as chaining formulae that transform string→string)
            WEFormulaeHelper.ResetMethodCache();
            var methods = WEFormulaeHelper.FilterAvailableMethodsForFormulae(
                typeof(string), "TestFormulaeClass", null);

            var names = methods.Select(m => m.Name).ToList();
            CollectionAssert.Contains(names, "ToUpperCase", "Should find ToUpperCase (string→string)");
            CollectionAssert.Contains(names, "AppendExclamation", "Should find AppendExclamation (string→string)");
        }

        // ── GetPathParts ───────────────────────────────────────────────────────

        [Test]
        public void GetPathParts_SingleSegment_ReturnsSingleElement()
        {
            var parts = WEFormulaeHelper.GetPathParts("&TestFormulaeClass;GetGreeting");
            Assert.AreEqual(1, parts.Length);
            Assert.AreEqual("&TestFormulaeClass;GetGreeting", parts[0]);
        }

        [Test]
        public void GetPathParts_ChainedFormula_SplitsOnSlash()
        {
            var parts = WEFormulaeHelper.GetPathParts(
                "&TestFormulaeClass;GetGreeting/&TestFormulaeClass;ToUpperCase");
            Assert.AreEqual(2, parts.Length);
            Assert.AreEqual("&TestFormulaeClass;GetGreeting", parts[0]);
            Assert.AreEqual("&TestFormulaeClass;ToUpperCase", parts[1]);
        }

        [Test]
        public void GetPathParts_NullInput_ReturnsNull()
        {
            var parts = WEFormulaeHelper.GetPathParts(null);
            Assert.IsNull(parts);
        }

        // ── GetCachedFloat3Fn / GetCachedColorFn / GetCachedEntityArrayFn ─────

        [Test]
        public void GetCachedFloat3Fn_UnknownKey_ReturnsNull()
        {
            var fn = WEFormulaeHelper.GetCachedFloat3Fn("helper_float3_nonexistent_xyz");
            Assert.IsNull(fn, "Unknown key should return null");
        }

        [Test]
        public void GetCachedColorFn_UnknownKey_ReturnsNull()
        {
            var fn = WEFormulaeHelper.GetCachedColorFn("helper_color_nonexistent_xyz");
            Assert.IsNull(fn, "Unknown key should return null");
        }

        [Test]
        public void GetCachedEntityArrayFn_UnknownKey_ReturnsNull()
        {
            var fn = WEFormulaeHelper.GetCachedEntityArrayFn("helper_entityarr_nonexistent_xyz");
            Assert.IsNull(fn, "Unknown key should return null");
        }

        [Test]
        public void GetCachedFloat3Fn_AfterInjection_ReturnsDelegate()
        {
            var expected = new float3(7, 8, 9);
            InjectCachedFunction<float3>("helper_float3_delegate_01", (em, e, vars) => expected);

            var fn = WEFormulaeHelper.GetCachedFloat3Fn("helper_float3_delegate_01");
            Assert.IsNotNull(fn, "Injected float3 formula fn should be retrievable");
            Assert.AreEqual(expected, fn(default, default, null));
        }

        [Test]
        public void GetCachedColorFn_AfterInjection_ReturnsDelegate()
        {
            InjectCachedFunction<Color>("helper_color_delegate_01", (em, e, vars) => Color.yellow);

            var fn = WEFormulaeHelper.GetCachedColorFn("helper_color_delegate_01");
            Assert.IsNotNull(fn, "Injected color formula fn should be retrievable");
            Assert.AreEqual(Color.yellow, fn(default, default, null));
        }

        [Test]
        public void GetRegisteredFormulaeCount_Float3Injection_IncreasesByOne()
        {
            int before = WEFormulaeHelper.GetRegisteredFormulaeCount();
            InjectCachedFunction<float3>("helper_float3_count_01", (em, e, vars) => new float3(1, 2, 3));
            int after = WEFormulaeHelper.GetRegisteredFormulaeCount();
            Assert.AreEqual(before + 1, after, "Count should increase by 1 after float3 injection");
        }

        [Test]
        public void GetRegisteredFormulaeCount_ColorInjection_IncreasesByOne()
        {
            int before = WEFormulaeHelper.GetRegisteredFormulaeCount();
            InjectCachedFunction<Color>("helper_color_count_01", (em, e, vars) => Color.red);
            int after = WEFormulaeHelper.GetRegisteredFormulaeCount();
            Assert.AreEqual(before + 1, after, "Count should increase by 1 after Color injection");
        }

        [Test]
        public void IsByRefLikeSafe_Int_ReturnsFalse()
        {
            // int is a value type but NOT a ref-like struct
            bool result = WEFormulaeHelper.IsByRefLikeSafe(typeof(int));
            Assert.IsFalse(result, "int should not be by-ref-like");
        }

        [Test]
        public void IsByRefLikeSafe_String_ReturnsFalse()
        {
            bool result = WEFormulaeHelper.IsByRefLikeSafe(typeof(string));
            Assert.IsFalse(result, "string should not be by-ref-like");
        }

        [Test]
        public void GetPathParts_EmptyString_ReturnsSingleEmptyElement()
        {
            var parts = WEFormulaeHelper.GetPathParts("");
            Assert.IsNotNull(parts);
            Assert.AreEqual(1, parts.Length, "Empty string splits to single empty element");
        }

        [Test]
        public void GetCachedFn_Float_InjectedThenRemoved_ReturnsNull()
        {
            string key = "helper_remove_test_01";
            InjectCachedFunction<float>(key, (em, e, vars) => 1.0f);
            var fnBefore = WEFormulaeHelper.GetCachedFloatFn(key);
            Assert.IsNotNull(fnBefore, "Should be retrievable after injection");

            var dic = GetCacheDictionary("cachedFnsFloat");
            dic.Remove(key);
            _injectedKeys.RemoveAll(t => t.key == key); // Already removed manually

            var fnAfter = WEFormulaeHelper.GetCachedFloatFn(key);
            Assert.IsNull(fnAfter, "Should return null after removal");
        }
    }
}
