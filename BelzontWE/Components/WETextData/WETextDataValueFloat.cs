using Belzont.Interfaces;
using Belzont.Utils;
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
        {
            InitializedEffectiveText = true;
            var loadedFnNow = false;
            if (!loadingFnDone)
            {
                if (formulaeStrBnk > 0)
                {
                    formulaeCompilationStatus = SetFormulae(Formulae, out _);
                }
                loadedFnNow = loadingFnDone = true;
                runtimeErrorLogged = false;
            }
            var oldValue = EffectiveValue;
            lock (locker)
            {
                try
                {
                    EffectiveValue = formulaeStrBnk > 0
                        ? WEFormulaeHelper.GetCachedFloatFn(Formulae) is FormulaeFn<float> fn
                            ? fn(em, geometryEntity, vars)
                            : float.NaN
                        : defaultValue;
                }
                catch (Exception e)
                {
                    EffectiveValue = float.NaN;
                    if (!runtimeErrorLogged)
                    {
                        runtimeErrorLogged = true;
                        formulaeCompilationStatus = 255;
                        if (BasicIMod.DebugMode) LogUtils.DoLog($"Runtime error in float formulae @{geometryEntity}: {e}");
                    }
                }
            }
            return loadedFnNow || EffectiveValue != oldValue;
        }
    }
}