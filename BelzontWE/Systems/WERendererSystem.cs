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
            if (WriteEverywhereCS2Mod.WeData.TempDisableRendering) return;
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
                    if (!EntityManager.TryGetComponent(item.textDataEntity, out WETextDataMesh mesh)
                    || !EntityManager.TryGetComponent(item.textDataEntity, out WETextDataMain main)
                    || !EntityManager.TryGetComponent(item.textDataEntity, out WETextDataMaterial materialData)
                    || item.transformMatrix == default
                    || main.nextUpdateFrame == 0) continue;

                    bool willCheckUpdate = ((FrameCounter + item.textDataEntity.Index) & 0x1f) == 0;

                    bool isPlaceholder = false;




                    bool doRender = mesh.TextType switch
                    {
                        WESimulationTextType.Placeholder => m_pickerTool.IsSelected,
                        WESimulationTextType.MatrixTransform => false,
                        _ => true,
                    };
                    if (doRender)
                    {
                        IBasicRenderInformation bri = mesh.RenderInformation;
                        if (bri != null && (!bri.IsValid() || bri.Guid != mesh.BriGuid))
                        {
                            mesh.ResetBri();
                            bri = null;
                            EntityManager.SetComponentData(item.textDataEntity, mesh);
                        }
                        if (bri == null)
                        {
                            switch (mesh.TextType)
                            {
                                case WESimulationTextType.Text:
                                case WESimulationTextType.Image:
                                    if (willCheckUpdate && mesh.ValueData.EffectiveValue.Length > 0 && !EntityManager.HasEnabledComponent<WEWaitingRendering>(item.textDataEntity))
                                    {
                                        if (!EntityManager.HasComponent<WEWaitingRendering>(item.textDataEntity))
                                        {
                                            cmd.AddComponent<WEWaitingRendering>(item.textDataEntity);
                                        }
                                        else
                                        {
                                            EntityManager.SetComponentEnabled<WEWaitingRendering>(item.textDataEntity, true);
                                        }
                                        if (dumpNextFrame)
                                        {
                                            LogUtils.DoInfoLog($"DUMP! G = {item.geometryEntity} E =  {item.textDataEntity}; T: {main.TargetEntity} P: {main.ParentEntity}\n{main.ItemName} - {mesh.TextType} - '{mesh.ValueData.EffectiveValue}'\nMARKED TO RE-RENDER");
                                        }
                                    }
                                    goto case WESimulationTextType.Placeholder;
                                case WESimulationTextType.Placeholder:
                                    doRender = m_pickerTool.IsSelected;
                                    isPlaceholder = true;
                                    goto case WESimulationTextType.WhiteTexture;
                                case WESimulationTextType.WhiteTexture:
                                case WESimulationTextType.WhiteCube:
                                    bri = WEAtlasesLibrary.GetWhiteTextureBRI();
                                    break;
                            }
                        }

                        var brii = bri as PrimitiveRenderInformation;
                        bool materialChanged = false;
                        if (doRender && (brii is null || brii.m_refText != ""))
                        {
                            Material[] ownMaterial = null;
                            if (isPlaceholder) ownMaterial = WEAtlasesLibrary.DefaultMaterialWhiteTexture();
                            else materialChanged = materialData.GetOwnMaterial(ref mesh, brii?.CubeCharCoordinates, out ownMaterial);

                            var bri2 = bri as PrimitiveRenderInformation;
                            var meshCount = bri2 is null || mesh.TextType == WESimulationTextType.WhiteCube ? 1 : bri2.MeshCount(materialData.Shader);

                            var baseMatrix = item.transformMatrix;
                            if (EntityManager.HasComponent<InterpolatedTransform>(item.geometryEntity))
                            {
                                var transformInterpolated = EntityManager.GetComponentData<InterpolatedTransform>(item.geometryEntity);
                                var interpolatedMatrix = Matrix4x4.TRS(transformInterpolated.m_Position, transformInterpolated.m_Rotation, Vector3.one);
                                baseMatrix = interpolatedMatrix * item.transformMatrix;
                            }
                            if (m_pickerTool.Enabled && (m_pickerController.CameraLocked.Value || m_pickerController.CurrentSubEntity.Value != m_pickerController.CurrentItemMatrixEntity)
                                                     && m_pickerController.CurrentSubEntity.Value == item.textDataEntity
                                                     && item.transformMatrix.ValidTRS())
                            {
                                m_pickerController.SetCurrentTargetMatrix(m_pickerController.CurrentSubEntity.Value, baseMatrix);
                            }
                            for (int i = 0; i < meshCount; i++)
                            {
                                var geomMesh = bri2 is not null ? (mesh.TextType == WESimulationTextType.WhiteCube ? bri2.MeshCube[0] : bri2.GetMesh(materialData.Shader, i)) : bri.GetMesh(materialData.Shader);
                                var effectiveMatrix = bri2 is null ? baseMatrix : baseMatrix * bri2.GetMeshTranslation(materialData.Shader, i);

                                Graphics.DrawMesh(geomMesh, effectiveMatrix, ownMaterial[i], 0, null, 0, null, ShadowCastingMode.TwoSided, true, null, LightProbeUsage.BlendProbes);
                                if (m_pickerController.IsValidEditingItem() && m_pickerController.ShowProjectionCube.Value && m_pickerController.CurrentSubEntity.Value == item.textDataEntity && materialData.Shader == WEShader.Decal)
                                {
                                    if (dumpNextFrame) LogUtils.DoInfoLog($"DUMP! DRAWING Extra mesh");
                                    Graphics.DrawMesh(geomMesh, effectiveMatrix, WEAtlasesLibrary.DefaultMaterialSemiTransparent(), 0, null, 0, null, false, false);
                                }
#if DEBUG
                                DrawCallsLastFrame++;
#endif
                                if (dumpNextFrame) LogUtils.DoInfoLog($"DUMP! G = {item.geometryEntity} E = {item.textDataEntity}; T: {main.TargetEntity} P: {main.ParentEntity}\n{main.ItemName} - {mesh.TextType} - '{mesh.ValueData.EffectiveValue}'\nBRI: {geomMesh?.vertices?.Length} | {bri.IsValid()} | M= {item.transformMatrix}");
                            }

                            if (materialChanged) EntityManager.SetComponentData(item.textDataEntity, materialData);
                        }
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