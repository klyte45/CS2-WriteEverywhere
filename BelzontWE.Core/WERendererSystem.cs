#define BURST
//#define VERBOSE 
using BelzontWE.Font.Utility;
using Colossal.Entities;
using Game;
using Game.Common;
using Game.Rendering;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

namespace BelzontWE
{
    public partial class WERendererSystem : SystemBase
    {
        private FontServer m_FontServer;
        private EntityQuery m_renderInterpolatedQueueEntities;
        private EntityQuery m_renderQueueEntities;
        private EntityQuery m_noWaitQueueEntities;
        private EntityQuery m_pendingQueueEntities;
        private CameraUpdateSystem m_CameraUpdateSystem;
        private RenderingSystem m_RenderingSystem;
        private EndFrameBarrier m_endFrameBarrier;

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();

            m_FontServer = World.GetOrCreateSystemManaged<FontServer>();
            m_CameraUpdateSystem = World.GetExistingSystemManaged<CameraUpdateSystem>();
            m_RenderingSystem = World.GetExistingSystemManaged<RenderingSystem>();
            m_endFrameBarrier = World.GetExistingSystemManaged<EndFrameBarrier>();
            m_renderInterpolatedQueueEntities = GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<CullingInfo>(),
                        ComponentType.ReadOnly<InterpolatedTransform>(),
                        ComponentType.ReadWrite<WEWaitingRenderingComponent>(),
                        ComponentType.ReadWrite<WECustomComponent>(),
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
                        ComponentType.ReadWrite<WECustomComponent>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<InterpolatedTransform>(),
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Deleted>(),
                    }
                }
            });
            m_noWaitQueueEntities = GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<CullingInfo>(),
                        ComponentType.ReadWrite<WECustomComponent>(),
                    },
                    Any =new ComponentType[]
                    {
                        ComponentType.ReadOnly<InterpolatedTransform>(),
                        ComponentType.ReadOnly<Game.Objects.Transform>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadWrite<WEWaitingRenderingComponent>(),
                        ComponentType.ReadOnly<Deleted>(),
                    }
                }
            });
            m_pendingQueueEntities = GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<CullingInfo>(),
                        ComponentType.ReadWrite<WEWaitingRenderingComponent>(),
                    },
                    Any =new ComponentType[]
                    {
                        ComponentType.ReadOnly<InterpolatedTransform>(),
                        ComponentType.ReadOnly<Game.Objects.Transform>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Deleted>(),
                    }
                }
            });
        }
        [Preserve]
        protected override void OnUpdate()
        {
            if (!m_pendingQueueEntities.IsEmptyIgnoreFilter)
            {
                NativeArray<Entity> entities = m_pendingQueueEntities.ToEntityArray(Allocator.TempJob);
                var barrier = m_endFrameBarrier.CreateCommandBuffer();
                for (var i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    var weCustomDataPending = EntityManager.GetBuffer<WEWaitingRenderingComponent>(entity);
                    if (!EntityManager.TryGetBuffer<WECustomComponent>(entity, true, out var wePersistentData))
                    {
                        wePersistentData = barrier.AddBuffer<WECustomComponent>(entity);
                    }

                    for (var j = 0; j < weCustomDataPending.Length; j++)
                    {
                        var weCustomData = weCustomDataPending[j];
                        var font = m_FontServer[weCustomData.fontName.ToString()];
                        if (font is null)
                        {
                            continue;
                        }
                        var bri = m_FontServer[weCustomData.fontName.ToString()].DrawString(weCustomData.text.ToString(), default);
                        if (bri == null)
                        {
                            continue;
                        }
                        wePersistentData.Add(WECustomComponent.From(weCustomData, font, bri));
                        weCustomDataPending.RemoveAt(j);
                        j--;
                    }
                }
                entities.Dispose();
            }
            if (!m_noWaitQueueEntities.IsEmptyIgnoreFilter)
            {
                NativeArray<Entity> entities = m_noWaitQueueEntities.ToEntityArray(Allocator.TempJob);
                var barrier = m_endFrameBarrier.CreateCommandBuffer();
                for (var i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    barrier.AddBuffer<WEWaitingRenderingComponent>(entity);
                }
                entities.Dispose();
            }
            if (!m_renderQueueEntities.IsEmptyIgnoreFilter || !m_renderInterpolatedQueueEntities.IsEmptyIgnoreFilter)
            {
                float4 m_LodParameters = 1f;
                float3 m_CameraPosition = 0f;
                float3 m_CameraDirection = 0f;
                LODParameters LodParametersStr;
                var availToDraw = new NativeQueue<WERenderData>(Allocator.Temp);

                if (m_CameraUpdateSystem.TryGetLODParameters(out LodParametersStr))
                {
                    IGameCameraController activeCameraController = m_CameraUpdateSystem.activeCameraController;
                    m_LodParameters = RenderingUtils.CalculateLodParameters(GetLevelOfDetail(m_RenderingSystem.frameLod, activeCameraController), LodParametersStr);
                    m_CameraPosition = LodParametersStr.cameraPosition;
                    m_CameraDirection = m_CameraUpdateSystem.activeViewer.forward;
                }
                if (!m_renderInterpolatedQueueEntities.IsEmptyIgnoreFilter)
                {
                    var job = new WERenderingInterpolatedJob
                    {
                        m_EntityType = SystemAPI.GetEntityTypeHandle(),
                        m_cullingInfo = SystemAPI.GetComponentTypeHandle<CullingInfo>(true),
                        m_iTransform = SystemAPI.GetComponentTypeHandle<InterpolatedTransform>(true),
                        m_weData = SystemAPI.GetBufferTypeHandle<WECustomComponent>(false),
                        m_weDataPendingLookup = SystemAPI.GetBufferLookup<WEWaitingRenderingComponent>(false),
                        m_weDataPending = SystemAPI.GetBufferTypeHandle<WEWaitingRenderingComponent>(false),
                        m_LodParameters = m_LodParameters,
                        m_CameraPosition = m_CameraPosition,
                        m_CameraDirection = m_CameraDirection,
                        availToDraw = availToDraw
                    };
                    job.Schedule(m_renderInterpolatedQueueEntities, Dependency).Complete();
                }
                if (!m_renderQueueEntities.IsEmptyIgnoreFilter)
                {
                    var job2 = new WERenderingRegularJob
                    {
                        m_EntityType = SystemAPI.GetEntityTypeHandle(),
                        m_cullingInfo = SystemAPI.GetComponentTypeHandle<CullingInfo>(true),
                        m_transform = SystemAPI.GetComponentTypeHandle<Game.Objects.Transform>(true),
                        m_weData = SystemAPI.GetBufferTypeHandle<WECustomComponent>(false),
                        m_weDataPendingLookup = SystemAPI.GetBufferLookup<WEWaitingRenderingComponent>(false),
                        m_weDataPending = SystemAPI.GetBufferTypeHandle<WEWaitingRenderingComponent>(false),
                        m_LodParameters = m_LodParameters,
                        m_CameraPosition = m_CameraPosition,
                        m_CameraDirection = m_CameraDirection,
                        availToDraw = availToDraw
                    };
                    job2.Schedule(m_renderQueueEntities, Dependency).Complete();
                }
                while (availToDraw.TryDequeue(out var item))
                {
                    if (item.weComponent.basicRenderInformation.Target is not BasicRenderInformation bri)
                    {
                        item.weComponent.basicRenderInformation.Free();
                        item.weComponent.basicRenderInformation = default;
                        continue;
                    }
                    //LogUtils.DoLog($"Matrix new:\n{string.Join("\t", item.transformMatrix.GetRow(0))}\n{string.Join("\t", item.transformMatrix.GetRow(1))}\n{string.Join("\t", item.transformMatrix.GetRow(2))}\n{string.Join("\t", item.transformMatrix.GetRow(3))}");
                    Graphics.DrawMesh(bri.m_mesh, item.transformMatrix, bri.m_generatedMaterial, 0, null, 0, null);//item.weComponent.MaterialProperties);
                }
                availToDraw.Dispose();
            }
        }
        private struct WERenderData
        {
            public WECustomComponent weComponent;
            public Matrix4x4 transformMatrix;
        }
        public float GetLevelOfDetail(float levelOfDetail, IGameCameraController cameraController)
        {
            if (cameraController != null)
            {
                levelOfDetail *= 1f - 1f / (2f + 0.01f * cameraController.zoom);
            }
            return levelOfDetail;
        }
        [BurstCompile]
        private struct WERenderingInterpolatedJob : IJobChunk
        {
            public ComponentTypeHandle<CullingInfo> m_cullingInfo;
            public ComponentTypeHandle<InterpolatedTransform> m_iTransform;
            public BufferTypeHandle<WECustomComponent> m_weData;
            public BufferTypeHandle<WEWaitingRenderingComponent> m_weDataPending;
            public float4 m_LodParameters;
            public float3 m_CameraPosition;
            public float3 m_CameraDirection;
            public EntityTypeHandle m_EntityType;
            public NativeQueue<WERenderData> availToDraw;
            public BufferLookup<WEWaitingRenderingComponent> m_weDataPendingLookup;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(m_EntityType);
                var cullings = chunk.GetNativeArray(ref m_cullingInfo);
                var i_transforms = chunk.GetNativeArray(ref m_iTransform);
                var weDatas = chunk.GetBufferAccessor(ref m_weData);
                var weDataPendings = chunk.GetBufferAccessor(ref m_weDataPending);

                for (int i = 0; i < entities.Length; i++)
                {
                    var cullInfo = cullings[i];
                    var weCustomData = weDatas[i];
                    var wePending = weDataPendings[i];
                    var i_transform = i_transforms[i];

                    float3 positionRef = i_transform.m_Position;
                    quaternion rotationRef = i_transform.m_Rotation;



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
                            var position = positionRef + math.rotate(rotationRef, weCustomData[j].offsetPosition);
                            var rotation = math.mul(rotationRef, weCustomData[j].offsetRotation * Quaternion.Euler(0, 180, 0));
                            availToDraw.Enqueue(new WERenderData
                            {
                                weComponent = weCustomData[j],
                                transformMatrix = Matrix4x4.TRS(position, rotation, weCustomData[j].scale * weCustomData[j].BriOffsetScaleX / weCustomData[j].BriPixelDensity)
                            });
                        }
                    }
                }
            }
        }
        [BurstCompile]
        private struct WERenderingRegularJob : IJobChunk
        {
            public ComponentTypeHandle<CullingInfo> m_cullingInfo;
            public BufferTypeHandle<WECustomComponent> m_weData;
            public BufferTypeHandle<WEWaitingRenderingComponent> m_weDataPending;
            public float4 m_LodParameters;
            public float3 m_CameraPosition;
            public float3 m_CameraDirection;
            public EntityTypeHandle m_EntityType;
            public NativeQueue<WERenderData> availToDraw;
            public BufferLookup<WEWaitingRenderingComponent> m_weDataPendingLookup;
            internal ComponentTypeHandle<Game.Objects.Transform> m_transform;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(m_EntityType);
                var cullings = chunk.GetNativeArray(ref m_cullingInfo);
                var transforms = chunk.GetNativeArray(ref m_transform);
                var weDatas = chunk.GetBufferAccessor(ref m_weData);
                var weDataPendings = chunk.GetBufferAccessor(ref m_weDataPending);

                for (int i = 0; i < entities.Length; i++)
                {
                    var cullInfo = cullings[i];
                    var weCustomData = weDatas[i];
                    var wePending = weDataPendings[i];
                    var transform = transforms[i];

                    float3 positionRef = transform.m_Position;
                    quaternion rotationRef = transform.m_Rotation;

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
                            var position = positionRef + math.rotate(rotationRef, weCustomData[j].offsetPosition);
                            var rotation = math.mul(rotationRef, weCustomData[j].offsetRotation * Quaternion.Euler(0, 180, 0));
                            availToDraw.Enqueue(new WERenderData
                            {
                                weComponent = weCustomData[j],
                                transformMatrix = Matrix4x4.TRS(position, rotation, weCustomData[j].scale * weCustomData[j].BriOffsetScaleX / weCustomData[j].BriPixelDensity)
                            });
                        }
                    }
                }
            }
        }
    }

}