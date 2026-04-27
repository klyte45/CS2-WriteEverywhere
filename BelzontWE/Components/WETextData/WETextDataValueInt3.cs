using BelzontWE.Utils;
using Colossal.OdinSerializer.Utilities;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;

namespace BelzontWE
{
    public struct WETextDataValueInt3
    {
        public int3 defaultValue;
        private int formulaeStrBnk;
        public byte formulaeCompilationStatus;
        public bool InitializedEffectiveText { get; private set; }
        public int3 EffectiveValue { get; private set; }
        private bool loadingFnDone;
        private bool runtimeErrorLogged;

        private static readonly WEFormulaeEvalCore.EvalConfig<int3> s_config = new()
        {
            nullFnFallback = new int3(int.MinValue, int.MinValue, int.MinValue),
            errorFallback = new int3(int.MinValue, int.MinValue, int.MinValue),
            equalityCheck = (a, b) => a.x == b.x && a.y == b.y && a.z == b.z,
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
            var result = formulaeCompilationStatus = WEFormulaeHelper.SetFormulae<int3>(newFormulae ?? "", out errorFmtArgs, out var value, out var resultFormulaeFn);
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
            var result = WEFormulaeEvalCore.TryEvaluate(
                ref formulaeStrBnk, ref formulaeCompilationStatus, ref loadingFnDone,
                ref runtimeErrorLogged, ref initEff, defaultValue, ref effVal,
                reader.RawManager, geometryEntity, vars, in s_config);
            InitializedEffectiveText = initEff;
            EffectiveValue = effVal;
            return result;
        }
    }
}