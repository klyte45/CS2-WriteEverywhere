using Belzont.Interfaces;
using Belzont.Utils;
using Colossal.OdinSerializer.Utilities;
using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using static BelzontWE.WEFormulaeHelper;

namespace BelzontWE
{
    public struct WETextDataValueColor
    {
        public Color defaultValue;
        private int formulaeStrBnk;
        public byte formulaeCompilationStatus;
        public string Formulae
        {
            get => WEStringsBank.Instance[formulaeStrBnk];
            set
            {
                formulaeStrBnk = WEStringsBank.Instance[value];
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
                formulaeStrBnk = 0;
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
                if (formulaeStrBnk > 0)
                {
                    formulaeCompilationStatus = SetFormulae(Formulae, out _);
                }
                loadedFnNow = loadingFnDone = true;
            }
            var oldVal = EffectiveValue;

            try
            {
                EffectiveValue = formulaeStrBnk > 0
                    ? WEFormulaeHelper.GetCachedColorFn(Formulae) is FormulaeFn<Color> fn
                        ? fn(em, geometryEntity, vars)
                        : Color.cyan
                    : defaultValue;
            }
            catch (Exception e)
            {
                if (BasicIMod.DebugMode) LogUtils.DoLog($"Error running formulae @{geometryEntity}: {e}");
                EffectiveValue = Color.magenta;
            }
            return loadedFnNow || EffectiveValue != oldVal;
        }

    }
}