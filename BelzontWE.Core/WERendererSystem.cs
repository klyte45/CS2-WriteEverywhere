using Belzont.Interfaces;
using Belzont.Utils;
using BelzontWE.Font.Utility;
using Colossal.Entities;
using Game;
using Game.Rendering;
using Game.SceneFlow;
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
        private EntityQuery m_renderQueueEntities;
        private EndFrameBarrier m_endFrameBarrier;
        private CameraUpdateSystem m_CameraUpdateSystem;
        private RenderingSystem m_RenderingSystem;
        private WEWorldPickerController m_pickerController;
        private WEWorldPickerTool m_pickerTool;
        private NativeQueue<WERenderData> availToDraw = new(Allocator.Persistent);
#if BURST
        [Preserve]
#endif
        protected override void OnCreate()
        {
            base.OnCreate();

            m_CameraUpdateSystem = World.GetExistingSystemManaged<CameraUpdateSystem>();
            m_RenderingSystem = World.GetExistingSystemManaged<RenderingSystem>();
            m_pickerController = World.GetExistingSystemManaged<WEWorldPickerController>();
            m_pickerTool = World.GetExistingSystemManaged<WEWorldPickerTool>();

            m_renderQueueEntities = GetEntityQuery(new EntityQueryDesc[]
            {
                new ()
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<WETextData>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<WEWaitingRenderingComponent>()
                    }
                }
            });

            m_endFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();

            RequireAnyForUpdate(m_renderQueueEntities);
        }
#if BURST
        [Preserve]
#endif
        protected override void OnUpdate()
        {
            if (GameManager.instance.isLoading) return;
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
            CheckRenderQueue(m_LodParameters, m_CameraPosition, m_CameraDirection);
            Render_Impl();

        }

#if BURST
        [Preserve]
#endif
        protected override void OnDestroy()
        {
            availToDraw.Dispose();
            base.OnDestroy();
        }
        private uint counter = 0;
#if BURST
        [Preserve]
#endif
        private void Render_Impl()
        {
            var checkUpdates = (++counter & WEModData.InstanceWE.FramesCheckUpdateVal) == WEModData.InstanceWE.FramesCheckUpdateVal;
            if (!m_renderQueueEntities.IsEmptyIgnoreFilter)
            {
                while (availToDraw.TryDequeue(out var item))
                {
                    if (item.weComponent.RenderInformation is not BasicRenderInformation bri)
                    {
                        continue;
                    }
                    DynamicBuffer<WESubTextRef> buffer = default;
                    if (checkUpdates && item.weComponent.GetEffectiveText(EntityManager) != (bri.m_isError ? item.weComponent.LastErrorStr : bri.m_refText))
                    {
                        EntityManager.AddComponent<WEWaitingRenderingComponent>(item.textDataEntity);
                    }
                    else if (m_pickerTool.Enabled && m_pickerController.CameraLocked.Value
                        && item.weComponent.TargetEntity == m_pickerController.CurrentEntity.Value
                        && EntityManager.TryGetBuffer<WESubTextRef>(m_pickerController.CurrentEntity.Value, true, out buffer)
                        && buffer[m_pickerController.CurrentItemIdx.Value].m_weTextData == item.textDataEntity
                        && item.weComponent.RenderInformation != null
                        && item.transformMatrix.ValidTRS())
                    {
                        m_pickerController.SetCurrentTargetMatrix(item.transformMatrix);
                    }
                    else if (BasicIMod.TraceMode && m_pickerTool.Enabled && m_pickerController.CameraLocked.Value)
                    {
                        LogUtils.DoTraceLog($"NOT UPDATE TRANSFORM!");
                        LogUtils.DoTraceLog($"item.weComponent.TargetEntity == m_pickerController.CurrentEntity.Value => {item.weComponent.TargetEntity} == {m_pickerController.CurrentEntity.Value}");
                        LogUtils.DoTraceLog($"buffer = {buffer.IsCreated}");
                        if (buffer.IsCreated)
                        {
                            LogUtils.DoTraceLog($"buffer[m_pickerController.CurrentItemIdx.Value].m_weTextData == item.textDataEntity => {(buffer.Length > m_pickerController.CurrentItemIdx.Value ? buffer[m_pickerController.CurrentItemIdx.Value].m_weTextData : default)} == {item.textDataEntity}");
                        }
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
            if (!GameManager.instance.isGameLoading && !GameManager.instance.isLoading && !m_renderQueueEntities.IsEmptyIgnoreFilter)
            {
                var job2 = new WERenderingJob
                {
                    m_EntityType = GetEntityTypeHandle(),
                    m_cullingInfo = GetComponentLookup<CullingInfo>(true),
                    m_transform = GetComponentLookup<Game.Objects.Transform>(true),
                    m_iTransform = GetComponentLookup<InterpolatedTransform>(true),
                    m_weData = GetComponentTypeHandle<WETextData>(false),
                    m_CommandBuffer = m_endFrameBarrier.CreateCommandBuffer(),
                    m_LodParameters = m_LodParameters,
                    m_CameraPosition = m_CameraPosition,
                    m_CameraDirection = m_CameraDirection,
                    availToDraw = availToDraw
                };
                job2.Schedule(m_renderQueueEntities, Dependency).Complete();
            }
        }

        private struct WERenderData
        {
            public Entity refEntity;
            public Entity textDataEntity;
            public WETextData weComponent;
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
            public ComponentLookup<CullingInfo> m_cullingInfo;
            public ComponentTypeHandle<WETextData> m_weData;
            public ComponentLookup<InterpolatedTransform> m_iTransform;
            public ComponentLookup<Game.Objects.Transform> m_transform;
            public float4 m_LodParameters;
            public float3 m_CameraPosition;
            public float3 m_CameraDirection;
            public EntityTypeHandle m_EntityType;
            public NativeQueue<WERenderData> availToDraw;
            public EntityCommandBuffer m_CommandBuffer;

            public unsafe void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(m_EntityType);
                var weDatas = chunk.GetNativeArray(ref m_weData);

                for (int i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    var weCustomData = weDatas[i];

                    if (weCustomData.TargetEntity == Entity.Null)
                    {
                        m_CommandBuffer.DestroyEntity(entity);
                        continue;
                    }

                    float3 positionRef;
                    quaternion rotationRef;

                    if (!m_cullingInfo.TryGetComponent(weCustomData.TargetEntity, out var cullInfo))
                    {
                        m_CommandBuffer.DestroyEntity(entity);
                        continue;
                    }

                    if (m_iTransform.TryGetComponent(weCustomData.TargetEntity, out var transform))
                    {
                        positionRef = transform.m_Position;
                        rotationRef = transform.m_Rotation;
                    }
                    else if (m_transform.TryGetComponent(weCustomData.TargetEntity, out var transform2))
                    {
                        positionRef = transform2.m_Position;
                        rotationRef = transform2.m_Rotation;
                    }
                    else
                    {
                        m_CommandBuffer.DestroyEntity(entity);
                        continue;
                    }

                    if (weCustomData.RenderInformation == null)
                    {
                        m_CommandBuffer.AddComponent<WEWaitingRenderingComponent>(entity);
                        continue;
                    }

                    float minDist = RenderingUtils.CalculateMinDistance(cullInfo.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters);
                    int num7 = RenderingUtils.CalculateLod(minDist * minDist, m_LodParameters);
                    if (num7 >= cullInfo.m_MinLod)
                    {
                        availToDraw.Enqueue(new WERenderData
                        {
                            textDataEntity = entity,
                            refEntity = *chunk.GetEntityDataPtrRO(m_EntityType),
                            weComponent = weCustomData,
                            transformMatrix = Matrix4x4.TRS(positionRef, rotationRef, Vector3.one) * Matrix4x4.TRS(weCustomData.offsetPosition, weCustomData.offsetRotation, weCustomData.scale * weCustomData.BriOffsetScaleX / weCustomData.BriPixelDensity)
                        });
                    }
                    else if (BasicIMod.VerboseMode)
                    {
                        LogUtils.DoVerboseLog($"NOT RENDER: num7 < cullInfo.m_MinLod = {num7} < {cullInfo.m_MinLod}");
                    }
                }
            }
        }
    }

}