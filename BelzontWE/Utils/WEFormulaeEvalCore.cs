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
