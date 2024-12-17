using Colossal.OdinSerializer.Utilities;
using System;
using System.Runtime.InteropServices;
using Unity.Entities;

namespace BelzontWE
{
    public struct WETextDataValueFloat : IDisposable
    {
        public float defaultValue;
        private GCHandle formulaeGC;
        public bool InitializedEffectiveText { get; private set; }
        public float EffectiveValue { get; private set; }
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
        public byte SetFormulae(string newFormulae, out string[] errorFmtArgs)
        {
            if (newFormulae.IsNullOrWhitespace())
            {
                if (formulaeGC.IsAllocated) formulaeGC.Free();
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

        public bool UpdateEffectiveValue(EntityManager em, Entity geometryEntity)
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
            var oldValue = EffectiveValue;
            try
            {
                EffectiveValue = formulaeGC.IsAllocated
                    ? WEFormulaeHelper.GetCachedFloatFn(Formulae) is Func<EntityManager, Entity, float> fn
                        ? fn(em, geometryEntity)
                        : float.NaN
                    : defaultValue;
            }
            catch
            {
                EffectiveValue = float.NaN;
            }
            return loadedFnNow || EffectiveValue != oldValue;
        }

        public void Dispose()
        {
            if (formulaeGC.IsAllocated) formulaeGC.Free();
        }
    }
}