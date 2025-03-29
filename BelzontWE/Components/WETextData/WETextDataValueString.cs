using Belzont.Interfaces;
using Belzont.Utils;
using Colossal.OdinSerializer.Utilities;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using static BelzontWE.WEFormulaeHelper;

namespace BelzontWE
{
    public struct WETextDataValueString
    {
        private int defaultValueStrBnk;
        private int formulaeStrBnk;
        public bool InitializedEffectiveText { get; private set; }
        public FixedString512Bytes EffectiveValue { get; private set; }
        private bool loadingFnDone;

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
            var result = WEFormulaeHelper.SetFormulae<string>(newFormulae ?? "", out errorFmtArgs, out var value, out var resultFormulaeFn);
            if (result == 0)
            {
                Formulae = value;
            }
            return result;
        }

        public bool UpdateEffectiveValue(EntityManager em, Entity geometryEntity, Dictionary<string, string> vars)
        {
            return UpdateEffectiveValue(em, geometryEntity, EffectiveValue.ToString(), vars);
        }
        public bool UpdateEffectiveValue(EntityManager em, Entity geometryEntity, string oldEffText, Dictionary<string, string> vars)
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
            try
            {
                EffectiveValue = formulaeStrBnk > 0
                    ? WEFormulaeHelper.GetCachedStringFn(Formulae) is FormulaeFn<string> fn
                        ? (fn(em, geometryEntity, vars)?.ToString().Trim().Truncate(500) ?? "<InvlidFn1>")
                        : "<InvalidFn2>"
                    : DefaultValue;
            }
            catch (Exception e)
            {
                if (BasicIMod.VerboseMode)
                {
                    LogUtils.DoWarnLog($"ERROR LOADING VALUE AT ENTITY! {geometryEntity} old = {oldEffText}\n{e}");
                }
                EffectiveValue = "<ERROR>";
            }
            return loadedFnNow || EffectiveValue.ToString() != oldEffText;
        }
    }
}
