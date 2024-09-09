using Belzont.Utils;
using System;
using Unity.Collections;
using Unity.Entities;

namespace BelzontWE
{
    public struct WETextDataValueString
    {
        public FixedString512Bytes defaultValue;
        public FixedString512Bytes formulaeStr;
        public readonly Func<EntityManager, Entity, string> FormulaeFn => WEFormulaeHelper.GetCachedStringFn(formulaeStr);
        public bool InitializedEffectiveText { get; private set; }
        public FixedString512Bytes EffectiveValue { get; private set; }
        private bool loadingFnDone;

        public byte SetFormulae(string newFormulae, out string[] errorFmtArgs)
            => WEFormulaeHelper.SetFormulae<string>(newFormulae ?? "", out errorFmtArgs, out formulaeStr, out var resultFormulaeFn);
        public bool UpdateEffectiveValue(EntityManager em, Entity geometryEntity)
        {
            return UpdateEffectiveValue(em, geometryEntity, EffectiveValue.ToString());
        }
        public bool UpdateEffectiveValue(EntityManager em, Entity geometryEntity, string oldEffText)
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
            try
            {
                EffectiveValue = FormulaeFn is Func<EntityManager, Entity, string> fn
                    ? fn(em, geometryEntity)?.ToString().Trim().Truncate(500) ?? "<InvlidFn>"
                    : formulaeStr.Length > 0 ? "<InvalidFn>" : defaultValue;
            }
            catch
            {
                EffectiveValue = "<ERROR>";
            }
            return loadedFnNow || EffectiveValue.ToString() != oldEffText;
        }
    }
}
