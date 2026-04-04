using BelzontWE.Utils;
using NSubstitute;
using NUnit.Framework;
using System.Collections;
using System.Reflection;
using Unity.Entities;
using static BelzontWE.WEFormulaeHelper;

namespace BelzontWE.Tests.Utils
{
    // Tests for formulae engine error handling (FE-06):
    // null result handling, postProcess application, errorFallback via pre-set runtimeErrorLogged.
    // Scenarios requiring Game.dll (compilation, first-throw DebugMode) are marked [Ignore].

    [TestFixture]
    public class WEFormulaeEngineErrorPathTests
    {
        private static void SetStructField(ref WETextDataValueString v, string fieldName, object value)
        {
            object boxed = v;
            typeof(WETextDataValueString).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(boxed, value);
            v = (WETextDataValueString)boxed;
        }

        private static void InjectStringFormula(string key, FormulaeFn<string> fn, out IDictionary cache)
        {
            var field = typeof(WEFormulaeHelper).GetField("cachedFnsString", BindingFlags.Static | BindingFlags.NonPublic);
            cache = (IDictionary)field.GetValue(null);
            var baseType = typeof(WEFormulaeHelper).GetNestedType("BaseCache`1", BindingFlags.NonPublic).MakeGenericType(typeof(string));
            var inst = System.Activator.CreateInstance(baseType);
            baseType.GetMethod("SetData", BindingFlags.Instance | BindingFlags.Public).Invoke(inst, new object[] { (byte)0, fn, null });
            cache[key] = inst;
        }

        // ── Null result from formula handled by postProcess ────────────────────

        [Test]
        public void NullReturningFormula_PostProcess_ReturnsInvlidFn1Tag()
        {
            // postProcess = v => v?.ToString().Trim().Truncate(500) ?? "<InvlidFn1>" (note: typo in source is intentional)
            string key = "fe06_null_return";
            InjectStringFormula(key, (em, e, vars) => null, out var cache);
            try
            {
                var v = new WETextDataValueString();
                v.Formulae = key;
                SetStructField(ref v, "loadingFnDone", true);
                v.UpdateEffectiveValue(Substitute.For<IECSReader>(), Entity.Null, null);
                Assert.AreEqual("<InvlidFn1>", v.EffectiveValue.ToString(),
                    "Null formula result should map to '<InvlidFn1>' via postProcess");
            }
            finally { cache.Remove(key); }
        }

        [Test]
        public void WhitespaceReturningFormula_PostProcess_TrimsToEmpty()
        {
            string key = "fe06_whitespace_return";
            InjectStringFormula(key, (em, e, vars) => "   ", out var cache);
            try
            {
                var v = new WETextDataValueString();
                v.Formulae = key;
                SetStructField(ref v, "loadingFnDone", true);
                v.UpdateEffectiveValue(Substitute.For<IECSReader>(), Entity.Null, null);
                Assert.AreEqual("", v.EffectiveValue.ToString(),
                    "Whitespace-only formula result should be trimmed to empty string");
            }
            finally { cache.Remove(key); }
        }

        [Test]
        public void LongStringFormula_PostProcess_TruncatesAt500Chars()
        {
            string key = "fe06_long_string";
            string longStr = new string('x', 600);
            InjectStringFormula(key, (em, e, vars) => longStr, out var cache);
            try
            {
                var v = new WETextDataValueString();
                v.Formulae = key;
                SetStructField(ref v, "loadingFnDone", true);
                v.UpdateEffectiveValue(Substitute.For<IECSReader>(), Entity.Null, null);
                Assert.LessOrEqual(v.EffectiveValue.ToString().Length, 500,
                    "Formula result longer than 500 chars should be truncated");
            }
            finally { cache.Remove(key); }
        }

        [Test]
        public void ThrowingFormula_WhenRuntimeErrorAlreadyLogged_ReturnsErrorFallback()
        {
            // Pre-set runtimeErrorLogged=true to bypass BasicIMod.DebugMode (Game.dll blocker)
            string key = "fe06_throwing_formula";
            InjectStringFormula(key, (em, e, vars) => throw new System.InvalidOperationException("simulated formula error"), out var cache);
            try
            {
                var v = new WETextDataValueString();
                v.Formulae = key;
                SetStructField(ref v, "loadingFnDone", true);
                SetStructField(ref v, "runtimeErrorLogged", true);
                v.UpdateEffectiveValue(Substitute.For<IECSReader>(), Entity.Null, null);
                Assert.AreEqual("<ERROR>", v.EffectiveValue.ToString(),
                    "Exception in formula should yield '<ERROR>' from s_config.errorFallback");
            }
            finally { cache.Remove(key); }
        }

        [Test]
        public void UnknownFormulaKey_ReturnsNullFnFallback_InvalidFn2()
        {
            // Formula key stored in WEStringsBank but NOT in the cached function dictionary
            var v = new WETextDataValueString();
            v.Formulae = "fe06_nonexistent_function_xyz";
            SetStructField(ref v, "loadingFnDone", true);
            v.UpdateEffectiveValue(Substitute.For<IECSReader>(), Entity.Null, null);
            Assert.AreEqual("<InvalidFn2>", v.EffectiveValue.ToString(),
                "Formula with no cached function should yield '<InvalidFn2>' from s_config.nullFnFallback");
        }

        // ── Scenarios requiring Game.dll (documented for completeness) ─────────

        [Test, Ignore("Compiling formula with wrong argument count requires SetFormulae which loads Game.dll")]
        public void WrongArgumentCount_Formula_HandledGracefully()
        {
            // Would test: a formula compiled with wrong arg count returns compilation error
            // Cannot test without Game.dll (SetFormulae compilation path)
        }

        [Test, Ignore("Deeply nested/recursive formula cycle detection requires real formula compilation (Game.dll)")]
        public void DeeplyNestedFormula_CycleOrDepthLimit_HandledGracefully()
        {
            // Would test: chain so deep it hits a cycle or stack overflow, returning errorFallback
            // Cannot test without Game.dll (SetFormulae + full chain evaluation)
        }
    }
}
