using Belzont.Interfaces;
using Belzont.Utils;
using Colossal.OdinSerializer.Utilities;
using System;
using System.Collections.Generic;
using Unity.Entities;
using static BelzontWE.WEFormulaeHelper;

namespace BelzontWE
{
    public static class WEFormulaeEvalCore
    {
        public struct EvalConfig<T>
        {
            public T nullFnFallback;
            public T errorFallback;
            public Func<T, T, bool> equalityCheck;
            public Func<T, T> postProcess;
        }

        /// <summary>
        /// Tokenizes a formula string into its path segments by splitting on '/'.
        /// Returns null if formula is null. Returns a single empty-string array for empty input.
        /// Each segment is either a component-reference ("TypeName;field.path")
        /// or a method-call reference (starts with '&amp;').
        /// </summary>
        internal static string[] TokenizeFormula(string formula) => formula?.Split(new[] { "/" }, StringSplitOptions.None);

        /// <summary>
        /// Returns true if the token represents a method-call segment (starts with '&amp;').
        /// </summary>
        internal static bool IsMethodCallToken(string token) => token != null && token.Length > 0 && token[0] == '&';

        /// <summary>
        /// Classifies a non-null token: "method" if it starts with '&amp;', "component" otherwise.
        /// </summary>
        internal static string ClassifyToken(string token)
        {
            if (token == null) return null;
            return IsMethodCallToken(token) ? "method" : "component";
        }

        public static bool TryEvaluate<T>(
            ref int formulaeStrBnk,
            ref byte formulaeCompilationStatus,
            ref bool loadingFnDone,
            ref bool runtimeErrorLogged,
            ref bool initializedEffectiveText,
            T defaultValue,
            ref T effectiveValue,
            EntityManager em,
            Entity geometryEntity,
            Dictionary<string, string> vars,
            in EvalConfig<T> config)
        {
            initializedEffectiveText = true;
            var loadedFnNow = false;
            if (!loadingFnDone)
            {
                if (formulaeStrBnk > 0)
                {
                    var formulae = WEStringsBank.Instance[formulaeStrBnk];
                    if (!formulae.IsNullOrWhitespace())
                    {
                        var result = formulaeCompilationStatus = SetFormulae<T>(formulae, out _, out var value, out _);
                        if (result == 0)
                        {
                            formulaeStrBnk = WEStringsBank.Instance[value];
                        }
                    }
                    else
                    {
                        formulaeStrBnk = 0;
                    }
                }
                loadedFnNow = loadingFnDone = true;
                runtimeErrorLogged = false;
            }
            var oldValue = effectiveValue;
            try
            {
                if (formulaeStrBnk > 0)
                {
                    var formulae = WEStringsBank.Instance[formulaeStrBnk];
                    if (GetCachedFn<T>(formulae) is FormulaeFn<T> fn)
                    {
                        var result = fn(em, geometryEntity, vars);
                        effectiveValue = config.postProcess != null ? config.postProcess(result) : result;
                    }
                    else
                    {
                        effectiveValue = config.nullFnFallback;
                    }
                }
                else
                {
                    effectiveValue = defaultValue;
                }
            }
            catch (Exception e)
            {
                effectiveValue = config.errorFallback;
                if (!runtimeErrorLogged)
                {
                    runtimeErrorLogged = true;
                    formulaeCompilationStatus = 255;
                    if (BasicIMod.DebugMode) LogUtils.DoLog($"Runtime error in {typeof(T).Name} formulae @{geometryEntity}: {e}");
                }
            }
            return loadedFnNow || (config.equalityCheck != null
                ? !config.equalityCheck(effectiveValue, oldValue)
                : !EqualityComparer<T>.Default.Equals(effectiveValue, oldValue));
        }
    }
}
