using Belzont.Utils;
using Colossal.OdinSerializer.Utilities;
using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;

namespace BelzontWE
{
    public struct WETextDataValueString : IDisposable
    {
        private GCHandle defaultValueGC;
        private GCHandle formulaeGC;
        public bool InitializedEffectiveText { get; private set; }
        public FixedString512Bytes EffectiveValue { get; private set; }
        private bool loadingFnDone;

        public string Formulae
        {
            get => formulaeGC.IsAllocated ? formulaeGC.Target as string ?? "" : "";
            set
            {
                if (formulaeGC.IsAllocated)
                {
                    if (value == (formulaeGC.Target as string)) return;
                    formulaeGC.Free();
                }
                if (!value.IsNullOrWhitespace()) formulaeGC = GCHandle.Alloc(value);
                loadingFnDone = false;
            }
        }

        public string DefaultValue
        {
            get => defaultValueGC.IsAllocated ? defaultValueGC.Target as string ?? "" : "";
            set
            {
                if (defaultValueGC.IsAllocated)
                {
                    if (value == (defaultValueGC.Target as string)) return;
                    defaultValueGC.Free();
                }
                if (!value.IsNullOrWhitespace()) defaultValueGC = GCHandle.Alloc(value);
            }
        }

        public byte SetFormulae(string newFormulae, out string[] errorFmtArgs)
        {
            if (newFormulae.IsNullOrWhitespace())
            {
                if (formulaeGC.IsAllocated) formulaeGC.Free();
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
                if (formulaeGC.IsAllocated)
                {
                    SetFormulae(Formulae, out _);
                }
                loadedFnNow = loadingFnDone = true;
            }
            try
            {
                EffectiveValue = formulaeGC.IsAllocated
                    ? WEFormulaeHelper.GetCachedStringFn(Formulae) is Func<EntityManager, Entity, string> fn
                        ? (fn(em, geometryEntity)?.ToString().Trim().Truncate(500) ?? "<InvlidFn1>")
                        : "<InvalidFn2>"
                    : DefaultValue;
            }
            catch
            {
                EffectiveValue = "<ERROR>";
            }
            return loadedFnNow || EffectiveValue.ToString() != oldEffText;
        }

        public void Dispose()
        {
            if (formulaeGC.IsAllocated) formulaeGC.Free();
            if (defaultValueGC.IsAllocated) defaultValueGC.Free();
        }
    }
}
