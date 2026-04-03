using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.Entities;
using static BelzontWE.WEFormulaeHelper;

namespace BelzontWE.Tests.Utils
{
    // Tests for WEFormulaeEvalCore.TryEvaluate evaluation dispatch (FE-02).
    // Uses reflection to inject test delegates into WEFormulaeHelper's private cache
    // and tests dispatch: known formula, variable dict, error fallback, null fn, empty dict.

    /// <summary>
    /// Minimal test class decorated with [WEBuiltinFunction]/[WEFormula] attributes
    /// matching the production pattern. Methods are pure: no game dependencies.
    /// </summary>
    [WEBuiltinFunction("TestFormulae")]
    public static class TestFormulaeClass
    {
        [WEFormula(typeof(string), "Returns a fixed greeting string")]
        public static string GetGreeting(Entity reference) => "Hello";

        [WEFormula(typeof(string), "Returns a greeting using variables")]
        public static string GetGreetingWithVars(Entity reference, Dictionary<string, string> vars)
            => vars != null && vars.TryGetValue("name", out var name) ? $"Hello, {name}!" : "Hello, stranger!";

        [WEFormula(typeof(int), "Returns a fixed integer")]
        public static int GetFixedInt(Entity reference) => 42;

        [WEFormula(typeof(float), "Returns a fixed float")]
        public static float GetFixedFloat(Entity reference) => 3.14f;
    }

    [TestFixture]
    public class WEFormulaeEvalCoreDispatchTests
    {
        private static readonly BindingFlags AllStatic = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

        // Reflection handles for injecting into the private cache dictionaries
        private static IDictionary GetCacheDictionary(string fieldName)
        {
            var field = typeof(WEFormulaeHelper).GetField(fieldName, AllStatic);
            Assert.IsNotNull(field, $"Could not find field '{fieldName}' via reflection");
            return (IDictionary)field.GetValue(null);
        }

        /// <summary>
        /// Injects a FormulaeFn&lt;T&gt; into WEFormulaeHelper's private cache dictionary
        /// using the same BaseCache&lt;T&gt; pattern the production code uses.
        /// </summary>
        private static void InjectCachedFunction<T>(string formulaKey, FormulaeFn<T> fn, byte resultCode = 0)
        {
            string fieldName;
            if (typeof(T) == typeof(string)) fieldName = "cachedFnsString";
            else if (typeof(T) == typeof(float)) fieldName = "cachedFnsFloat";
            else if (typeof(T) == typeof(int)) fieldName = "cachedFnsInt";
            else throw new NotSupportedException($"No cache dictionary for type {typeof(T).Name}");

            var dic = GetCacheDictionary(fieldName);

            // Create a BaseCache<T> instance via reflection (it's a private nested class)
            var baseCacheType = typeof(WEFormulaeHelper).GetNestedType("BaseCache`1", BindingFlags.NonPublic);
            Assert.IsNotNull(baseCacheType, "Could not find nested type BaseCache`1");
            var concreteType = baseCacheType.MakeGenericType(typeof(T));
            var cache = Activator.CreateInstance(concreteType);

            // Call SetData(resultCode, fn) via the IBaseCache interface
            var setDataMethod = concreteType.GetMethod("SetData", BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(setDataMethod, "Could not find SetData method on BaseCache<T>");
            setDataMethod.Invoke(cache, new object[] { resultCode, fn, null });

            dic[formulaKey] = cache;
        }

        /// <summary>
        /// Removes a key from WEFormulaeHelper's private cache dictionary.
        /// </summary>
        private static void RemoveCachedFunction<T>(string formulaKey)
        {
            string fieldName;
            if (typeof(T) == typeof(string)) fieldName = "cachedFnsString";
            else if (typeof(T) == typeof(float)) fieldName = "cachedFnsFloat";
            else if (typeof(T) == typeof(int)) fieldName = "cachedFnsInt";
            else return;

            var dic = GetCacheDictionary(fieldName);
            if (dic.Contains(formulaKey)) dic.Remove(formulaKey);
        }

        /// <summary>
        /// Registers a string in WEStringsBank and returns its index.
        /// </summary>
        private static int RegisterString(string value)
        {
            return WEStringsBank.Instance[value];
        }

        // Keys used in tests — unique per test to avoid cross-contamination
        private readonly List<string> _registeredKeys = new();

        private int SetupFormulaWithFn<T>(string key, FormulaeFn<T> fn)
        {
            _registeredKeys.Add(key);
            int idx = RegisterString(key);
            InjectCachedFunction(key, fn);
            return idx;
        }

        [TearDown]
        public void Cleanup()
        {
            foreach (var key in _registeredKeys)
            {
                RemoveCachedFunction<string>(key);
                RemoveCachedFunction<float>(key);
                RemoveCachedFunction<int>(key);
            }
            _registeredKeys.Clear();
        }

        // ── No-Formula Path (formulaeStrBnk == 0) ─────────────────────────────

        [Test]
        public void TryEvaluate_NoFormula_SetsDefaultValue()
        {
            int formulaeStrBnk = 0;
            byte compilationStatus = 0;
            bool loadingFnDone = true;
            bool runtimeErrorLogged = false;
            bool initializedEffectiveText = false;
            string effectiveValue = "old";
            var config = new WEFormulaeEvalCore.EvalConfig<string>
            {
                nullFnFallback = "NULLFN",
                errorFallback = "ERROR",
            };

            WEFormulaeEvalCore.TryEvaluate(
                ref formulaeStrBnk, ref compilationStatus, ref loadingFnDone,
                ref runtimeErrorLogged, ref initializedEffectiveText,
                "DEFAULT", ref effectiveValue, default, default,
                null, in config);

            Assert.AreEqual("DEFAULT", effectiveValue);
            Assert.IsTrue(initializedEffectiveText);
        }

        [Test]
        public void TryEvaluate_NoFormula_SecondCall_ReturnsFalse()
        {
            int formulaeStrBnk = 0;
            byte compilationStatus = 0;
            bool loadingFnDone = true;
            bool runtimeErrorLogged = false;
            bool initializedEffectiveText = false;
            string effectiveValue = "DEFAULT";
            var config = new WEFormulaeEvalCore.EvalConfig<string>();

            // First call sets effectiveValue = defaultValue; oldValue = "DEFAULT"
            // effectiveValue = "DEFAULT" == oldValue "DEFAULT" => false (no change)
            var result = WEFormulaeEvalCore.TryEvaluate(
                ref formulaeStrBnk, ref compilationStatus, ref loadingFnDone,
                ref runtimeErrorLogged, ref initializedEffectiveText,
                "DEFAULT", ref effectiveValue, default, default,
                null, in config);

            Assert.IsFalse(result, "No change should return false");
        }

        [Test]
        public void TryEvaluate_NoFormula_ValueChanges_ReturnsTrue()
        {
            int formulaeStrBnk = 0;
            byte compilationStatus = 0;
            bool loadingFnDone = true;
            bool runtimeErrorLogged = false;
            bool initializedEffectiveText = false;
            string effectiveValue = "old";
            var config = new WEFormulaeEvalCore.EvalConfig<string>();

            var result = WEFormulaeEvalCore.TryEvaluate(
                ref formulaeStrBnk, ref compilationStatus, ref loadingFnDone,
                ref runtimeErrorLogged, ref initializedEffectiveText,
                "DEFAULT", ref effectiveValue, default, default,
                null, in config);

            Assert.IsTrue(result, "Value changed from 'old' to 'DEFAULT', should return true");
            Assert.AreEqual("DEFAULT", effectiveValue);
        }

        // ── Known Formula → Cached Function ────────────────────────────────────

        [Test]
        public void TryEvaluate_KnownFormula_ReturnsEvaluatedString()
        {
            FormulaeFn<string> fn = (em, e, vars) => "Hello";
            int formulaeStrBnk = SetupFormulaWithFn("test_known_formula_01", fn);

            byte compilationStatus = 0;
            bool loadingFnDone = true;
            bool runtimeErrorLogged = false;
            bool initializedEffectiveText = false;
            string effectiveValue = "";
            var config = new WEFormulaeEvalCore.EvalConfig<string>
            {
                nullFnFallback = "NULLFN",
                errorFallback = "ERROR",
            };

            WEFormulaeEvalCore.TryEvaluate(
                ref formulaeStrBnk, ref compilationStatus, ref loadingFnDone,
                ref runtimeErrorLogged, ref initializedEffectiveText,
                "DEFAULT", ref effectiveValue, default, default,
                null, in config);

            Assert.AreEqual("Hello", effectiveValue);
        }

        // ── Formula With Variable Replaced From Dict ───────────────────────────

        [Test]
        public void TryEvaluate_FormulaWithVarDict_ReplacesVariable()
        {
            FormulaeFn<string> fn = (em, e, vars) =>
                vars != null && vars.TryGetValue("name", out var name)
                    ? $"Hello, {name}!"
                    : "Hello, stranger!";
            int formulaeStrBnk = SetupFormulaWithFn("test_var_formula_01", fn);

            byte compilationStatus = 0;
            bool loadingFnDone = true;
            bool runtimeErrorLogged = false;
            bool initializedEffectiveText = false;
            string effectiveValue = "";
            var vars = new Dictionary<string, string> { ["name"] = "World" };
            var config = new WEFormulaeEvalCore.EvalConfig<string>
            {
                nullFnFallback = "NULLFN",
                errorFallback = "ERROR",
            };

            WEFormulaeEvalCore.TryEvaluate(
                ref formulaeStrBnk, ref compilationStatus, ref loadingFnDone,
                ref runtimeErrorLogged, ref initializedEffectiveText,
                "DEFAULT", ref effectiveValue, default, default,
                vars, in config);

            Assert.AreEqual("Hello, World!", effectiveValue);
        }

        // ── Unknown Formula → Null Fn → nullFnFallback ────────────────────────

        [Test]
        public void TryEvaluate_UnknownFormula_ReturnsNullFnFallback()
        {
            // Register a string in WEStringsBank but do NOT inject a cached function
            string unknownKey = "test_unknown_formula_01";
            _registeredKeys.Add(unknownKey);
            int formulaeStrBnk = RegisterString(unknownKey);

            byte compilationStatus = 0;
            bool loadingFnDone = true;
            bool runtimeErrorLogged = false;
            bool initializedEffectiveText = false;
            string effectiveValue = "";
            var config = new WEFormulaeEvalCore.EvalConfig<string>
            {
                nullFnFallback = "NULL_FALLBACK",
                errorFallback = "ERROR_FALLBACK",
            };

            WEFormulaeEvalCore.TryEvaluate(
                ref formulaeStrBnk, ref compilationStatus, ref loadingFnDone,
                ref runtimeErrorLogged, ref initializedEffectiveText,
                "DEFAULT", ref effectiveValue, default, default,
                null, in config);

            Assert.AreEqual("NULL_FALLBACK", effectiveValue);
        }

        // ── Error Fallback (Exception in Formula Function) ─────────────────────

        [Test, Ignore("BasicIMod.DebugMode in catch block triggers Game.dll load — no Game.dll in test runner")]
        public void TryEvaluate_ThrowingFormula_ReturnsErrorFallback()
        {
            FormulaeFn<string> fn = (em, e, vars) => throw new InvalidOperationException("test boom");
            int formulaeStrBnk = SetupFormulaWithFn("test_error_formula_01", fn);

            byte compilationStatus = 0;
            bool loadingFnDone = true;
            bool runtimeErrorLogged = false;
            bool initializedEffectiveText = false;
            string effectiveValue = "";
            var config = new WEFormulaeEvalCore.EvalConfig<string>
            {
                nullFnFallback = "NULLFN",
                errorFallback = "ERROR_CAUGHT",
            };

            WEFormulaeEvalCore.TryEvaluate(
                ref formulaeStrBnk, ref compilationStatus, ref loadingFnDone,
                ref runtimeErrorLogged, ref initializedEffectiveText,
                "DEFAULT", ref effectiveValue, default, default,
                null, in config);

            Assert.AreEqual("ERROR_CAUGHT", effectiveValue);
        }

        [Test, Ignore("BasicIMod.DebugMode in catch block triggers Game.dll load — no Game.dll in test runner")]
        public void TryEvaluate_ThrowingFormula_SetsCompilationStatus255()
        {
            FormulaeFn<string> fn = (em, e, vars) => throw new Exception("fail");
            int formulaeStrBnk = SetupFormulaWithFn("test_error_status_01", fn);

            byte compilationStatus = 0;
            bool loadingFnDone = true;
            bool runtimeErrorLogged = false;
            bool initializedEffectiveText = false;
            string effectiveValue = "";
            var config = new WEFormulaeEvalCore.EvalConfig<string>
            {
                errorFallback = "ERR",
            };

            WEFormulaeEvalCore.TryEvaluate(
                ref formulaeStrBnk, ref compilationStatus, ref loadingFnDone,
                ref runtimeErrorLogged, ref initializedEffectiveText,
                "DEFAULT", ref effectiveValue, default, default,
                null, in config);

            Assert.AreEqual(255, compilationStatus);
            Assert.IsTrue(runtimeErrorLogged);
        }

        [Test, Ignore("BasicIMod.DebugMode in catch block triggers Game.dll load — no Game.dll in test runner")]
        public void TryEvaluate_ThrowingFormula_SecondCall_DoesNotRelogError()
        {
            FormulaeFn<string> fn = (em, e, vars) => throw new Exception("fail twice");
            int formulaeStrBnk = SetupFormulaWithFn("test_error_relog_01", fn);

            byte compilationStatus = 0;
            bool loadingFnDone = true;
            bool runtimeErrorLogged = false;
            bool initializedEffectiveText = false;
            string effectiveValue = "";
            var config = new WEFormulaeEvalCore.EvalConfig<string>
            {
                errorFallback = "ERR",
            };

            // First call
            WEFormulaeEvalCore.TryEvaluate(
                ref formulaeStrBnk, ref compilationStatus, ref loadingFnDone,
                ref runtimeErrorLogged, ref initializedEffectiveText,
                "DEFAULT", ref effectiveValue, default, default,
                null, in config);

            Assert.IsTrue(runtimeErrorLogged, "First call should set runtimeErrorLogged");
            Assert.AreEqual(255, compilationStatus);

            // Second call — runtimeErrorLogged remains true, compilationStatus not re-set
            byte prevStatus = compilationStatus;
            WEFormulaeEvalCore.TryEvaluate(
                ref formulaeStrBnk, ref compilationStatus, ref loadingFnDone,
                ref runtimeErrorLogged, ref initializedEffectiveText,
                "DEFAULT", ref effectiveValue, default, default,
                null, in config);

            Assert.IsTrue(runtimeErrorLogged, "runtimeErrorLogged should stay true on second error");
            Assert.AreEqual(prevStatus, compilationStatus);
        }

        // ── Null/Empty Dict Handling ───────────────────────────────────────────

        [Test]
        public void TryEvaluate_NullDict_FormulaStillEvaluates()
        {
            FormulaeFn<string> fn = (em, e, vars) =>
                vars == null ? "null_dict" : "has_dict";
            int formulaeStrBnk = SetupFormulaWithFn("test_null_dict_01", fn);

            byte compilationStatus = 0;
            bool loadingFnDone = true;
            bool runtimeErrorLogged = false;
            bool initializedEffectiveText = false;
            string effectiveValue = "";
            var config = new WEFormulaeEvalCore.EvalConfig<string>();

            WEFormulaeEvalCore.TryEvaluate(
                ref formulaeStrBnk, ref compilationStatus, ref loadingFnDone,
                ref runtimeErrorLogged, ref initializedEffectiveText,
                "DEFAULT", ref effectiveValue, default, default,
                null, in config);

            Assert.AreEqual("null_dict", effectiveValue);
        }

        [Test]
        public void TryEvaluate_EmptyDict_FormulaStillEvaluates()
        {
            FormulaeFn<string> fn = (em, e, vars) =>
                vars != null && vars.Count == 0 ? "empty_dict" : "other";
            int formulaeStrBnk = SetupFormulaWithFn("test_empty_dict_01", fn);

            byte compilationStatus = 0;
            bool loadingFnDone = true;
            bool runtimeErrorLogged = false;
            bool initializedEffectiveText = false;
            string effectiveValue = "";
            var config = new WEFormulaeEvalCore.EvalConfig<string>();

            WEFormulaeEvalCore.TryEvaluate(
                ref formulaeStrBnk, ref compilationStatus, ref loadingFnDone,
                ref runtimeErrorLogged, ref initializedEffectiveText,
                "DEFAULT", ref effectiveValue, default, default,
                new Dictionary<string, string>(), in config);

            Assert.AreEqual("empty_dict", effectiveValue);
        }

        [Test]
        public void TryEvaluate_VarDictWithMissingKey_FormulaHandlesGracefully()
        {
            FormulaeFn<string> fn = (em, e, vars) =>
                vars != null && vars.TryGetValue("missing", out var v) ? v : "not_found";
            int formulaeStrBnk = SetupFormulaWithFn("test_missing_key_01", fn);

            byte compilationStatus = 0;
            bool loadingFnDone = true;
            bool runtimeErrorLogged = false;
            bool initializedEffectiveText = false;
            string effectiveValue = "";
            var config = new WEFormulaeEvalCore.EvalConfig<string>();

            var vars = new Dictionary<string, string> { ["other"] = "value" };
            WEFormulaeEvalCore.TryEvaluate(
                ref formulaeStrBnk, ref compilationStatus, ref loadingFnDone,
                ref runtimeErrorLogged, ref initializedEffectiveText,
                "DEFAULT", ref effectiveValue, default, default,
                vars, in config);

            Assert.AreEqual("not_found", effectiveValue);
        }

        // ── PostProcess Config ─────────────────────────────────────────────────

        [Test]
        public void TryEvaluate_WithPostProcess_AppliesTransformation()
        {
            FormulaeFn<string> fn = (em, e, vars) => "hello";
            int formulaeStrBnk = SetupFormulaWithFn("test_postprocess_01", fn);

            byte compilationStatus = 0;
            bool loadingFnDone = true;
            bool runtimeErrorLogged = false;
            bool initializedEffectiveText = false;
            string effectiveValue = "";
            var config = new WEFormulaeEvalCore.EvalConfig<string>
            {
                postProcess = s => s.ToUpper(),
            };

            WEFormulaeEvalCore.TryEvaluate(
                ref formulaeStrBnk, ref compilationStatus, ref loadingFnDone,
                ref runtimeErrorLogged, ref initializedEffectiveText,
                "DEFAULT", ref effectiveValue, default, default,
                null, in config);

            Assert.AreEqual("HELLO", effectiveValue);
        }

        // ── EqualityCheck Config ───────────────────────────────────────────────

        [Test]
        public void TryEvaluate_CustomEqualityCheck_UsedForReturnValue()
        {
            FormulaeFn<string> fn = (em, e, vars) => "HELLO";
            int formulaeStrBnk = SetupFormulaWithFn("test_eq_check_01", fn);

            byte compilationStatus = 0;
            bool loadingFnDone = true;
            bool runtimeErrorLogged = false;
            bool initializedEffectiveText = false;
            string effectiveValue = "hello"; // different case
            var config = new WEFormulaeEvalCore.EvalConfig<string>
            {
                // Case-insensitive equality check — "hello" and "HELLO" are equal
                equalityCheck = (a, b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase),
            };

            var result = WEFormulaeEvalCore.TryEvaluate(
                ref formulaeStrBnk, ref compilationStatus, ref loadingFnDone,
                ref runtimeErrorLogged, ref initializedEffectiveText,
                "DEFAULT", ref effectiveValue, default, default,
                null, in config);

            // equalityCheck returns true (equal) → TryEvaluate returns false (no change detected)
            Assert.IsFalse(result, "Case-insensitive equality should detect no change");
        }

        // ── First-Load Path (loadingFnDone = false, formulaeStrBnk == 0) ──────

        [Test]
        public void TryEvaluate_FirstLoad_NoFormula_SetsLoadingFnDone()
        {
            int formulaeStrBnk = 0;
            byte compilationStatus = 0;
            bool loadingFnDone = false; // first call
            bool runtimeErrorLogged = false;
            bool initializedEffectiveText = false;
            string effectiveValue = "";
            var config = new WEFormulaeEvalCore.EvalConfig<string>();

            var result = WEFormulaeEvalCore.TryEvaluate(
                ref formulaeStrBnk, ref compilationStatus, ref loadingFnDone,
                ref runtimeErrorLogged, ref initializedEffectiveText,
                "DEFAULT", ref effectiveValue, default, default,
                null, in config);

            Assert.IsTrue(loadingFnDone, "loadingFnDone should be set to true after first call");
            Assert.IsTrue(result, "First call with loadedFnNow=true should return true");
            Assert.AreEqual("DEFAULT", effectiveValue);
        }

        [Test]
        public void TryEvaluate_FirstLoad_ReturnsTrue_EvenWhenValueUnchanged()
        {
            int formulaeStrBnk = 0;
            byte compilationStatus = 0;
            bool loadingFnDone = false;
            bool runtimeErrorLogged = false;
            bool initializedEffectiveText = false;
            string effectiveValue = "DEFAULT"; // same as default — no value change
            var config = new WEFormulaeEvalCore.EvalConfig<string>();

            var result = WEFormulaeEvalCore.TryEvaluate(
                ref formulaeStrBnk, ref compilationStatus, ref loadingFnDone,
                ref runtimeErrorLogged, ref initializedEffectiveText,
                "DEFAULT", ref effectiveValue, default, default,
                null, in config);

            // loadedFnNow = true → return always includes true for first load
            Assert.IsTrue(result, "First load should return true even if value unchanged");
        }

        // ── Type Mismatch in Chain → Error Fallback ────────────────────────────

        [Test]
        public void TryEvaluate_TypeMismatch_IntFormulaInStringEval_ErrorFallback()
        {
            // Inject an int-returning function, but evaluate as string type.
            // GetCachedFn<string> will return null for an int-keyed cache entry → nullFnFallback.
            string key = "test_type_mismatch_01";
            _registeredKeys.Add(key);
            int formulaeStrBnk = RegisterString(key);
            InjectCachedFunction<int>(key, (em, e, vars) => 42);

            byte compilationStatus = 0;
            bool loadingFnDone = true;
            bool runtimeErrorLogged = false;
            bool initializedEffectiveText = false;
            string effectiveValue = "";
            var config = new WEFormulaeEvalCore.EvalConfig<string>
            {
                nullFnFallback = "TYPE_MISMATCH_FALLBACK",
                errorFallback = "ERROR_FALLBACK",
            };

            WEFormulaeEvalCore.TryEvaluate(
                ref formulaeStrBnk, ref compilationStatus, ref loadingFnDone,
                ref runtimeErrorLogged, ref initializedEffectiveText,
                "DEFAULT", ref effectiveValue, default, default,
                null, in config);

            // Type mismatch: int cache has it, but string cache does not → null fn → nullFnFallback
            Assert.AreEqual("TYPE_MISMATCH_FALLBACK", effectiveValue);
        }

        // ── Float Type Dispatch ────────────────────────────────────────────────

        [Test]
        public void TryEvaluate_Float_KnownFormula_ReturnsEvaluatedValue()
        {
            FormulaeFn<float> fn = (em, e, vars) => 3.14f;
            int formulaeStrBnk = SetupFormulaWithFn("test_float_formula_01", fn);

            byte compilationStatus = 0;
            bool loadingFnDone = true;
            bool runtimeErrorLogged = false;
            bool initializedEffectiveText = false;
            float effectiveValue = 0f;
            var config = new WEFormulaeEvalCore.EvalConfig<float>
            {
                nullFnFallback = -1f,
                errorFallback = -999f,
            };

            WEFormulaeEvalCore.TryEvaluate(
                ref formulaeStrBnk, ref compilationStatus, ref loadingFnDone,
                ref runtimeErrorLogged, ref initializedEffectiveText,
                0f, ref effectiveValue, default, default,
                null, in config);

            Assert.AreEqual(3.14f, effectiveValue, 0.001f);
        }

        [Test, Ignore("BasicIMod.DebugMode in catch block triggers Game.dll load — no Game.dll in test runner")]
        public void TryEvaluate_Float_ThrowingFormula_ReturnsErrorFallback()
        {
            FormulaeFn<float> fn = (em, e, vars) => throw new ArithmeticException("div/0");
            int formulaeStrBnk = SetupFormulaWithFn("test_float_error_01", fn);

            byte compilationStatus = 0;
            bool loadingFnDone = true;
            bool runtimeErrorLogged = false;
            bool initializedEffectiveText = false;
            float effectiveValue = 0f;
            var config = new WEFormulaeEvalCore.EvalConfig<float>
            {
                errorFallback = -999f,
            };

            WEFormulaeEvalCore.TryEvaluate(
                ref formulaeStrBnk, ref compilationStatus, ref loadingFnDone,
                ref runtimeErrorLogged, ref initializedEffectiveText,
                0f, ref effectiveValue, default, default,
                null, in config);

            Assert.AreEqual(-999f, effectiveValue, 0.001f);
            Assert.AreEqual(255, compilationStatus);
        }
    }
}
