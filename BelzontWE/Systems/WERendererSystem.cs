using Belzont.Interfaces;
using Belzont.Utils;
using BelzontWE.Font.Utility;
using BelzontWE.Sprites;
using Game;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering;
using Colossal.Entities;
using Game.Rendering;


#if BURST
using UnityEngine.Scripting;
#else
#endif

namespace BelzontWE
{

    public partial class WERendererSystem : SystemBase
    {
        private WEWorldPickerController m_pickerController;
        private WEWorldPickerTool m_pickerTool;
        internal static bool dumpNextFrame;
        private EndFrameBarrier m_endFrameBarrier;
        private WEPreCullingSystem m_wePreCullSys;

#if DEBUG
        public uint DrawCallsLastFrame { get; private set; } = 0;
#endif
        private int FrameCounter { get; set; } = 0;
#if BURST
        [Preserve]
#endif
        protected unsafe override void OnCreate()
        {
            base.OnCreate();
            m_endFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
            m_pickerController = World.GetExistingSystemManaged<WEWorldPickerController>();
            m_pickerTool = World.GetExistingSystemManaged<WEWorldPickerTool>();
            m_wePreCullSys = World.GetExistingSystemManaged<WEPreCullingSystem>();
            RenderPipelineManager.beginContextRendering += Render;
        }
#if BURST
        [Preserve]
#endif
        protected override void OnDestroy()
        {
            base.OnDestroy();
            RenderPipelineManager.beginContextRendering -= Render;
        }
#if BURST
        [Preserve]
#endif
        private void Render(ScriptableRenderContext context, List<Camera> cameras)
        {
            FrameCounter++;

#if DEBUG
            DrawCallsLastFrame = 0;
#endif
            if (m_wePreCullSys.m_availToDraw.Length > 0)
            {
                var count = m_wePreCullSys.m_availToDraw.Length;
                EntityCommandBuffer cmd = m_endFrameBarrier.CreateCommandBuffer();
                for (int j = 0; j < count; j++)
                {
                    var item = m_wePreCullSys.m_availToDraw[j];
                    bool willCheckUpdate = ((FrameCounter + item.textDataEntity.Index) & 0x1f) == 0;
                    ref var transform = ref item.transform;
                    ref var main = ref item.main;
                    ref var material = ref item.material;
                    ref var mesh = ref item.mesh;

                    if (willCheckUpdate && !EntityManager.HasEnabledComponent<WETextDataDirtyFormulae>(item.textDataEntity))
                    {
                        main.CheckDirtyFormulae(item.geometryEntity, item.textDataEntity, item.variables, cmd);
                    }

                    if (item.transformMatrix == default) continue;

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
                            if (willCheckUpdate && mesh.IsDirty() && !EntityManager.HasEnabledComponent<WEWaitingRendering>(item.textDataEntity))
                            {
                                if (dumpNextFrame) LogUtils.DoInfoLog($"DUMP! +WEWaitingRendering");
                                cmd.AddComponent<WEWaitingRendering>(item.textDataEntity);
                                cmd.SetComponentEnabled<WEWaitingRendering>(item.textDataEntity, true);
                            }
                            break;
                        case WESimulationTextType.Placeholder:
                            if (willCheckUpdate && mesh.IsTemplateDirty())
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
                                    if (willCheckUpdate && mesh.ValueData.EffectiveValue.Length > 0 && !EntityManager.HasEnabledComponent<WEWaitingRendering>(item.textDataEntity))
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

                            var baseMatrix = item.transformMatrix;
                            if (EntityManager.HasComponent<InterpolatedTransform>(item.geometryEntity))
                            {
                                var transformInterpolated = EntityManager.GetComponentData<InterpolatedTransform>(item.geometryEntity);
                                var interpolatedMatrix = Matrix4x4.TRS(transformInterpolated.m_Position, transformInterpolated.m_Rotation, Vector3.one);
                                baseMatrix = interpolatedMatrix * item.transformMatrix;
                            }

                            for (int i = 0; i < meshCount; i++)
                            {
                                var geomMesh = bri2 is not null ? (mesh.TextType == WESimulationTextType.WhiteCube ? bri2.MeshCube[0] : bri2.GetMesh(item.material.Shader, i)) : bri.GetMesh(item.material.Shader);
                                var effectiveMatrix = bri2 is null ? baseMatrix : baseMatrix * bri2.GetMeshTranslation(item.material.Shader, i);

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

        protected override void OnUpdate()
        {
        }
    }
}