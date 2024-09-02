using Belzont.Utils;
using System;
using Unity.Collections;
using Unity.Entities;

namespace BelzontWE
{
    public partial struct WETextData
    {
        private struct WETextDataValueString
        {
            public FixedString512Bytes formulaeHandlerStr;
            public bool loadingFnDone;
            public FixedString512Bytes m_text;
            public readonly Func<EntityManager, Entity, string> FormulaeFn => WEFormulaeHelper.GetCached(formulaeHandlerStr);
            public bool InitializedEffectiveText { get; private set; }
            public FixedString512Bytes EffectiveText { get; set; }

            public byte SetFormulae(string newFormulae, out string[] errorFmtArgs)
                => WEFormulaeHelper.SetFormulae(newFormulae ?? "", out errorFmtArgs, out formulaeHandlerStr, out var resultFormulaeFn);

            public bool UpdateEffectiveText(EntityManager em, Entity geometryEntity, string oldEffText)
            {
                InitializedEffectiveText = true;
                var loadedFnNow = false;
                if (!loadingFnDone)
                {
                    if (formulaeHandlerStr.Length > 0)
                    {
                        SetFormulae(formulaeHandlerStr.ToString(), out _);
                    }
                    loadedFnNow = loadingFnDone = true;
                }
                EffectiveText = FormulaeFn is Func<EntityManager, Entity, string> fn
                    ? fn(em, geometryEntity)?.ToString().Trim().Truncate(500) ?? "<InvlidFn>"
                    : formulaeHandlerStr.Length > 0 ? "<InvalidFn>" : m_text;
                return loadedFnNow || EffectiveText.ToString() != oldEffText;
            }
        }

    }
}