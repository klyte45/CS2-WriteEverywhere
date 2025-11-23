using Colossal.OdinSerializer.Utilities;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static BelzontWE.WEFormulaeHelper;

namespace BelzontWE
{
    public struct WETextDataValueFloat3
    {
        public float3 defaultValue;
        private int formulaeStrBnk;
        public bool InitializedEffectiveText { get; private set; }
        public float3 EffectiveValue { get; private set; }
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
            var result = SetFormulae<float3>(newFormulae ?? "", out errorFmtArgs, out var value, out var resultFormulaeFn);
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
                    ? GetCachedFloat3Fn(Formulae) is FormulaeFn<float3> fn
                        ? fn(em, geometryEntity, vars)
                        : new float3(float.NaN, float.NaN, float.NaN)
                    : defaultValue;
            }
            catch
            {
                EffectiveValue = new float3(float.NaN, float.NaN, float.NaN);
            }
            return loadedFnNow || (Vector3)EffectiveValue != (Vector3)oldValue;
        }
    }
}
//"System.Reflection.TargetInvocationException: Exception has been thrown by the target of an invocation. --->
//System.InvalidProgramException: Invalid IL code in (wrapper dynamic-method) MonoMod.Utils.DynamicMethodDefinition:__WE_CS2_float3_formulae_Game_Objects_Transform_m_Position (Unity.Entities.EntityManager,Unity.Entities.Entity,System.Collections.Generic.Dictionary`2<string, string>):
//IL_001f: call      0x00000003\n\n\r\n  at (wrapper managed-to-native) System.Object.__icall_wrapper_mono_gc_wbarrier_generic_nostore_internal(intptr)\r\n  at (wrapper write-barrier) System.Object.wbarrier_conc(intptr)\r\n  at (wrapper managed-to-native) System.Reflection.RuntimeMethodInfo.InternalInvoke(System.Reflection.RuntimeMethodInfo,object,object[],System.Exception&)\r\n  at System.Reflection.RuntimeMethodInfo.Invoke (System.Object obj, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder binder, System.Object[] parameters, System.Globalization.CultureInfo culture) [0x0006a] in <356d430fdcb04bbf8dc54776e1d3627f>:0 \r\n   --- End of inner exception stack trace ---\r\n  at System.Reflection.RuntimeMethodInfo.Invoke (System.Object obj, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder binder, System.Object[] parameters, System.Globalization.CultureInfo culture) [0x00083] in <356d430fdcb04bbf8dc54776e1d3627f>:0 \r\n  at System.Reflection.Emit.DynamicMethod.Invoke (System.Object obj, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder binder, System.Object[] parameters, System.Globalization.CultureInfo culture) [0x00025] in <356d430fdcb04bbf8dc54776e1d3627f>:0 \r\n  at System.Reflection.MethodBase.Invoke (System.Object obj, System.Object[] parameters) [0x00000] in <356d430fdcb04bbf8dc54776e1d3627f>:0 \r\n  at BelzontWE.WEFormulaeHelper+<>c__DisplayClass20_0`1[T].<SetFormulae>b__2 (Unity.Entities.EntityManager x, Unity.Entities.Entity e, System.Collections.Generic.Dictionary`2[TKey,TValue] d) [0x00000] in V:\\GameModding\\Cities Skylines\\CodedMods\\Belzont\\BelzontWE\\BelzontWE\\Utils\\WEFormulaeHelper.cs:262 \r\n  at (wrapper delegate-invoke) BelzontWE.WEFormulaeHelper+FormulaeFn`1[Unity.Mathematics.float3].invoke_T_EntityManager_Entity_Dictionary`2<string, string>(Unity.Entities.EntityManager,Unity.Entities.Entity,System.Collections.Generic.Dictionary`2<string, string>)\r\n  at BelzontWE.WETextDataValueFloat3.UpdateEffectiveValue (Unity.Entities.EntityManager em, Unity.Entities.Entity geometryEntity, System.Collections.Generic.Dictionary`2[TKey,TValue] vars) [0x0004d]
//in V:\\GameModding\\Cities Skylines\\CodedMods\\Belzont\\BelzontWE\\BelzontWE\\Components\\WETextData\\WETextDataValueFloat3.cs:58 "