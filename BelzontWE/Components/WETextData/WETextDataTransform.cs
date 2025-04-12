using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace BelzontWE
{
    public struct WETextDataTransform : IComponentData
    {
        public float3 offsetPosition;
        public quaternion offsetRotation;
        public float3 scale;
        public WEPlacementPivot pivot;
        public WEZPlacementPivot pivotZ;
        public WEPlacementAlignment alignment;
        public bool useAbsoluteSizeEditing;

        public bool useFormulaeToCheckIfDraw;
        private WETextDataValueFloat mustDrawFn;
        private WETextDataValueInt instanceCount;

        private int nextUpdateFrame;

        public readonly bool MustDraw => mustDrawFn.EffectiveValue > 0;
        public string MustDrawFormulae => mustDrawFn.Formulae;
        internal WETextDataValueFloat MustDrawFn { readonly get => mustDrawFn; set => mustDrawFn = value; }
        public WETextDataValueInt InstanceCountFn { readonly get => instanceCount; set => instanceCount = value; }
        public int DefaultInstanceCount
        {
            readonly get => instanceCount.defaultValue;
            set => instanceCount.defaultValue = value;
        }
        public readonly float3 PivotAsFloat3 => new(pivot switch
        {
            WEPlacementPivot.TopLeft => new float2(0, 0),
            WEPlacementPivot.TopCenter => new float2(.5f, 0),
            WEPlacementPivot.TopRight => new float2(1, 0),
            WEPlacementPivot.MiddleLeft => new float2(0, .5f),
            WEPlacementPivot.MiddleCenter => new float2(.5f, .5f),
            WEPlacementPivot.MiddleRight => new float2(1, .5f),
            WEPlacementPivot.BottomLeft => new float2(0, 1),
            WEPlacementPivot.BottomCenter => new float2(.5f, 1),
            WEPlacementPivot.BottomRight => new float2(1, 1),
            _ => default,
        },
            pivotZ switch
            {
                WEZPlacementPivot.Front => 0,
                WEZPlacementPivot.Middle => .5f,
                WEZPlacementPivot.Back => 1,
                _ => 0,
            });
        public enum ArrayInstancingAxisOrder
        {
            XYZ,
            XZY,
            YXZ,
            YZX,
            ZXY,
            ZYX
        }
        private uint3 arrayInstancingCount;
        public uint3 ArrayInstancing { readonly get => arrayInstancingCount; set => arrayInstancingCount = math.clamp(value, new(1, 1, 1), new(100, 100, 100)); }
        public float3 arrayInstancingGapMeters;
        public ArrayInstancingAxisOrder arrayAxisGrowthOrder;
        internal readonly float3[] SpacingByAxisOrder
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
        internal (WEPlacementAlignment m, WEPlacementAlignment n, WEPlacementAlignment o) AlignmentByAxisOrder
            => arrayAxisGrowthOrder switch
            {
                ArrayInstancingAxisOrder.XYZ => (alignment.GetX(), alignment.GetY(), alignment.GetZ()),
                ArrayInstancingAxisOrder.XZY => (alignment.GetX(), alignment.GetZ(), alignment.GetY()),
                ArrayInstancingAxisOrder.YXZ => (alignment.GetY(), alignment.GetX(), alignment.GetZ()),
                ArrayInstancingAxisOrder.YZX => (alignment.GetY(), alignment.GetZ(), alignment.GetX()),
                ArrayInstancingAxisOrder.ZXY => (alignment.GetZ(), alignment.GetX(), alignment.GetY()),
                ArrayInstancingAxisOrder.ZYX => (alignment.GetZ(), alignment.GetY(), alignment.GetX()),
                _ => default,
            };

        public uint3 AxisOrderToXYZ(uint3 axisOrder)
        {
            return arrayAxisGrowthOrder switch
            {
                ArrayInstancingAxisOrder.XYZ => arrayInstancingCount,
                ArrayInstancingAxisOrder.XZY => arrayInstancingCount.xzy,
                ArrayInstancingAxisOrder.YXZ => arrayInstancingCount.yxz,
                ArrayInstancingAxisOrder.YZX => arrayInstancingCount.zxy,
                ArrayInstancingAxisOrder.ZXY => arrayInstancingCount.yzx,
                ArrayInstancingAxisOrder.ZYX => arrayInstancingCount.zyx,
                _ => default,
            };
        }



        public int SetFormulaeMustDraw(string value, out string[] cmpErr) => mustDrawFn.SetFormulae(value, out cmpErr);
        public int SetFormulaeInstanceCount(string value, out string[] cmpErr) => instanceCount.SetFormulae(value, out cmpErr);

        public bool UpdateFormulaes(EntityManager em, Entity geometryEntity, string varsStr, bool updateCounter)
        {
            if ((!updateCounter && !useFormulaeToCheckIfDraw) || nextUpdateFrame > Time.frameCount)
            {
                return false;
            }
            nextUpdateFrame = Time.frameCount + WEModData.InstanceWE.FramesCheckUpdateVal;

            var vars = WEVarsCacheBank.Instance[WEVarsCacheBank.Instance[varsStr]];
            var changed = (useFormulaeToCheckIfDraw && mustDrawFn.UpdateEffectiveValue(em, geometryEntity, vars));
            if (updateCounter)
            {
                changed |= instanceCount.UpdateEffectiveValue(em, geometryEntity, vars);
            }
            return changed;
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
                },
                arrayInstancingCount = new(1, 1, 1),
                instanceCount = new()
                {
                    defaultValue = -1
                },
                pivotZ = WEZPlacementPivot.Middle
            };

    }
}