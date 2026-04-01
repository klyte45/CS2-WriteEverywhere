using Colossal.Mathematics;
using Game.City;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace BelzontWE.Builtin
{
    [WEBuiltinFunction("Attached")]
    public static class WEAttachedFn
    {
        private static CityConfigurationSystem system;
        private static CityConfigurationSystem System => system ??= World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<CityConfigurationSystem>();

        [WEFormula(typeof(float3))]
        public static float3 GetOffsetForNearestSidewalk(Entity e)
        {
            var outputRef = new NativeReference<float3>(Allocator.TempJob);
            var job = new WESidewalkGetterJob
            {
                m_attachedLookup = System.GetComponentLookup<Attached>(true),
                m_transformLookup = System.GetComponentLookup<Transform>(true),
                m_compositionLookup = System.GetComponentLookup<Composition>(true),
                m_netCompositionLookup = System.GetComponentLookup<NetCompositionData>(true),
                m_curveLookup = System.GetComponentLookup<Curve>(true),
                srcEntity = e,
                isRhw = System.leftHandTraffic,
                output = outputRef,
                m_laneBufferLookup = System.GetBufferLookup<NetCompositionLane>(true),                
            };
            job.Schedule().Complete();

            var result = outputRef.Value;
            outputRef.Dispose();
            return result;
        }

        [BurstCompile]
        private struct WESidewalkGetterJob : IJob
        {
            public NativeReference<float3> output;
            public Entity srcEntity;
            public ComponentLookup<Attached> m_attachedLookup;
            public ComponentLookup<Transform> m_transformLookup;
            public ComponentLookup<Composition> m_compositionLookup;
            public ComponentLookup<NetCompositionData> m_netCompositionLookup;
            public ComponentLookup<Curve> m_curveLookup;
            public BufferLookup<NetCompositionLane> m_laneBufferLookup;
            public bool isRhw;

            public void Execute()
            {
                if (!m_attachedLookup.TryGetComponent(srcEntity, out var attached)
                    || !m_transformLookup.TryGetComponent(srcEntity, out var transform)
                    || !m_curveLookup.TryGetComponent(attached.m_Parent, out var curve)
                    || !m_compositionLookup.TryGetComponent(attached.m_Parent, out Composition composition)
                    || !m_netCompositionLookup.TryGetComponent(composition.m_Edge, out var netComposition)
                    )
                {
                    return;
                }
                float3 origin = transform.m_Position;
                quaternion originOrientation = transform.m_Rotation;
                float3 targetCurvePosition = MathUtils.Position(curve.m_Bezier, attached.m_CurvePosition);


                // Tangent along the curve at the attachment point, projected onto XZ plane
                float3 curveTangent = MathUtils.Tangent(curve.m_Bezier, attached.m_CurvePosition);
                float3 tangentXZ = math.normalize(new float3(curveTangent.x, 0f, curveTangent.z));
                // Perpendicular to the curve in the XZ plane (rotate tangent 90° around Y)
                float3 perpXZ = new float3(-tangentXZ.z, 0f, tangentXZ.x);
                float3 nearestSidewalk = default;
                if ((netComposition.m_State & CompositionState.HasPedestrianLanes) != 0 && m_laneBufferLookup.TryGetBuffer(composition.m_Edge, out var laneBuff) && !laneBuff.IsEmpty)
                {
                    var minDist = float.MaxValue;
                    // Build curve-local axes: X = left perp, Y = up, Z = forward along tangent
                    var laneAxisX = perpXZ;                        // lateral (left = positive X in lane space)
                    var laneAxisY = new float3(0f, 1f, 0f);        // height
                    var laneAxisZ = tangentXZ;                     // longitudinal (along curve)
                    for (int i = 0; i < laneBuff.Length; i++)
                    {
                        var lane = laneBuff[i];
                        if ((lane.m_Flags & LaneFlags.Pedestrian) != 0)
                        {
                            // Transform lane.m_Position from road-local space to world space
                            var refPos = targetCurvePosition
                                + laneAxisX * lane.m_Position.x
                                + laneAxisY * lane.m_Position.y
                                + laneAxisZ * lane.m_Position.z;
                            var dist = math.distance(refPos, origin);
                            if (dist < minDist)
                            {
                                minDist = dist;
                                nearestSidewalk = refPos;
                            }
                        }
                    }
                }
                else
                {
                    float width = netComposition.m_Width + 1;

                    // candidate1 = left side, candidate2 = right side (perpXZ points left of tangent)
                    float3 candidate1 = targetCurvePosition + perpXZ * (width * 0.5f);
                    float3 candidate2 = targetCurvePosition - perpXZ * (width * 0.5f);

                    // RHW = true → left sidewalk; RHW = false → right sidewalk
                    nearestSidewalk = isRhw ? candidate1 : candidate2;
                }
                // Convert the world-space delta into the entity's local orientation
                float3 worldDelta = nearestSidewalk - origin;
                output.Value = math.rotate(math.inverse(originOrientation), worldDelta);

            }
        }
    }
}