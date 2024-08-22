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
using Game.Tools;
using Game.Common;
using System.Collections.Generic;



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
        public static uint FrameCounter { get; private set; } = 0;
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
                        ComponentType.ReadOnly<CullingInfo>(),
                    },
                    Any = new ComponentType[]
                    {
                        ComponentType.ReadOnly<WESubTextRef>(),
                        ComponentType.ReadOnly<WETemplateForPrefab>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<WETemplateForPrefabEmpty>(),
                    }
                }
            });

            m_endFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();

            RequireAnyForUpdate(m_renderQueueEntities);
            RenderPipelineManager.beginContextRendering += Render;
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

        }

#if BURST
        [Preserve]
#endif
        protected override void OnDestroy()
        {
            availToDraw.Dispose();
            base.OnDestroy();
            RenderPipelineManager.beginContextRendering -= Render;
        }
#if BURST
        [Preserve]
#endif
        private void Render(ScriptableRenderContext context, List<Camera> cameras)
        {
            ++FrameCounter;
            EntityCommandBuffer cmd;
            if (availToDraw.Count > 0)
            {
                if (dumpNextFrame) LogUtils.DoLog($"Drawing Items: E {m_renderQueueEntities.CalculateEntityCount()} | C {m_renderQueueEntities.CalculateChunkCount()}");
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
                        if (dumpNextFrame) LogUtils.DoInfoLog($"DUMP! G = {item.geometryEntity} E = {item.textDataEntity}; T: {item.weComponent.TargetEntity} P: {item.weComponent.ParentEntity}\n{item.weComponent.ItemName} - {item.weComponent.TextType}\nTEMPLATE DIRTY");
                        continue;
                    }
                    BasicRenderInformation bri;
                    if ((bri = item.weComponent.RenderInformation) == null)
                    {
                        if (!item.weComponent.InitializedEffectiveText)
                        {
                            item.weComponent.UpdateEffectiveText(EntityManager, item.geometryEntity);
                            cmd.SetComponent(item.textDataEntity, item.weComponent);
                        }
                        switch (item.weComponent.TextType)
                        {
                            case WESimulationTextType.Text:
                            case WESimulationTextType.Image:
                                if (!EntityManager.HasComponent<WEWaitingRendering>(item.textDataEntity))
                                {
                                    cmd.AddComponent<WEWaitingRendering>(item.textDataEntity);
                                    if (dumpNextFrame)
                                    {
                                        LogUtils.DoInfoLog($"DUMP! G = {item.geometryEntity} E =  {item.textDataEntity}; T: {item.weComponent.TargetEntity} P: {item.weComponent.ParentEntity}\n{item.weComponent.ItemName} - {item.weComponent.TextType} - '{item.weComponent.EffectiveText}'\nMARKED TO RE-RENDER");
                                    }
                                }
                                break;
                            case WESimulationTextType.WhiteTexture:
                                bri = WEAtlasesLibrary.GetWhiteTextureBRI();
                                item.weComponent = item.weComponent.UpdateBRI(bri, bri.m_refText);
                                break;

                        }
                    }

                    bool briWasNull = false;
                    if (bri is null)
                    {
                        if (!m_pickerTool.IsSelected) continue;
                        bri = WEAtlasesLibrary.GetWhiteTextureBRI();
                        briWasNull = true;
                    }
                    else if (item.weComponent.TextType == WESimulationTextType.Text || item.weComponent.TextType == WESimulationTextType.Image)
                    {
                        if ((((FrameCounter + item.textDataEntity.Index) & WEModData.InstanceWE.FramesCheckUpdateVal) == WEModData.InstanceWE.FramesCheckUpdateVal)
                              && !EntityManager.HasComponent<WEWaitingRendering>(item.textDataEntity)
                              && item.weComponent.UpdateEffectiveText(EntityManager, item.geometryEntity)
                              )
                        {
                            if (dumpNextFrame) LogUtils.DoInfoLog($"DUMP! +WEWaitingRendering");
                            cmd.AddComponent<WEWaitingRendering>(item.textDataEntity);
                        }
                    }
                    if (bri.m_refText != "")
                    {
                        var material = briWasNull ? bri.GeneratedMaterial : item.weComponent.OwnMaterial;
                        foreach (var camera in cameras)
                        {
                            if (camera.cameraType == CameraType.Game || camera.cameraType == CameraType.SceneView)
                            {
                                Graphics.DrawMesh(bri.Mesh, item.transformMatrix, material, 0, camera, 0);//, item.weComponent.MaterialProperties);
                            }
                        }
                        if (dumpNextFrame) LogUtils.DoInfoLog($"DUMP! G = {item.geometryEntity} E = {item.textDataEntity}; T: {item.weComponent.TargetEntity} P: {item.weComponent.ParentEntity}\n{item.weComponent.ItemName} - {item.weComponent.TextType} - '{item.weComponent.EffectiveText}'\nBRI: {item.weComponent.RenderInformation?.m_refText} | {item.weComponent.RenderInformation?.Mesh?.vertices?.Length} | {bri.GeneratedMaterial} | M= {item.transformMatrix}");
                    }
                    if (!briWasNull) cmd.SetComponent(item.textDataEntity, item.weComponent);
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
                    m_cullingInfo = GetComponentTypeHandle<CullingInfo>(true),
                    m_transform = GetComponentLookup<Game.Objects.Transform>(true),
                    m_iTransform = GetComponentLookup<InterpolatedTransform>(true),
                    m_weDataLookup = GetComponentLookup<WETextData>(true),
                    m_weTemplateUpdaterLookup = GetComponentLookup<WETemplateUpdater>(true),
                    m_weTemplateForPrefabLookup = GetComponentLookup<WETemplateForPrefab>(true),
                    m_CommandBuffer = m_endFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                    m_LodParameters = m_LodParameters,
                    m_CameraPosition = m_CameraPosition,
                    m_CameraDirection = m_CameraDirection,
                    availToDraw = availToDraw.AsParallelWriter(),
                    isAtWeEditor = m_pickerTool.IsSelected,
                    m_selectedSubEntity = m_pickerController.CurrentSubEntity.Value,
                    m_selectedEntity = m_pickerController.CurrentEntity.Value,
                    m_weSubRefLookup = GetBufferLookup<WESubTextRef>(true),
                    doLog = dumpNextFrame
                };
                job2.ScheduleParallel(m_renderQueueEntities, Dependency).Complete();
            }
        }


        private struct WERenderData
        {
            public Entity textDataEntity;
            public Entity geometryEntity;
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
    }

}