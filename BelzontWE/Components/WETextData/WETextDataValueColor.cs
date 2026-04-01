using Colossal.OdinSerializer.Utilities;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

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
        private bool runtimeErrorLogged;

        private static readonly WEFormulaeEvalCore.EvalConfig<Color> s_config = new()
        {
            nullFnFallback = Color.cyan,
            errorFallback = Color.magenta,
        };

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
            var initEff = InitializedEffectiveText;
            var effVal = EffectiveValue;
            var result = WEFormulaeEvalCore.TryEvaluate(
                ref formulaeStrBnk, ref formulaeCompilationStatus, ref loadingFnDone,
                ref runtimeErrorLogged, ref initEff, defaultValue, ref effVal,
                em, geometryEntity, vars, in s_config);
            InitializedEffectiveText = initEff;
            EffectiveValue = effVal;
            return result;
        }

    }
}