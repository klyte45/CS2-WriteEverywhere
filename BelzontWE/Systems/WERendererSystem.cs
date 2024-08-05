using Game;
using Game.Rendering;
using Game.SceneFlow;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using BelzontWE.Font.Utility;
using WriteEverywhere.Sprites;
using Belzont.Utils;
using Unity.Burst.Intrinsics;


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
        internal static bool dumpNextFrame;
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
                        ComponentType.ReadOnly<WEWaitingRendering>(),
                        ComponentType.ReadOnly<WETemplateData>(),
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
            EntityCommandBuffer cmd;
            if (!m_renderQueueEntities.IsEmptyIgnoreFilter)
            {
                cmd = m_endFrameBarrier.CreateCommandBuffer();
                while (availToDraw.TryDequeue(out var item))
                {
                    if (m_pickerTool.Enabled && m_pickerController.CameraLocked.Value
                        && m_pickerController.CurrentSubEntity.Value == item.textDataEntity
                        && item.transformMatrix.ValidTRS())
                    {
                        m_pickerController.SetCurrentTargetMatrix(item.transformMatrix);
                    }
                    if (item.weComponent.IsTemplateDirty())
                    {
                        item.weComponent.ClearTemplateDirty();
                        cmd.AddComponent<WEWaitingRenderingPlaceholder>(item.textDataEntity);
                        cmd.SetComponent(item.textDataEntity, item.weComponent);
                        if (dumpNextFrame) LogUtils.DoInfoLog($"DUMP! E = {item.textDataEntity}; T: {item.weComponent.TargetEntity} P: {item.weComponent.ParentEntity}\n{item.weComponent.ItemName} - {item.weComponent.TextType}\nTEMPLATE DIRTY");
                        continue;
                    }
                    BasicRenderInformation bri;
                    if ((bri = item.weComponent.RenderInformation) == null)
                    {
                        if (!item.weComponent.InitializedEffectiveText)
                        {
                            item.weComponent.UpdateEffectiveText(EntityManager);
                            cmd.SetComponent(item.textDataEntity, item.weComponent);
                        }
                        else if ((item.weComponent.TextType == WESimulationTextType.Text || item.weComponent.TextType == WESimulationTextType.Image) && !EntityManager.HasComponent<WEWaitingRendering>(item.textDataEntity))
                        {
                            cmd.SetComponent(item.textDataEntity, item.weComponent);
                            cmd.AddComponent<WEWaitingRendering>(item.textDataEntity);
                            if (dumpNextFrame)
                            {
                                LogUtils.DoInfoLog($"DUMP! E =  {item.textDataEntity}; T: {item.weComponent.TargetEntity} P: {item.weComponent.ParentEntity}\n{item.weComponent.ItemName} - {item.weComponent.TextType} - '{item.weComponent.EffectiveText}'\nMARKED TO RE-RENDER");
                            }
                        }
                        if (!m_pickerTool.IsSelected) continue;
                    }
                    bool wasDirty = false;
                    if (bri is null)
                    {
                        bri = WEAtlasesLibrary.GetWhiteTextureBRI();
                    }
                    else if (item.weComponent.TextType == WESimulationTextType.Text || item.weComponent.TextType == WESimulationTextType.Image)
                    {
                        if ((((counter + item.textDataEntity.Index) & WEModData.InstanceWE.FramesCheckUpdateVal) == WEModData.InstanceWE.FramesCheckUpdateVal)
                              && !EntityManager.HasComponent<WEWaitingRendering>(item.textDataEntity)
                              && item.weComponent.UpdateEffectiveText(EntityManager)
                              )
                        {
                            wasDirty = true;
                            cmd.AddComponent<WEWaitingRendering>(item.textDataEntity);
                        }
                        wasDirty |= item.weComponent.IsDirty();
                    }
                    if (dumpNextFrame)
                    {
                        LogUtils.DoInfoLog($"DUMP! E = {item.textDataEntity}; T: {item.weComponent.TargetEntity} P: {item.weComponent.ParentEntity}\n{item.weComponent.ItemName} - {item.weComponent.TextType} - '{item.weComponent.EffectiveText}'\nBRI: {item.weComponent.RenderInformation?.m_refText} | {item.weComponent.RenderInformation?.Mesh?.vertices?.Length} | M= {item.transformMatrix}");

                    }
                    if (bri.m_refText != "") Graphics.DrawMesh(bri.Mesh, item.transformMatrix, bri.m_generatedMaterial, 0, null, 0, item.weComponent.MaterialProperties);
                    if (wasDirty) cmd.SetComponent(item.textDataEntity, item.weComponent);
                }
                dumpNextFrame = false;
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
                    m_weData = GetComponentTypeHandle<WETextData>(true),
                    m_weTemplateUpdaterLookup = GetComponentLookup<WETemplateUpdater>(true),
                    m_weTemplateForPrefabLookup = GetComponentLookup<WETemplateForPrefab>(true),
                    m_weTemplateDataLookup = GetComponentLookup<WETemplateData>(true),
                    m_CommandBuffer = m_endFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                    m_LodParameters = m_LodParameters,
                    m_CameraPosition = m_CameraPosition,
                    m_CameraDirection = m_CameraDirection,
                    availToDraw = availToDraw.AsParallelWriter(),
                    isAtWeEditor = m_pickerTool.IsSelected,
                    m_selectedSubEntity = m_pickerController.CurrentSubEntity.Value
                };
                job2.ScheduleParallel(m_renderQueueEntities, Dependency).Complete();
            }
        }

        private struct WERenderData
        {
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
            public ComponentLookup<WETemplateUpdater> m_weTemplateUpdaterLookup;
            public ComponentLookup<WETemplateForPrefab> m_weTemplateForPrefabLookup;
            public ComponentLookup<WETemplateData> m_weTemplateDataLookup;
            public ComponentLookup<InterpolatedTransform> m_iTransform;
            public ComponentLookup<Game.Objects.Transform> m_transform;
            public float4 m_LodParameters;
            public float3 m_CameraPosition;
            public float3 m_CameraDirection;
            public EntityTypeHandle m_EntityType;
            public NativeQueue<WERenderData>.ParallelWriter availToDraw;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
            public Entity m_selectedSubEntity;
            public bool isAtWeEditor;

            public unsafe void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(m_EntityType);
                var weDatas = chunk.GetNativeArray(ref m_weData);

                for (int i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    var weCustomData = weDatas[i];

                    if (weCustomData.TextType == WESimulationTextType.Archetype)
                    {
                        if (m_weTemplateForPrefabLookup.HasComponent(weCustomData.TargetEntity))
                        {
                            if (m_weTemplateForPrefabLookup[weCustomData.TargetEntity].childEntity != entity)
                            {
#if !BURST
                                if (BasicIMod.DebugMode) LogUtils.DoLog($"Destroy Entity! {entity} childEntity {prefabForLookup.childEntity} != weCustomData.TargetEntity {weCustomData.TargetEntity} (prefabForLookup I)");
#endif
                                m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, entity);
                            }
                        }
                        continue;
                    }

                    if (!weCustomData.HasBRI && weCustomData.TextType != WESimulationTextType.Placeholder)
                    {
                        m_CommandBuffer.AddComponent<WEWaitingRendering>(unfilteredChunkIndex, entity);
                        continue;
                    }

                    if (weCustomData.TextType == WESimulationTextType.Placeholder != m_weTemplateUpdaterLookup.HasComponent(entity))
                    {
                        if (weCustomData.TextType != WESimulationTextType.Placeholder)
                        {
                            m_CommandBuffer.AddComponent<WEWaitingRendering>(unfilteredChunkIndex, entity);
                        }
                        else
                        {
                            m_CommandBuffer.AddComponent<WEWaitingRenderingPlaceholder>(unfilteredChunkIndex, entity);
                        }
                        continue;
                    }

                    if (!m_weTemplateDataLookup.HasComponent(weCustomData.TargetEntity) && (weCustomData.TargetEntity == Entity.Null || weCustomData.ParentEntity == Entity.Null))
                    {
#if !BURST
                        if (BasicIMod.DebugMode) LogUtils.DoLog($"Destroy Entity! {entity} any target or parent are null");
#endif
                        m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, entity);
                        continue;
                    }
                    if ((weCustomData.TextType == WESimulationTextType.Placeholder && !isAtWeEditor)) continue;

                    if (!GetBaseMatrix(entity, ref weCustomData, out CullingInfo cullInfo, out var baseMatrix, unfilteredChunkIndex)) continue;
                    float minDist = RenderingUtils.CalculateMinDistance(cullInfo.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters);

                    int num7 = RenderingUtils.CalculateLod(minDist * minDist, m_LodParameters);
                    if (num7 >= cullInfo.m_MinLod || (isAtWeEditor && entity == m_selectedSubEntity))
                    {
                        availToDraw.Enqueue(new WERenderData
                        {
                            textDataEntity = entity,
                            weComponent = weCustomData,
                            transformMatrix = baseMatrix
                        });
                    }

#if !BURST
                    else if (BasicIMod.VerboseMode)
                    {
                        LogUtils.DoVerboseLog($"NOT RENDER {entity}: num7 < cullInfo.m_MinLod = {num7} < {cullInfo.m_MinLod}");
                    }
#endif
                }
            }

            private bool GetBaseMatrix(Entity entity, ref WETextData weCustomData, out CullingInfo cullInfo, out Matrix4x4 matrix, int unfilteredChunkIndex, bool scaleless = false)
            {
                float3 positionRef;
                quaternion rotationRef;
                Entity parentRef = weCustomData.ParentEntity;
                Entity targetRef = weCustomData.TargetEntity;
                WETextData refWeData = weCustomData;


                if (m_weTemplateUpdaterLookup.TryGetComponent(parentRef, out var updater))
                {
                    if (!m_weDataLookup.TryGetComponent(parentRef, out var templateUpdaterWEdata))
                    {
                        cullInfo = default;
                        matrix = default;
                        return false;
                    }
                    if (updater.childEntity != weCustomData.TargetEntity)
                    {

#if !BURST
                        if (BasicIMod.DebugMode) LogUtils.DoLog($"Destroy Entity! {entity} childEntity {updater.childEntity} != weCustomData.TargetEntity {weCustomData.TargetEntity} (I)");
#endif
                        m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, entity);
                        cullInfo = default;
                        matrix = default;
                        return false;
                    }
                    parentRef = templateUpdaterWEdata.ParentEntity;
                    targetRef = templateUpdaterWEdata.TargetEntity;
                    refWeData = templateUpdaterWEdata;
                }
                else if (m_weTemplateUpdaterLookup.TryGetComponent(targetRef, out updater))
                {
                    if (!m_weDataLookup.TryGetComponent(targetRef, out var templateUpdaterWEdata))
                    {
                        cullInfo = default;
                        matrix = default;
                        return false;
                    }
                    if (updater.childEntity != weCustomData.TargetEntity)
                    {
#if !BURST
                        if (BasicIMod.DebugMode) LogUtils.DoLog($"Destroy Entity! {entity} childEntity {updater.childEntity} != weCustomData.TargetEntity {weCustomData.TargetEntity} (II)");
#endif
                        m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, entity);
                        cullInfo = default;
                        matrix = default;
                        return false;
                    }
                    targetRef = templateUpdaterWEdata.TargetEntity;
                }


                if (m_weDataLookup.TryGetComponent(targetRef, out var archetypeWeData) && archetypeWeData.TextType == WESimulationTextType.Archetype)
                {
                    targetRef = archetypeWeData.TargetEntity;
                }


                if (m_weDataLookup.TryGetComponent(parentRef, out var parentData))
                {
                    if (!GetBaseMatrix(entity, ref parentData, out cullInfo, out matrix, unfilteredChunkIndex, true))
                    {
                        return false;
                    }
                }
                else if (m_weTemplateDataLookup.HasComponent(parentRef))
                {
                    cullInfo = default;
                    matrix = Matrix4x4.TRS(m_CameraPosition, quaternion.identity, Vector3.one) * Matrix4x4.TRS(Vector3.back, quaternion.Euler(m_CameraDirection), Vector3.one);
                    return true;
                }
                else if (parentRef != targetRef)
                {
                    cullInfo = default;
                    matrix = default;
                    if (m_weTemplateDataLookup.HasComponent(targetRef)) return false;
#if !BURST
                    if (BasicIMod.DebugMode) LogUtils.DoLog($"Destroy Entity! {entity} parentRef {parentRef} != targetRef {targetRef}");
#endif
                    m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, entity);
                    return false;
                }
                else
                {
                    if (!m_cullingInfo.TryGetComponent(parentRef, out cullInfo))
                    {
#if !BURST
                        if (BasicIMod.DebugMode) LogUtils.DoLog($"Destroy Entity! {entity} no cull info on parent ref ({parentRef})");
#endif
                        m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, entity);
                        matrix = default;
                        return false;
                    }
                    if (cullInfo.m_PassedCulling != 1 && (!isAtWeEditor || entity != m_selectedSubEntity))
                    {
                        matrix = default;
                        return false;
                    }
                    if (m_iTransform.TryGetComponent(parentRef, out var transform))
                    {
                        positionRef = transform.m_Position;
                        rotationRef = transform.m_Rotation;
                    }
                    else if (m_transform.TryGetComponent(parentRef, out var transform2))
                    {
                        positionRef = transform2.m_Position;
                        rotationRef = transform2.m_Rotation;
                    }
                    else
                    {
#if !BURST
                        if (BasicIMod.DebugMode) LogUtils.DoLog($"Destroy Entity! {entity} no transform! ({parentRef})");
#endif
                        m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, entity);
                        matrix = default;
                        cullInfo = default;
                        return false;
                    }
                    matrix = Matrix4x4.TRS(positionRef, rotationRef, Vector3.one);
                }
                var scale = Vector3.one;
                if (!scaleless)
                {
                    scale = weCustomData.scale * weCustomData.BriOffsetScaleX / weCustomData.BriPixelDensity;
                    if (weCustomData.HasBRI && weCustomData.TextType == WESimulationTextType.Text && weCustomData.maxWidthMeters > 0 && weCustomData.BriWidthMetersUnscaled * scale.x > weCustomData.maxWidthMeters)
                    {
                        scale.x = weCustomData.maxWidthMeters / weCustomData.BriWidthMetersUnscaled;
                    }
                }

                matrix *= Matrix4x4.TRS(refWeData.offsetPosition, refWeData.offsetRotation, scale);


                return true;
            }
        }
    }

}