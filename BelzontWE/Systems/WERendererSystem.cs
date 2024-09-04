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
using BelzontWE.Sprites;
using Belzont.Utils;
using Game.Tools;
using Game.Common;
using System.Collections.Generic;
using Game.Prefabs;

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
        private uint FrameCounter { get; set; } = 0;
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
                        ComponentType.ReadOnly<PrefabRef>(),
                        ComponentType.ReadOnly<WESubTextRef>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<WEPlaceholderToBeProcessedInMain>(),
                    }
                },
                new ()
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<CullingInfo>(),
                        ComponentType.ReadOnly<PrefabRef>(),
                        ComponentType.ReadOnly<WETemplateForPrefab>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<WETemplateForPrefabEmpty>(),
                        ComponentType.ReadOnly<WEPlaceholderToBeProcessedInMain>(),
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
            FrameCounter++;
            EntityCommandBuffer cmd;
            if (availToDraw.Count > 0)
            {
                if (dumpNextFrame) LogUtils.DoLog($"Drawing Items: E {m_renderQueueEntities.CalculateEntityCount()} | C {m_renderQueueEntities.CalculateChunkCount()}");
                cmd = m_endFrameBarrier.CreateCommandBuffer();
                while (availToDraw.TryDequeue(out var item))
                {
                    ref var main = ref item.main;
                    ref var material = ref item.material;
                    ref var mesh = ref item.mesh;

                    if (m_pickerTool.Enabled && m_pickerController.CameraLocked.Value
                        && m_pickerController.CurrentSubEntity.Value == item.textDataEntity
                        && item.transformMatrix.ValidTRS())
                    {
                        m_pickerController.SetCurrentTargetMatrix(item.transformMatrix);
                    }

                    bool briWasNull = false;
                    bool doRender = true;
                    if (!mesh.ValueData.InitializedEffectiveText || ((FrameCounter + item.textDataEntity.Index) & WEModData.InstanceWE.FramesCheckUpdateVal) == WEModData.InstanceWE.FramesCheckUpdateVal)
                    {
                        mesh.UpdateFormulaes(EntityManager, item.geometryEntity);
                        material.UpdateFormulaes(EntityManager, item.geometryEntity);
                    }

                    switch (mesh.TextType)
                    {
                        case WESimulationTextType.Text:
                        case WESimulationTextType.Image:
                        case WESimulationTextType.WhiteTexture:
                            if (mesh.IsDirty() && !EntityManager.HasComponent<WEWaitingRendering>(item.textDataEntity))
                            {
                                if (dumpNextFrame) LogUtils.DoInfoLog($"DUMP! +WEWaitingRendering");
                                cmd.AddComponent<WEWaitingRendering>(item.textDataEntity);
                            }
                            break;
                        case WESimulationTextType.Placeholder:
                            if (mesh.TextType == WESimulationTextType.Placeholder && mesh.IsTemplateDirty())
                            {
                                mesh.ClearTemplateDirty();
                                cmd.AddComponent<WEWaitingRendering>(item.textDataEntity);
                                if (dumpNextFrame) LogUtils.DoInfoLog($"DUMP! G = {item.geometryEntity} E = {item.textDataEntity}; T: {main.TargetEntity} P: {main.ParentEntity}\n{main.ItemName} - {mesh.TextType}\nTEMPLATE DIRTY");
                            }
                            doRender = m_pickerTool.IsSelected;
                            break;
                    }
                    if (doRender)
                    {
                        BasicRenderInformation bri;
                        if ((bri = mesh.RenderInformation) == null)
                        {
                            switch (mesh.TextType)
                            {
                                case WESimulationTextType.Text:
                                case WESimulationTextType.Image:
                                    if (mesh.ValueData.EffectiveValue.Length > 0 && !EntityManager.HasComponent<WEWaitingRendering>(item.textDataEntity))
                                    {
                                        cmd.AddComponent<WEWaitingRendering>(item.textDataEntity);
                                        if (dumpNextFrame)
                                        {
                                            LogUtils.DoInfoLog($"DUMP! G = {item.geometryEntity} E =  {item.textDataEntity}; T: {main.TargetEntity} P: {main.ParentEntity}\n{main.ItemName} - {mesh.TextType} - '{mesh.ValueData.EffectiveValue}'\nMARKED TO RE-RENDER");
                                        }
                                    }
                                    goto case WESimulationTextType.Placeholder;
                                case WESimulationTextType.Placeholder:
                                    doRender = m_pickerTool.IsSelected;
                                    briWasNull = true;
                                    goto case WESimulationTextType.WhiteTexture;
                                case WESimulationTextType.WhiteTexture:
                                    bri = WEAtlasesLibrary.GetWhiteTextureBRI();
                                    break;
                            }
                        }
                        if (doRender && bri.m_refText != "")
                        {
                            Material ownMaterial;
                            if (briWasNull) ownMaterial = WEAtlasesLibrary.DefaultMaterialWhiteTexture();
                            else material.GetOwnMaterial(ref mesh, out ownMaterial);
                            Graphics.DrawMesh(bri.Mesh, item.transformMatrix, ownMaterial, 0, null, 0);
                            if (dumpNextFrame) LogUtils.DoInfoLog($"DUMP! G = {item.geometryEntity} E = {item.textDataEntity}; T: {main.TargetEntity} P: {main.ParentEntity}\n{main.ItemName} - {mesh.TextType} - '{mesh.ValueData.EffectiveValue}'\nBRI: {mesh.RenderInformation?.m_refText} | {mesh.RenderInformation?.Mesh?.vertices?.Length} | {!!bri.Main} | M= {item.transformMatrix}");
                        }
                    }
                    if (EntityManager.HasComponent<WETextDataMain>(item.textDataEntity)) EntityManager.SetComponentData(item.textDataEntity, main);
                    if (EntityManager.HasComponent<WETextDataMaterial>(item.textDataEntity)) EntityManager.SetComponentData(item.textDataEntity, material);
                    if (EntityManager.HasComponent<WETextDataMesh>(item.textDataEntity)) EntityManager.SetComponentData(item.textDataEntity, mesh);

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
                    m_weMainLookup = GetComponentLookup<WETextDataMain>(true),
                    m_weMeshLookup = GetComponentLookup<WETextDataMesh>(true),
                    m_weMaterialLookup = GetComponentLookup<WETextDataMaterial>(true),
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
                    doLog = dumpNextFrame,
                    m_weTransformLookup = GetComponentLookup<WETextDataTransform>(true),
                };
                job2.ScheduleParallel(m_renderQueueEntities, Dependency).Complete();
            }
        }


        private struct WERenderData
        {
            public Entity textDataEntity;
            public Entity geometryEntity;
            public WETextDataMain main;
            public WETextDataMesh mesh;
            public WETextDataMaterial material;
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