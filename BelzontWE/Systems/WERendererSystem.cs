using Belzont.Interfaces;
using Belzont.Utils;
using BelzontWE.Font.Utility;
using BelzontWE.Sprites;
using Game;
using Game.Common;
using Game.Prefabs;
using Game.Rendering;
using Game.SceneFlow;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Colossal.Entities;






#if BURST
using UnityEngine.Scripting;
using Unity.Burst;
#else
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

        internal const char VARIABLE_ITEM_SEPARATOR = '↓';
        internal const char VARIABLE_KV_SEPARATOR = '→';

#if DEBUG
        public uint DrawCallsLastFrame { get; private set; } = 0;
#endif
        private uint FrameCounter { get; set; } = 0;
#if BURST
        [Preserve]
#endif
        protected unsafe override void OnCreate()
        {
            base.OnCreate();

            LogUtils.DoInfoLog($"WERenderingJob size: {sizeof(WERenderingJob)}");

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
                        ComponentType.ReadOnly<WEDrawing>(),
                    },
                    None = new ComponentType[]
                    {
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
                        ComponentType.ReadOnly<WEDrawing>(),
                    },
                    None = new ComponentType[]
                    {
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
            if (GameManager.instance.isGameLoading) return;
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
#if DEBUG
            DrawCallsLastFrame = 0;
#endif
            if (availToDraw.Count > 0)
            {
                if (dumpNextFrame) LogUtils.DoLog($"Drawing Items: E {m_renderQueueEntities.CalculateEntityCount()} | C {m_renderQueueEntities.CalculateChunkCount()}");
                cmd = m_endFrameBarrier.CreateCommandBuffer();
                while (availToDraw.TryDequeue(out var item))
                {
                    ref var transform = ref item.transform;
                    ref var main = ref item.main;
                    ref var material = ref item.material;
                    ref var mesh = ref item.mesh;

                    if (!EntityManager.HasEnabledComponent<WETextDataDirtyFormulae>(item.textDataEntity))
                    {
                        main.CheckDirtyFormulae(item.geometryEntity, item.textDataEntity, item.variables, cmd);
                    }

                    if (main.nextUpdateFrame == 0) continue;

                    bool ìsPlaceholder = false;
                    bool doRender = true;


                    if (m_pickerTool.Enabled && m_pickerController.CameraLocked.Value
                        && m_pickerController.CurrentSubEntity.Value == item.textDataEntity
                        && item.transformMatrix.ValidTRS())
                    {
                        m_pickerController.SetCurrentTargetMatrix(item.transformMatrix);
                    }

                    switch (mesh.TextType)
                    {
                        case WESimulationTextType.Text:
                        case WESimulationTextType.Image:
                        case WESimulationTextType.WhiteTexture:
                        case WESimulationTextType.WhiteCube:
                            if (mesh.IsDirty() && !EntityManager.HasEnabledComponent<WEWaitingRendering>(item.textDataEntity))
                            {
                                if (dumpNextFrame) LogUtils.DoInfoLog($"DUMP! +WEWaitingRendering");
                                cmd.AddComponent<WEWaitingRendering>(item.textDataEntity);
                                cmd.SetComponentEnabled<WEWaitingRendering>(item.textDataEntity, true);
                            }
                            break;
                        case WESimulationTextType.Placeholder:
                            if (mesh.IsTemplateDirty())
                            {
                                mesh.ClearTemplateDirty();
                                cmd.AddComponent<WEWaitingRendering>(item.textDataEntity);
                                cmd.SetComponentEnabled<WEWaitingRendering>(item.textDataEntity, true);
                                if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"DUMP! G = {item.geometryEntity} E = {item.textDataEntity}; T: {main.TargetEntity} P: {main.ParentEntity}\n{main.ItemName} - {mesh.TextType} - {mesh.originalName}\nTEMPLATE DIRTY");
                            }
                            doRender = m_pickerTool.IsSelected;
                            break;
                        case WESimulationTextType.MatrixTransform:
                            doRender = false;
                            break;
                    }
                    if (doRender)
                    {
                        IBasicRenderInformation bri;
                        if ((bri = mesh.RenderInformation) == null)
                        {
                            switch (mesh.TextType)
                            {
                                case WESimulationTextType.Text:
                                case WESimulationTextType.Image:
                                    if (mesh.ValueData.EffectiveValue.Length > 0 && !EntityManager.HasEnabledComponent<WEWaitingRendering>(item.textDataEntity))
                                    {
                                        cmd.AddComponent<WEWaitingRendering>(item.textDataEntity);
                                        cmd.SetComponentEnabled<WEWaitingRendering>(item.textDataEntity, true);
                                        if (dumpNextFrame)
                                        {
                                            LogUtils.DoInfoLog($"DUMP! G = {item.geometryEntity} E =  {item.textDataEntity}; T: {main.TargetEntity} P: {main.ParentEntity}\n{main.ItemName} - {mesh.TextType} - '{mesh.ValueData.EffectiveValue}'\nMARKED TO RE-RENDER");
                                        }
                                    }
                                    goto case WESimulationTextType.Placeholder;
                                case WESimulationTextType.Placeholder:
                                    doRender = m_pickerTool.IsSelected;
                                    ìsPlaceholder = true;
                                    goto case WESimulationTextType.WhiteTexture;
                                case WESimulationTextType.WhiteTexture:
                                case WESimulationTextType.WhiteCube:
                                    bri = WEAtlasesLibrary.GetWhiteTextureBRI();
                                    break;
                            }
                        }
                        var brii = bri as PrimitiveRenderInformation;
                        if (doRender && (brii is null || brii.m_refText != ""))
                        {
                            Material[] ownMaterial = null;
                            if (ìsPlaceholder) ownMaterial = WEAtlasesLibrary.DefaultMaterialWhiteTexture();
                            else material.GetOwnMaterial(ref mesh, brii?.CubeCharCoordinates, out ownMaterial);

                            var bri2 = bri as PrimitiveRenderInformation;
                            var meshCount = bri2 is null || mesh.TextType == WESimulationTextType.WhiteCube ? 1 : bri2.MeshCount(item.material.Shader);
                            for (int i = 0; i < meshCount; i++)
                            {
                                var geomMesh = bri2 is not null ? (mesh.TextType == WESimulationTextType.WhiteCube ? bri2.MeshCube[0] : bri2.GetMesh(item.material.Shader, i)) : bri.GetMesh(item.material.Shader);
                                var effectiveMatrix = bri2 is null ? item.transformMatrix : item.transformMatrix * bri2.GetMeshTranslation(item.material.Shader, i);

                                Graphics.DrawMesh(geomMesh, effectiveMatrix, ownMaterial[i], 0, null, 0, null, ShadowCastingMode.TwoSided, true, null, LightProbeUsage.BlendProbes);
                                if (m_pickerController.IsValidEditingItem() && m_pickerController.ShowProjectionCube.Value && m_pickerController.CurrentSubEntity.Value == item.textDataEntity && material.Shader == WEShader.Decal)
                                {
                                    if (dumpNextFrame) LogUtils.DoInfoLog($"DUMP! DRAWING Extra mesh");
                                    Graphics.DrawMesh(geomMesh, effectiveMatrix, WEAtlasesLibrary.DefaultMaterialSemiTransparent(), 0, null, 0, null, false, false);
                                }
#if DEBUG
                                DrawCallsLastFrame++;
#endif
                                if (dumpNextFrame) LogUtils.DoInfoLog($"DUMP! G = {item.geometryEntity} E = {item.textDataEntity}; T: {main.TargetEntity} P: {main.ParentEntity}\n{main.ItemName} - {mesh.TextType} - '{mesh.ValueData.EffectiveValue}'\nBRI: {geomMesh?.vertices?.Length} | {!!bri.Main} | M= {item.transformMatrix}");
                            }
                        }
                    }
                    //      if (!WETemplateManager.Instance.IsAnyGarbagePending)
                    {
                        if (EntityManager.HasComponent<WETextDataMain>(item.textDataEntity)) EntityManager.SetComponentData(item.textDataEntity, main);
                        if (EntityManager.HasComponent<WETextDataMaterial>(item.textDataEntity)) EntityManager.SetComponentData(item.textDataEntity, material);
                        if (EntityManager.HasComponent<WETextDataMesh>(item.textDataEntity)) EntityManager.SetComponentData(item.textDataEntity, mesh);
                        if (EntityManager.HasComponent<WETextDataTransform>(item.textDataEntity)) EntityManager.SetComponentData(item.textDataEntity, transform);
                    }

                }
                dumpNextFrame = false;
            }
        }
#if BURST
        [BurstCompile]
#endif
        private void CheckRenderQueue(float4 m_LodParameters, float3 m_CameraPosition, float3 m_CameraDirection)
        {
            if (!GameManager.instance.isGameLoading && !m_renderQueueEntities.IsEmptyIgnoreFilter)
            {
                var job2 = new WERenderingJob
                {
                    m_EntityType = GetEntityTypeHandle(),
                    m_cullingInfo = GetComponentTypeHandle<CullingInfo>(true),
                    m_transform = GetComponentLookup<Game.Objects.Transform>(true),
                    m_iTransform = GetComponentLookup<InterpolatedTransform>(true),
                    m_weMainLookup = GetComponentLookup<WETextDataMain>(true),
                    m_weMeshLookup = GetComponentLookup<WETextDataMesh>(false),
                    m_weMaterialLookup = GetComponentLookup<WETextDataMaterial>(true),
                    m_weTemplateUpdaterLookup = GetBufferLookup<WETemplateUpdater>(true),
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
                    m_weVariablesLookup = GetBufferLookup<WETextDataVariable>(true),
                    //doLog = dumpNextFrame,
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
            public WETextDataTransform transform;
            public WETextDataMesh mesh;
            public WETextDataMaterial material;
            public Matrix4x4 transformMatrix;
            public FixedString512Bytes variables;
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