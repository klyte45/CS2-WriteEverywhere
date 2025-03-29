using Belzont.Utils;
using Colossal.OdinSerializer.Utilities;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Entities;
using UnityEngine;
using static BelzontWE.WEFormulaeHelper;

namespace BelzontWE
{
    public struct WETextDataValueColor : IDisposable
    {
        public Color defaultValue;
        private GCHandle formulaeGC;
        public byte formulaeCompilationStatus;
        public string Formulae
        {
            get => formulaeGC.IsAllocated ? formulaeGC.Target as string ?? "" : "";
            set
            {
                if (formulaeGC.IsAllocated)
                {
                    if (value == (formulaeGC.Target as string)) return;
                    formulaeGC.Free();
                }
                if (!value.IsNullOrWhitespace()) formulaeGC = GCHandle.Alloc(new string(value));
                loadingFnDone = false;
            }
        }

        public bool InitializedEffectiveText { get; private set; }
        public Color EffectiveValue { get; private set; }
        private bool loadingFnDone;

        public byte SetFormulae(string newFormulae, out string[] errorFmtArgs)
        {
            if (newFormulae.IsNullOrWhitespace())
            {
                if (formulaeGC.IsAllocated) formulaeGC.Free();
                errorFmtArgs = null;
                return 0;
            }
            var result = formulaeCompilationStatus = WEFormulaeHelper.SetFormulae<Color>(newFormulae ?? "", out errorFmtArgs, out var value, out var resultFormulaeFn);
            if (result == 0)
            {
                Formulae = value;
            }
            return result;
        }

        public bool UpdateEffectiveValue(EntityManager em, Entity geometryEntity, Dictionary<string, string> vars)
        {
            InitializedEffectiveText = true;
            var loadedFnNow = false;
            if (!loadingFnDone)
            {
                if (formulaeGC.IsAllocated)
                {
                    formulaeCompilationStatus = SetFormulae(Formulae, out _);
                }
                loadedFnNow = loadingFnDone = true;
            }
            var oldVal = EffectiveValue;
            
            try
            {
                EffectiveValue = formulaeGC.IsAllocated
                    ? WEFormulaeHelper.GetCachedColorFn(Formulae) is FormulaeFn<Color> fn
                        ? fn(em, geometryEntity, vars)
                        : Color.cyan
                    : defaultValue;
            }
            catch (Exception e)
            {
                LogUtils.DoLog($"Error running formulae @{geometryEntity}: {e}");
                EffectiveValue = Color.magenta;
            }
            return loadedFnNow || EffectiveValue != oldVal;
        }

        public void Dispose()
        {
            if (formulaeGC.IsAllocated) formulaeGC.Free();
        }

        public bool IsInconsistent => formulaeGC.IsAllocated && formulaeGC.Target == null;
    }
}