using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Entities;
using static BelzontWE.WEFormulaeHelper;

namespace BelzontWE.Tests.Utils
{
    // Integration tests: formula discovery → cache injection → evaluation round-trip (FE-03).
    //
    // TRUE end-to-end tests (SetFormulae → TryEvaluate) are [Ignore]'d because SetFormulae
    // calls BasicIMod.DebugMode after IL generation, which loads Game.dll — unavailable in the
    // test runner.
    //
    // The passing tests simulate the round-trip by:
    //   1. Calling FilterAvailableMethodsForFormulae to verify method DISCOVERY works
    //   2. Building a FormulaeFn<T> that delegates to the real TestFormulaeClass methods
    //   3. Injecting into the private cache via reflection (same pattern as dispatch tests)
    //   4. Evaluating via TryEvaluate and asserting results
    //
    // This verifies everything except IL compilation: discovery, cache, dispatch, evaluation.

    [TestFixture]
    public class WEFormulaeEvalCoreIntegrationTests
    {
        private static readonly BindingFlags AllStatic = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

        private readonly List<string> _registeredKeys = new List<string>();

        // ── Reflection helpers (same pattern as WEFormulaeEvalCoreDispatchTests) ──

        private static IDictionary GetCacheDictionary(string fieldName)
        {
            var field = typeof(WEFormulaeHelper).GetField(fieldName, AllStatic);
            Assert.IsNotNull(field, $"Could not find field '{fieldName}' via reflection");
            return (IDictionary)field.GetValue(null);
        }

        private static void InjectCachedFunction<T>(string formulaKey, FormulaeFn<T> fn, byte resultCode = 0)
        {
            string fieldName;
            if (typeof(T) == typeof(string)) fieldName = "cachedFnsString";
            else if (typeof(T) == typeof(float)) fieldName = "cachedFnsFloat";
            else if (typeof(T) == typeof(int)) fieldName = "cachedFnsInt";
            else throw new NotSupportedException($"No cache dictionary for type {typeof(T).Name}");

            var dic = GetCacheDictionary(fieldName);
            var baseCacheType = typeof(WEFormulaeHelper).GetNestedType("BaseCache`1", BindingFlags.NonPublic);
            Assert.IsNotNull(baseCacheType, "Could not find nested type BaseCache`1");
            var concreteType = baseCacheType.MakeGenericType(typeof(T));
            var cache = Activator.CreateInstance(concreteType);
            var setDataMethod = concreteType.GetMethod("SetData", BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(setDataMethod, "Could not find SetData method on BaseCache<T>");
            setDataMethod.Invoke(cache, new object[] { resultCode, fn, null });
            dic[formulaKey] = cache;
        }

        private static void RemoveFromCache(string key)
        {
            string[] fieldNames = { "cachedFnsString", "cachedFnsFloat", "cachedFnsInt", "cachedFnsColor", "cachedFnsFloat3" };
            foreach (var fieldName in fieldNames)
            {
                var field = typeof(WEFormulaeHelper).GetField(fieldName, AllStatic);
                if (field != null)
                {
                    var dic = (IDictionary)field.GetValue(null);
                    if (dic.Contains(key)) dic.Remove(key);
                }
            }
        }

        private int SetupFormulaWithFn<T>(string key, FormulaeFn<T> fn)
        {
            _registeredKeys.Add(key);
            InjectCachedFunction(key, fn);
            return WEStringsBank.Instance[key];
        }

        private T Evaluate<T>(int formulaeStrBnk, T defaultValue, Dictionary<string, string> vars = null,
            WEFormulaeEvalCore.EvalConfig<T> config = default)
        {
            byte compilationStatus = 0;
            bool loadingFnDone = true;
            bool runtimeErrorLogged = false;
            bool initializedEffectiveText = false;
            T effectiveValue = defaultValue;

            WEFormulaeEvalCore.TryEvaluate(
                ref formulaeStrBnk, ref compilationStatus, ref loadingFnDone,
                ref runtimeErrorLogged, ref initializedEffectiveText,
                defaultValue, ref effectiveValue, default, default,
                vars, in config);

            return effectiveValue;
        }

        [TearDown]
        public void Cleanup()
        {
            foreach (var key in _registeredKeys)
            {
                RemoveFromCache(key);
            }
            _registeredKeys.Clear();
        }

        // ══════════════════════════════════════════════════════════════════════════
        // SECTION 1: Method Discovery (FilterAvailableMethodsForFormulae)
        // Verifies that TestFormulaeClass methods are discoverable at runtime.
        // ══════════════════════════════════════════════════════════════════════════

        [Test]
        public void Discovery_TestFormulaeClass_GetGreeting_IsDiscoverable()
        {
            WEFormulaeHelper.ResetMethodCache();
            var methods = WEFormulaeHelper.FilterAvailableMethodsForFormulae(typeof(Entity), "TestFormulaeClass", "GetGreeting");
            Assert.AreEqual(1, methods.Count(), "GetGreeting should match exactly one method");
        }

        [Test]
        public void Discovery_TestFormulaeClass_GetGreetingWithVars_IsDiscoverable()
        {
            WEFormulaeHelper.ResetMethodCache();
            var methods = WEFormulaeHelper.FilterAvailableMethodsForFormulae(typeof(Entity), "TestFormulaeClass", "GetGreetingWithVars");
            Assert.AreEqual(1, methods.Count(), "GetGreetingWithVars should match exactly one method");
        }

        [Test]
        public void Discovery_TestFormulaeClass_NonExistentMethod_ReturnsEmpty()
        {
            WEFormulaeHelper.ResetMethodCache();
            var methods = WEFormulaeHelper.FilterAvailableMethodsForFormulae(typeof(Entity), "TestFormulaeClass", "NonExistentMethod");
            Assert.AreEqual(0, methods.Count(), "NonExistentMethod should not be found");
        }

        // ══════════════════════════════════════════════════════════════════════════
        // SECTION 2: Simulated Round-Trip (Discovery → Cache → Evaluate)
        // Builds FormulaeFn<T> from real TestFormulaeClass methods, injects, evaluates.
        // ══════════════════════════════════════════════════════════════════════════

        // ── Const String Formula ───────────────────────────────────────────────

        [Test]
        public void RoundTrip_ConstStringFormula_ReturnsExpectedValue()
        {
            FormulaeFn<string> fn = (em, e, vars) => TestFormulaeClass.GetGreeting(e);
            int idx = SetupFormulaWithFn("integ_const_str_01", fn);

            var result = Evaluate(idx, "fallback");
            Assert.AreEqual("Hello", result);
        }

        // ── Dict-Reading Formula ───────────────────────────────────────────────

        [Test]
        public void RoundTrip_DictReadingFormula_ReadsVariable()
        {
            FormulaeFn<string> fn = (em, e, vars) => TestFormulaeClass.GetGreetingWithVars(e, vars);
            int idx = SetupFormulaWithFn("integ_dict_read_01", fn);

            var vars = new Dictionary<string, string> { ["name"] = "World" };
            var result = Evaluate(idx, "fallback", vars);
            Assert.AreEqual("Hello, World!", result);
        }

        [Test]
        public void RoundTrip_DictReadingFormula_MissingKey_UsesFallbackBranch()
        {
            FormulaeFn<string> fn = (em, e, vars) => TestFormulaeClass.GetGreetingWithVars(e, vars);
            int idx = SetupFormulaWithFn("integ_dict_missing_01", fn);

            var vars = new Dictionary<string, string> { ["other"] = "value" };
            var result = Evaluate(idx, "fallback", vars);
            Assert.AreEqual("Hello, stranger!", result);
        }

        [Test]
        public void RoundTrip_DictReadingFormula_NullDict_UsesFallbackBranch()
        {
            FormulaeFn<string> fn = (em, e, vars) => TestFormulaeClass.GetGreetingWithVars(e, vars);
            int idx = SetupFormulaWithFn("integ_dict_null_01", fn);

            var result = Evaluate(idx, "fallback", null);
            Assert.AreEqual("Hello, stranger!", result);
        }

        // ── Chained Transforms ─────────────────────────────────────────────────

        [Test]
        public void RoundTrip_ChainedTransform_SingleChain_ToUpperCase()
        {
            // Simulates: GetGreeting → ToUpperCase
            FormulaeFn<string> fn = (em, e, vars) =>
                TestFormulaeClass.ToUpperCase(TestFormulaeClass.GetGreeting(e));
            int idx = SetupFormulaWithFn("integ_chain_upper_01", fn);

            var result = Evaluate(idx, "fallback");
            Assert.AreEqual("HELLO", result);
        }

        [Test]
        public void RoundTrip_ChainedTransform_DoubleChain_ToUpperThenExclamation()
        {
            // Simulates: GetGreeting → ToUpperCase → AppendExclamation
            FormulaeFn<string> fn = (em, e, vars) =>
                TestFormulaeClass.AppendExclamation(
                    TestFormulaeClass.ToUpperCase(
                        TestFormulaeClass.GetGreeting(e)));
            int idx = SetupFormulaWithFn("integ_chain_double_01", fn);

            var result = Evaluate(idx, "fallback");
            Assert.AreEqual("HELLO!", result);
        }

        // ── Int Formula Round-Trip ─────────────────────────────────────────────

        [Test]
        public void RoundTrip_IntFormula_ReturnsExpectedValue()
        {
            FormulaeFn<int> fn = (em, e, vars) => TestFormulaeClass.GetFixedInt(e);
            int idx = SetupFormulaWithFn("integ_int_01", fn);

            byte compilationStatus = 0;
            bool loadingFnDone = true;
            bool runtimeErrorLogged = false;
            bool initializedEffectiveText = false;
            int effectiveValue = 0;
            var config = new WEFormulaeEvalCore.EvalConfig<int>();

            WEFormulaeEvalCore.TryEvaluate(
                ref idx, ref compilationStatus, ref loadingFnDone,
                ref runtimeErrorLogged, ref initializedEffectiveText,
                0, ref effectiveValue, default, default,
                null, in config);

            Assert.AreEqual(42, effectiveValue);
        }

        // ── Float Formula Round-Trip ───────────────────────────────────────────

        [Test]
        public void RoundTrip_FloatFormula_ReturnsExpectedValue()
        {
            FormulaeFn<float> fn = (em, e, vars) => TestFormulaeClass.GetFixedFloat(e);
            int idx = SetupFormulaWithFn("integ_float_01", fn);

            byte compilationStatus = 0;
            bool loadingFnDone = true;
            bool runtimeErrorLogged = false;
            bool initializedEffectiveText = false;
            float effectiveValue = 0f;
            var config = new WEFormulaeEvalCore.EvalConfig<float>();

            WEFormulaeEvalCore.TryEvaluate(
                ref idx, ref compilationStatus, ref loadingFnDone,
                ref runtimeErrorLogged, ref initializedEffectiveText,
                0f, ref effectiveValue, default, default,
                null, in config);

            Assert.AreEqual(3.14f, effectiveValue, 0.001f);
        }

        // ── Re-registration (Cache Hit) ────────────────────────────────────────

        [Test]
        public void RoundTrip_ReRegistration_SameKey_UsesCache()
        {
            FormulaeFn<string> fn1 = (em, e, vars) => "first";
            string key = "integ_cache_reuse_01";
            int idx = SetupFormulaWithFn(key, fn1);

            // First evaluation
            var result1 = Evaluate(idx, "fallback");
            Assert.AreEqual("first", result1);

            // GetCachedFn should return the function from cache
            var cached = WEFormulaeHelper.GetCachedFn<string>(key);
            Assert.IsNotNull(cached, "Cached function should be retrievable via GetCachedFn");
        }

        // ══════════════════════════════════════════════════════════════════════════
        // SECTION 3: True End-to-End (SetFormulae → TryEvaluate) — [Ignore]
        // These test the full IL compilation pipeline but require Game.dll.
        // ══════════════════════════════════════════════════════════════════════════

        private const string SetFormulaeIgnoreReason =
            "SetFormulae calls BasicIMod.DebugMode after IL generation, which loads Game.dll — unavailable in test runner";

        [Test, Ignore(SetFormulaeIgnoreReason)]
        public void TrueRoundTrip_ConstStringFormula_ReturnsExpectedValue()
        {
            var resultCode = WEFormulaeHelper.SetFormulae<string>(
                "&TestFormulaeClass;GetGreeting", out _, out var resultStr, out _);
            Assert.AreEqual(0, resultCode);

            int idx = WEStringsBank.Instance[resultStr];
            var result = Evaluate(idx, "fallback");
            Assert.AreEqual("Hello", result);
        }

        [Test, Ignore(SetFormulaeIgnoreReason)]
        public void TrueRoundTrip_DictReadingFormula_ReadsVariable()
        {
            var resultCode = WEFormulaeHelper.SetFormulae<string>(
                "&TestFormulaeClass;GetGreetingWithVars", out _, out var resultStr, out _);
            Assert.AreEqual(0, resultCode);

            int idx = WEStringsBank.Instance[resultStr];
            var vars = new Dictionary<string, string> { ["name"] = "World" };
            var result = Evaluate(idx, "fallback", vars);
            Assert.AreEqual("Hello, World!", result);
        }

        [Test, Ignore(SetFormulaeIgnoreReason)]
        public void TrueRoundTrip_ChainedTransform_DoubleChain()
        {
            var resultCode = WEFormulaeHelper.SetFormulae<string>(
                "&TestFormulaeClass;GetGreeting/&TestFormulaeClass;ToUpperCase/&TestFormulaeClass;AppendExclamation",
                out _, out var resultStr, out _);
            Assert.AreEqual(0, resultCode);

            int idx = WEStringsBank.Instance[resultStr];
            var result = Evaluate(idx, "fallback");
            Assert.AreEqual("HELLO!", result);
        }

        // ── Error Path (SetFormulae returns error before hitting DebugMode) ────

        [Test]
        public void TrueRoundTrip_UnknownMethod_ReturnsNonZeroCode()
        {
            var resultCode = WEFormulaeHelper.SetFormulae<string>(
                "&TestFormulaeClass;NonExistentMethod", out _, out _, out _);
            Assert.AreNotEqual(0, resultCode, "Unknown method should return error code");
        }
    }
}
