using System;
using Unity.Collections;
using Unity.Entities;

namespace BelzontWE
{
    public partial struct WETextData
    {
        private struct WETextDataValueFloat
        {
            public float defaultValue;
            public FixedString512Bytes formulaeStr;
            public readonly Func<EntityManager, Entity, float> FormulaeFn => WEFormulaeHelper.GetCachedFloatFn(formulaeStr);
            public bool InitializedEffectiveText { get; private set; }
            public float EffectiveValue { get; private set; }
            private bool loadingFnDone;

            public byte SetFormulae(string newFormulae, out string[] errorFmtArgs)
                => WEFormulaeHelper.SetFormulae<float>(newFormulae ?? "", out errorFmtArgs, out formulaeStr, out var resultFormulaeFn);

            public bool UpdateEffectiveText(EntityManager em, Entity geometryEntity, string oldEffText)
            {
                InitializedEffectiveText = true;
                var loadedFnNow = false;
                if (!loadingFnDone)
                {
                    if (formulaeStr.Length > 0)
                    {
                        SetFormulae(formulaeStr.ToString(), out _);
                    }
                    loadedFnNow = loadingFnDone = true;
                }
                EffectiveValue = FormulaeFn is Func<EntityManager, Entity, float> fn
                    ? fn(em, geometryEntity) : formulaeStr.Length > 0 ? float.NaN : defaultValue;
                return loadedFnNow || EffectiveValue.ToString() != oldEffText;
            }

            public WETextDataStruct.WETextDataStyleStructFormulaeFloat ToDataStruct() => new()
            {
                defaultValue = defaultValue,
                formulae = formulaeStr
            };

            public static WETextDataValueFloat FromStruct(WETextDataStruct.WETextDataStyleStructFormulaeFloat dataStruct) => new()
            {
                defaultValue = dataStruct.defaultValue,
                formulaeStr = dataStruct.formulae
            };
        }

    }
}