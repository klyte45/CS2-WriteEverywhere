using BelzontWE.Font.Utility;
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
#else
using Belzont.Interfaces;
using Belzont.Utils;
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
            ++counter;
            EntityCommandBuffer cmd = default;
            if (!m_renderQueueEntities.IsEmptyIgnoreFilter)
            {
                while (availToDraw.TryDequeue(out var item))
                {
                    if (m_pickerTool.Enabled && m_pickerController.CameraLocked.Value
                        && item.weComponent.TargetEntity == m_pickerController.CurrentEntity.Value
                        && m_pickerController.CurrentSubEntity.Value == item.textDataEntity
                        && item.transformMatrix.ValidTRS())
                    {
                        m_pickerController.SetCurrentTargetMatrix(item.transformMatrix);
                    }

                    if (item.weComponent.RenderInformation is not BasicRenderInformation bri)
                    {
                        continue;
                    }
                    if ((((counter + item.textDataEntity.Index) & WEModData.InstanceWE.FramesCheckUpdateVal) == WEModData.InstanceWE.FramesCheckUpdateVal)
                        && !EntityManager.HasComponent<WEWaitingRenderingComponent>(item.textDataEntity)
                        && item.weComponent.GetEffectiveText(EntityManager) != (bri.m_isError ? item.weComponent.LastErrorStr : bri.m_refText))
                    {
                        if (!cmd.IsCreated) cmd = m_endFrameBarrier.CreateCommandBuffer();
                        cmd.AddComponent<WEWaitingRenderingComponent>(item.textDataEntity);
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
                    m_weDataLookup = GetComponentLookup<WETextData>(true),
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
            public ComponentLookup<WETextData> m_weDataLookup;
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

                    if (weCustomData.TargetEntity == Entity.Null || weCustomData.ParentEntity == Entity.Null)
                    {
                        m_CommandBuffer.DestroyEntity(entity);
                        continue;
                    }

                    if (!GetBaseMatrix(entity, ref weCustomData, out CullingInfo cullInfo, out var baseMatrix)) continue;

                    float minDist = RenderingUtils.CalculateMinDistance(cullInfo.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters);
                    if (!weCustomData.HasBRI)
                    {
                        m_CommandBuffer.AddComponent<WEWaitingRenderingComponent>(entity);
                        continue;
                    }
                    int num7 = RenderingUtils.CalculateLod(minDist * minDist, m_LodParameters);
                    if (num7 >= cullInfo.m_MinLod)
                    {
                        availToDraw.Enqueue(new WERenderData
                        {
                            textDataEntity = entity,
                            refEntity = *chunk.GetEntityDataPtrRO(m_EntityType),
                            weComponent = weCustomData,
                            transformMatrix = baseMatrix
                        });
                    }

#if !BURST
                    else if (BasicIMod.VerboseMode)
                    {
                        LogUtils.DoVerboseLog($"NOT RENDER: num7 < cullInfo.m_MinLod = {num7} < {cullInfo.m_MinLod}");
                    }
#endif
                }
            }

            private bool GetBaseMatrix(Entity entity, ref WETextData weCustomData, out CullingInfo cullInfo, out Matrix4x4 matrix, bool scaleless = false)
            {
                float3 positionRef;
                quaternion rotationRef;
                Entity geometryRef = weCustomData.ParentEntity;
                if (m_weDataLookup.TryGetComponent(geometryRef, out var parentData))
                {
                    if (!GetBaseMatrix(entity, ref parentData, out cullInfo, out matrix, true))
                    {
                        return false;
                    }
                }
                else if (weCustomData.ParentEntity != weCustomData.TargetEntity)
                {
                    m_CommandBuffer.DestroyEntity(entity);
                    cullInfo = default;
                    matrix = default;
                    return false;
                }
                else
                {
                    if (!m_cullingInfo.TryGetComponent(geometryRef, out cullInfo))
                    {
                        m_CommandBuffer.DestroyEntity(entity);
                        matrix = default;
                        return false;
                    }
                    if (m_iTransform.TryGetComponent(weCustomData.ParentEntity, out var transform))
                    {
                        positionRef = transform.m_Position;
                        rotationRef = transform.m_Rotation;
                    }
                    else if (m_transform.TryGetComponent(weCustomData.ParentEntity, out var transform2))
                    {
                        positionRef = transform2.m_Position;
                        rotationRef = transform2.m_Rotation;
                    }
                    else
                    {
                        m_CommandBuffer.DestroyEntity(entity);
                        matrix = default;
                        cullInfo = default;
                        return false;
                    }
                    matrix = Matrix4x4.TRS(positionRef, rotationRef, Vector3.one);
                }
                var scale = Vector3.one;
                if (!scaleless && weCustomData.HasBRI)
                {
                    scale = weCustomData.scale * weCustomData.BriOffsetScaleX / weCustomData.BriPixelDensity;
                    if (weCustomData.TextType == WESimulationTextType.Text && weCustomData.maxWidthMeters > 0 && weCustomData.BriWidthMetersUnscaled * scale.x > weCustomData.maxWidthMeters)
                    {
                        scale.x = weCustomData.maxWidthMeters / weCustomData.BriWidthMetersUnscaled;
                    }
                }
                matrix *= Matrix4x4.TRS(weCustomData.offsetPosition, weCustomData.offsetRotation, scale);

                return true;
            }
        }
    }

}