using Colossal.OdinSerializer.Utilities;
using System.Collections.Generic;
using Unity.Entities;
using static BelzontWE.WEFormulaeHelper;

namespace BelzontWE
{
    public struct WETextDataValueInt
    {
        public int defaultValue;
        private int formulaeStrBnk;
        public bool InitializedEffectiveText { get; private set; }
        public int EffectiveValue { get; private set; }
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
            var result = WEFormulaeHelper.SetFormulae<int>(newFormulae ?? "", out errorFmtArgs, out var value, out var resultFormulaeFn);
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
                    ? WEFormulaeHelper.GetCachedIntFn(Formulae) is FormulaeFn<int> fn
                        ? fn(em, geometryEntity, vars)
                        : int.MinValue
                    : defaultValue;
            }
            catch
            {
                EffectiveValue = int.MinValue;
            }
            return loadedFnNow || EffectiveValue != oldValue;
        }
    }
}