using Belzont.Utils;
using BelzontWE.Utils;
using Colossal.OdinSerializer.Utilities;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace BelzontWE
{
    public struct WETextDataValueString
    {
        private int defaultValueStrBnk;
        private int formulaeStrBnk;
        public byte formulaeCompilationStatus;
        public bool InitializedEffectiveText { get; private set; }
        public FixedString512Bytes EffectiveValue { get; private set; }
        private bool loadingFnDone;
        private bool runtimeErrorLogged;

        private static readonly WEFormulaeEvalCore.EvalConfig<string> s_config = new()
        {
            nullFnFallback = "<InvalidFn2>",
            errorFallback = "<ERROR>",
            postProcess = v => v?.ToString().Trim().Truncate(500) ?? "<InvlidFn1>",
        };

        public string Formulae
        {
            readonly get => WEStringsBank.Instance[formulaeStrBnk];
            set
            {
                formulaeStrBnk = WEStringsBank.Instance[value];
                loadingFnDone = false;
            }
        }

        public string DefaultValue
        {
            readonly get => WEStringsBank.Instance[defaultValueStrBnk];
            set
            {
                defaultValueStrBnk = WEStringsBank.Instance[value];
            }
        }

        public readonly bool IsEmpty => defaultValueStrBnk <= 0 && formulaeStrBnk <= 0;

        public byte SetFormulae(string newFormulae, out string[] errorFmtArgs)
        {
            if (newFormulae.IsNullOrWhitespace())
            {
                formulaeStrBnk = 0;
                errorFmtArgs = null;
                return 0;
            }
            var result = formulaeCompilationStatus = WEFormulaeHelper.SetFormulae<string>(newFormulae ?? "", out errorFmtArgs, out var value, out var resultFormulaeFn);
            if (result == 0)
            {
                Formulae = value;
            }
            return result;
        }

        public bool UpdateEffectiveValue(EntityManager em, Entity geometryEntity, Dictionary<string, string> vars)
            => UpdateEffectiveValue(new EntityManagerECSReader(em), geometryEntity, EffectiveValue.ToString(), vars);

        public bool UpdateEffectiveValue(EntityManager em, Entity geometryEntity, string oldEffText, Dictionary<string, string> vars)
            => UpdateEffectiveValue(new EntityManagerECSReader(em), geometryEntity, oldEffText, vars);

        public bool UpdateEffectiveValue(IECSReader reader, Entity geometryEntity, Dictionary<string, string> vars)
            => UpdateEffectiveValue(reader, geometryEntity, EffectiveValue.ToString(), vars);

        public bool UpdateEffectiveValue(IECSReader reader, Entity geometryEntity, string oldEffText, Dictionary<string, string> vars)
        {
            var initEff = InitializedEffectiveText;
            string effVal = EffectiveValue.ToString();
            var loadedOrChanged = WEFormulaeEvalCore.TryEvaluate(
                ref formulaeStrBnk, ref formulaeCompilationStatus, ref loadingFnDone,
                ref runtimeErrorLogged, ref initEff, DefaultValue, ref effVal,
                reader.RawManager, geometryEntity, vars, in s_config);
            InitializedEffectiveText = initEff;
            EffectiveValue = effVal;
            return loadedOrChanged || EffectiveValue.ToString() != oldEffText;
        }
    }
}
