using System;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace BelzontWE
{
    public struct WETextDataTransform : IComponentData, IDisposable, ICleanupComponentData
    {
        public float3 offsetPosition;
        public quaternion offsetRotation;
        public float3 scale;
        public WEPlacementPivot pivot;
        public bool useAbsoluteSizeEditing;

        public bool useFormulaeToCheckIfDraw;
        private WETextDataValueFloat mustDrawFn;

        private int nextUpdateFrame;

        public readonly bool MustDraw => mustDrawFn.EffectiveValue > 0;
        public string MustDrawFormulae => mustDrawFn.Formulae;
        internal WETextDataValueFloat MustDrawFn { readonly get => mustDrawFn; set => mustDrawFn = value; }
        public int SetFormulaeMustDraw(string value, out string[] cmpErr) => mustDrawFn.SetFormulae(value, out cmpErr);

        public bool UpdateFormulaes(EntityManager em, Entity geometryEntity, string varsStr)
        {
            if (!useFormulaeToCheckIfDraw || nextUpdateFrame > Time.frameCount)
            {
                return false;
            }
            nextUpdateFrame = Time.frameCount + WEModData.InstanceWE.FramesCheckUpdateVal;
            var vars = varsStr.Split(WERendererSystem.VARIABLE_ITEM_SEPARATOR).Select(x => x.Split(WERendererSystem.VARIABLE_KV_SEPARATOR, 2))
                        .Where(x => x.Length == 2).GroupBy(x => x[0]).ToDictionary(x => x.Key, x => x.Last()[1]);
            return mustDrawFn.UpdateEffectiveValue(em, geometryEntity, vars);
        }

        public static WETextDataTransform CreateDefault(Entity target, Entity? parent = null)
            => new()
            {
                offsetPosition = new(0, 0, 0),
                offsetRotation = new(),
                scale = new(1, 1, 1),
                pivot = WEPlacementPivot.MiddleCenter,
                mustDrawFn = new WETextDataValueFloat
                {
                    defaultValue = 1
                }
            };

        public void Dispose()
        {
            mustDrawFn.Dispose();
        }
    }
}