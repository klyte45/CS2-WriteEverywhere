﻿using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace BelzontWE
{
    public struct WETextDataValueColor
    {
        public Color defaultValue;
        public FixedString512Bytes formulaeStr;
        public readonly Func<EntityManager, Entity, Color> FormulaeFn => WEFormulaeHelper.GetCachedColorFn(formulaeStr);
        public bool InitializedEffectiveText { get; private set; }
        public Color EffectiveValue { get; private set; }
        private bool loadingFnDone;

        public byte SetFormulae(string newFormulae, out string[] errorFmtArgs)
            => WEFormulaeHelper.SetFormulae<Color>(newFormulae ?? "", out errorFmtArgs, out formulaeStr, out var resultFormulaeFn);

        public bool UpdateEffectiveValue(EntityManager em, Entity geometryEntity)
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
            var oldVal = EffectiveValue;
            EffectiveValue = FormulaeFn is Func<EntityManager, Entity, Color> fn
                ? fn(em, geometryEntity) : formulaeStr.Length > 0 ? UnityEngine.Color.cyan : defaultValue;
            return loadedFnNow || EffectiveValue != oldVal;
        }
    }
}