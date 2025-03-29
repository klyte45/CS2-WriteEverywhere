using Colossal.OdinSerializer.Utilities;
using System.Collections.Generic;
using Unity.Entities;
using static BelzontWE.WEFormulaeHelper;

namespace BelzontWE
{
    public struct WETextDataValueFloat
    {
        public float defaultValue;
        private int formulaeStrBnk;
        public bool InitializedEffectiveText { get; private set; }
        public float EffectiveValue { get; private set; }
        private bool loadingFnDone;
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
            var result = WEFormulaeHelper.SetFormulae<float>(newFormulae ?? "", out errorFmtArgs, out var value, out var resultFormulaeFn);
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
                    SetFormulae(Formulae, out _);
                }
                loadedFnNow = loadingFnDone = true;
            }
            var oldValue = EffectiveValue;
            try
            {
                EffectiveValue = formulaeStrBnk > 0
                    ? WEFormulaeHelper.GetCachedFloatFn(Formulae) is FormulaeFn<float> fn
                        ? fn(em, geometryEntity, vars)
                        : float.NaN
                    : defaultValue;
            }
            catch
            {
                EffectiveValue = float.NaN;
            }
            return loadedFnNow || EffectiveValue != oldValue;
        }
    }
}