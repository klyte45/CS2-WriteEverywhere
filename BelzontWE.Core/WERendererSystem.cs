using BelzontWE.Font.Utility;
using Game.Common;
using Game.Rendering;
using Game.Tools;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
#if BURST
using UnityEngine.Scripting;
using Unity.Burst;
#endif

namespace BelzontWE
{
    public partial class WERendererSystem : SystemBase
    {
        private EntityQuery m_renderInterpolatedQueueEntities;
        private EntityQuery m_renderQueueEntities;
        private CameraUpdateSystem m_CameraUpdateSystem;
        private RenderingSystem m_RenderingSystem;
        private NativeQueue<WERenderData> availToDraw = new(Allocator.Persistent);
#if BURST
        [Preserve]
#endif
        protected override void OnCreate()
        {
            base.OnCreate();

            m_CameraUpdateSystem = World.GetExistingSystemManaged<CameraUpdateSystem>();
            m_RenderingSystem = World.GetExistingSystemManaged<RenderingSystem>();
            m_renderInterpolatedQueueEntities = GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<CullingInfo>(),
                        ComponentType.ReadOnly<InterpolatedTransform>(),
                        ComponentType.ReadWrite<WEWaitingRenderingComponent>(),
                        ComponentType.ReadWrite<WESimulationTextComponent>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Deleted>(),
                    }
                }
            });
            m_renderQueueEntities = GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<CullingInfo>(),
                        ComponentType.ReadOnly<Game.Objects.Transform>(),
                        ComponentType.ReadWrite<WEWaitingRenderingComponent>(),
                        ComponentType.ReadWrite<WESimulationTextComponent>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<InterpolatedTransform>(),
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Deleted>(),
                    }
                }
            });


            RequireAnyForUpdate(m_renderQueueEntities, m_renderInterpolatedQueueEntities);
        }
#if BURST
        [Preserve]
#endif
        protected override void OnUpdate()
        {
            float4 m_LodParameters = 1f;
            float3 m_CameraPosition = 0f;
            float3 m_CameraDirection = 0f;

            if (m_CameraUpdateSystem.TryGetLODParameters(out LODParameters LodParametersStr))
            {
                IGameCameraController activeCameraController = m_CameraUpdateSystem.activeCameraController;
                m_LodParameters = RenderingUtils.CalculateLodParameters(GetLevelOfDetail(m_RenderingSystem.frameLod, activeCameraController), LodParametersStr);
                m_CameraPosition = LodParametersStr.cameraPosition;
                m_CameraDirection = m_CameraUpdateSystem.activeViewer.forward;
            }
            CheckInterpolated(m_LodParameters, m_CameraPosition, m_CameraDirection);
            CheckRenderQueue(m_LodParameters, m_CameraPosition, m_CameraDirection);
            Render_Impl(m_CameraUpdateSystem.activeCamera);

        }

#if BURST
        [Preserve]
#endif
        protected override void OnDestroy()
        {
            availToDraw.Dispose();
            base.OnDestroy();
        }
#if BURST
        [Preserve]
#endif

        private void Render_Impl(Camera camera)
        {
            if (!m_renderQueueEntities.IsEmptyIgnoreFilter || !m_renderInterpolatedQueueEntities.IsEmptyIgnoreFilter)
            {
                while (availToDraw.TryDequeue(out var item))
                {
                    if (item.weComponent.basicRenderInformation.Target is not BasicRenderInformation bri)
                    {
                        item.weComponent.basicRenderInformation.Free();
                        item.weComponent.basicRenderInformation = default;
                        continue;
                    }
                    Graphics.DrawMesh(bri.m_mesh, item.transformMatrix, bri.m_generatedMaterial, 0, null, 0, item.weComponent.MaterialProperties);
                }
            }
        }

#if BURST
        [BurstCompile]
#endif
        private void CheckRenderQueue(float4 m_LodParameters, float3 m_CameraPosition, float3 m_CameraDirection)
        {
            if (!m_renderQueueEntities.IsEmptyIgnoreFilter)
            {
                var job2 = new WERenderingJob
                {
                    m_EntityType = GetEntityTypeHandle(),
                    m_cullingInfo = GetComponentTypeHandle<CullingInfo>(true),
                    m_transform = GetComponentTypeHandle<Game.Objects.Transform>(true),
                    m_weData = GetBufferTypeHandle<WESimulationTextComponent>(false),
                    m_weDataPendingLookup = GetBufferLookup<WEWaitingRenderingComponent>(false),
                    m_weDataPending = GetBufferTypeHandle<WEWaitingRenderingComponent>(false),
                    m_LodParameters = m_LodParameters,
                    m_CameraPosition = m_CameraPosition,
                    m_CameraDirection = m_CameraDirection,
                    availToDraw = availToDraw,
                    isInterpolated = false
                };
                job2.Schedule(m_renderQueueEntities, Dependency).Complete();
            }
        }

#if BURST
        [BurstCompile]
#endif
        private void CheckInterpolated(float4 m_LodParameters, float3 m_CameraPosition, float3 m_CameraDirection)
        {
            if (!m_renderInterpolatedQueueEntities.IsEmptyIgnoreFilter)
            {
                var job = new WERenderingJob
                {
                    m_EntityType = GetEntityTypeHandle(),
                    m_cullingInfo = GetComponentTypeHandle<CullingInfo>(true),
                    m_iTransform = GetComponentTypeHandle<InterpolatedTransform>(true),
                    m_weData = GetBufferTypeHandle<WESimulationTextComponent>(false),
                    m_weDataPendingLookup = GetBufferLookup<WEWaitingRenderingComponent>(false),
                    m_weDataPending = GetBufferTypeHandle<WEWaitingRenderingComponent>(false),
                    m_LodParameters = m_LodParameters,
                    m_CameraPosition = m_CameraPosition,
                    m_CameraDirection = m_CameraDirection,
                    availToDraw = availToDraw,
                    isInterpolated = true
                };
                job.Schedule(m_renderInterpolatedQueueEntities, Dependency).Complete();
            }
        }

        private struct WERenderData
        {
            public WESimulationTextComponent weComponent;
            public Matrix4x4 transformMatrix;
        }
#if BURST
        [Preserve]
#endif
        public float GetLevelOfDetail(float levelOfDetail, IGameCameraController cameraController)
        {
            if (cameraController != null)
            {
                levelOfDetail *= 1f - 1f / (2f + 0.01f * cameraController.zoom);
            }
            return levelOfDetail;
        }
#if BURST
        [BurstCompile]
#endif
        private struct WERenderingJob : IJobChunk
        {
            public ComponentTypeHandle<CullingInfo> m_cullingInfo;
            public BufferTypeHandle<WESimulationTextComponent> m_weData;
            public BufferTypeHandle<WEWaitingRenderingComponent> m_weDataPending;
            public ComponentTypeHandle<InterpolatedTransform> m_iTransform;
            public float4 m_LodParameters;
            public float3 m_CameraPosition;
            public float3 m_CameraDirection;
            public EntityTypeHandle m_EntityType;
            public NativeQueue<WERenderData> availToDraw;
            public BufferLookup<WEWaitingRenderingComponent> m_weDataPendingLookup;
            internal ComponentTypeHandle<Game.Objects.Transform> m_transform;
            public bool isInterpolated;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Game.Objects.Transform> transforms = default;
                NativeArray<InterpolatedTransform> i_transforms = default;


                var entities = chunk.GetNativeArray(m_EntityType);
                var cullings = chunk.GetNativeArray(ref m_cullingInfo);
                if (isInterpolated)
                {
                    i_transforms = chunk.GetNativeArray(ref m_iTransform);
                }
                else
                {
                    transforms = chunk.GetNativeArray(ref m_transform);
                }
                var weDatas = chunk.GetBufferAccessor(ref m_weData);
                var weDataPendings = chunk.GetBufferAccessor(ref m_weDataPending);

                for (int i = 0; i < entities.Length; i++)
                {
                    var cullInfo = cullings[i];
                    var weCustomData = weDatas[i];
                    var wePending = weDataPendings[i];

                    float3 positionRef;
                    quaternion rotationRef;

                    if (isInterpolated)
                    {
                        var transform = i_transforms[i];
                        positionRef = transform.m_Position;
                        rotationRef = transform.m_Rotation;
                    }
                    else
                    {
                        var transform = transforms[i];
                        positionRef = transform.m_Position;
                        rotationRef = transform.m_Rotation;
                    }

                    for (int j = 0; j < weCustomData.Length; j++)
                    {
                        if (!weCustomData[j].basicRenderInformation.IsAllocated)
                        {
                            wePending.Add(WEWaitingRenderingComponent.From(weCustomData[j]));
                            weCustomData.RemoveAt(j);
                            j--;
                            continue;
                        }

                        float minDist = RenderingUtils.CalculateMinDistance(cullInfo.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters);
                        int num7 = RenderingUtils.CalculateLod(minDist * minDist, m_LodParameters);
                        if (num7 >= cullInfo.m_MinLod)
                        {
                            availToDraw.Enqueue(new WERenderData
                            {
                                weComponent = weCustomData[j],
                                transformMatrix = Matrix4x4.TRS(positionRef, rotationRef, Vector3.one) * Matrix4x4.TRS(weCustomData[j].offsetPosition, weCustomData[j].offsetRotation, weCustomData[j].scale * weCustomData[j].BriOffsetScaleX / weCustomData[j].BriPixelDensity)
                            });
                        }
                    }
                }
            }
        }
    }

}