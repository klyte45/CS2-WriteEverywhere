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
        public readonly float2 PivotAsFloat2 => pivot switch
        {
            WEPlacementPivot.TopLeft => new float2(0, 0),
            WEPlacementPivot.TopCenter => new float2(.5f, 0),
            WEPlacementPivot.TopRight => new float2(0, 1),
            WEPlacementPivot.MiddleLeft => new float2(0, .5f),
            WEPlacementPivot.MiddleCenter => new float2(.5f, .5f),
            WEPlacementPivot.MiddleRight => new float2(1, .5f),
            WEPlacementPivot.BottomLeft => new float2(0, 1),
            WEPlacementPivot.BottomCenter => new float2(.5f, 1),
            WEPlacementPivot.BottomRight => new float2(1, 1),
            _ => default,
        };
        public enum ArrayInstancingAxisOrder
        {
            XYZ,
            XZY,
            YXZ,
            YZX,
            ZXY,
            ZYX
        }
        public uint3 ArrayInstancing { readonly get => arrayInstancingCount; set => arrayInstancingCount = math.clamp(value, new(1, 1, 1), new(100, 100, 100)); }
        private uint3 arrayInstancingCount;
        public float3 arrayInstancingGapMeters;
        public ArrayInstancingAxisOrder arrayAxisGrowthOrder; internal readonly float3[] SpacingByAxisOrder
            => arrayAxisGrowthOrder switch
            {
                ArrayInstancingAxisOrder.XYZ => new float3[]
                    {
                        new(arrayInstancingGapMeters.x, 0, 0),
                        new(0, arrayInstancingGapMeters.y, 0),
                        new(0, 0, arrayInstancingGapMeters.z),
                    },
                ArrayInstancingAxisOrder.XZY => new float3[]
                    {
                        new(arrayInstancingGapMeters.x, 0, 0),
                        new(0, 0, arrayInstancingGapMeters.z),
                        new(0, arrayInstancingGapMeters.y, 0),
                    },
                ArrayInstancingAxisOrder.YXZ => new float3[]
                    {
                        new(0, arrayInstancingGapMeters.y, 0),
                        new(arrayInstancingGapMeters.x, 0, 0),
                        new(0, 0, arrayInstancingGapMeters.z),
                    },
                ArrayInstancingAxisOrder.YZX => new float3[]
                    {
                        new(0, arrayInstancingGapMeters.y, 0),
                        new(0, 0, arrayInstancingGapMeters.z),
                        new(arrayInstancingGapMeters.x, 0, 0),
                    },
                ArrayInstancingAxisOrder.ZXY => new float3[]
                    {
                        new(0, 0, arrayInstancingGapMeters.z),
                        new(arrayInstancingGapMeters.x, 0, 0),
                        new(0, arrayInstancingGapMeters.y, 0),
                    },
                ArrayInstancingAxisOrder.ZYX => new float3[]
                    {
                        new(0, 0, arrayInstancingGapMeters.z),
                        new(0, arrayInstancingGapMeters.y, 0),
                        new(arrayInstancingGapMeters.x, 0, 0),
                    },
                _ => null,
            };


        internal readonly uint3 InstanceCountByAxisOrder
            => arrayAxisGrowthOrder switch
            {
                ArrayInstancingAxisOrder.XYZ => arrayInstancingCount,
                ArrayInstancingAxisOrder.XZY => arrayInstancingCount.xzy,
                ArrayInstancingAxisOrder.YXZ => arrayInstancingCount.yxz,
                ArrayInstancingAxisOrder.YZX => arrayInstancingCount.yzx,
                ArrayInstancingAxisOrder.ZXY => arrayInstancingCount.zxy,
                ArrayInstancingAxisOrder.ZYX => arrayInstancingCount.zyx,
                _ => default,
            };



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