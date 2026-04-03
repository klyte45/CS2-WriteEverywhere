using BelzontWE.Utils;
using Colossal.OdinSerializer.Utilities;
using System;
using System.Collections.Generic;
using Unity.Entities;
using static BelzontWE.WEFormulaeHelper;

namespace BelzontWE
{
    public struct WETextDataValueFloat
    {
        public float defaultValue;
        private int formulaeStrBnk;
        public byte formulaeCompilationStatus;
        public bool InitializedEffectiveText { get; private set; }
        public float EffectiveValue { get; private set; }
        private bool loadingFnDone;
        private bool runtimeErrorLogged;

        private static readonly object locker = new();

        private static readonly WEFormulaeEvalCore.EvalConfig<float> s_config = new()
        {
            nullFnFallback = float.NaN,
            errorFallback = float.NaN,
        };

        public string Formulae
        {
            get => WEStringsBank.Instance[formulaeStrBnk];
            set
            {
                formulaeStrBnk = WEStringsBank.Instance[value];
                loadingFnDone = false;
            }
        }
        public byte SetFormulae(string newFormulae, out string[] errorFmtArgs)
        {
            if (newFormulae.IsNullOrWhitespace())
            {
                formulaeStrBnk = 0;
                errorFmtArgs = null;
                return 0;
            }
            var result = formulaeCompilationStatus = WEFormulaeHelper.SetFormulae<float>(newFormulae ?? "", out errorFmtArgs, out var value, out var resultFormulaeFn);
            if (result == 0)
            {
                Formulae = value;
            }
            return result;
        }

        public bool UpdateEffectiveValue(EntityManager em, Entity geometryEntity, Dictionary<string, string> vars)
            => UpdateEffectiveValue(new EntityManagerECSReader(em), geometryEntity, vars);

        public bool UpdateEffectiveValue(IECSReader reader, Entity geometryEntity, Dictionary<string, string> vars)
        {
            var initEff = InitializedEffectiveText;
            var effVal = EffectiveValue;
            bool result;
            lock (locker)
            {
                result = WEFormulaeEvalCore.TryEvaluate(
                    ref formulaeStrBnk, ref formulaeCompilationStatus, ref loadingFnDone,
                    ref runtimeErrorLogged, ref initEff, defaultValue, ref effVal,
                    reader.RawManager, geometryEntity, vars, in s_config);
            }
            InitializedEffectiveText = initEff;
            EffectiveValue = effVal;
            return result;
        }
    }
}