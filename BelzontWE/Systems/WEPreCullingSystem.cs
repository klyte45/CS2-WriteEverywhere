using Colossal.Entities;
using Colossal.Mathematics;
using Game.Rendering;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace BelzontWE
{
    public partial class WEPreCullingSystem : SystemBase
    {
        private CameraUpdateSystem m_CameraUpdateSystem;
        private RenderingSystem m_RenderingSystem;
        internal NativeArray<WERenderData> m_availToDraw;
        private WEWorldPickerController m_pickerController;
        private PreCullingSystem m_preCullingSystem;
        private WEWorldPickerTool m_pickerTool;
        internal const char VARIABLE_ITEM_SEPARATOR = '↓';
        internal const char VARIABLE_KV_SEPARATOR = '→';

        private bool ready;

        internal struct WERenderData
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

        private int frameCounter = 0;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_CameraUpdateSystem = World.GetExistingSystemManaged<CameraUpdateSystem>();
            m_RenderingSystem = World.GetExistingSystemManaged<RenderingSystem>();
            m_pickerTool = World.GetOrCreateSystemManaged<WEWorldPickerTool>();
            m_pickerController = World.GetOrCreateSystemManaged<WEWorldPickerController>();
            m_preCullingSystem = World.GetOrCreateSystemManaged<PreCullingSystem>();
            ready = true;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (m_availToDraw.IsCreated)
            {
                m_availToDraw.Dispose();
            }
        }

        protected override void OnUpdate()
        {
            if (!ready) return;
            if (WriteEverywhereCS2Mod.WeData.TempDisableRendering) return;

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

            var data = m_preCullingSystem.GetCullingData(true, out JobHandle deps);
            if (data.IsEmpty) return;
            var commandBuffer = new EntityCommandBuffer(Allocator.Persistent);
            var queueRender = new NativeQueue<WERenderData>(Allocator.Persistent);
            WERenderingJob cullingActionJob = new()
            {

                m_CullingActions = data,
                m_WEDrawingLookup = GetComponentLookup<WEDrawing>(true),
                m_CommandBuffer = commandBuffer.AsParallelWriter(),
                m_EntityType = GetEntityTypeHandle(),
                m_transform = GetComponentLookup<Game.Objects.Transform>(true),
                m_weMainLookup = GetComponentLookup<WETextDataMain>(true),
                m_weMeshLookup = GetComponentLookup<WETextDataMesh>(true),
                m_weMaterialLookup = GetComponentLookup<WETextDataMaterial>(true),
                m_weTemplateUpdaterLookup = GetBufferLookup<WETemplateUpdater>(true),
                m_weTemplateForPrefabLookup = GetComponentLookup<WETemplateForPrefab>(true),
                m_LodParameters = m_LodParameters,
                m_CameraPosition = m_CameraPosition,
                m_CameraDirection = m_CameraDirection,
                availToDraw = queueRender.AsParallelWriter(),
                isAtWeEditor = m_pickerTool.IsSelected,
                m_selectedSubEntity = m_pickerController.CurrentSubEntity.Value,
                m_selectedEntity = m_pickerController.CurrentEntity.Value,
                m_weSubRefLookup = GetBufferLookup<WESubTextRef>(true),
                m_weVariablesLookup = GetBufferLookup<WETextDataVariable>(true),
                m_weTransformLookup = GetComponentLookup<WETextDataTransform>(true),
                m_weWaitingRenderingLookup = GetComponentLookup<WEWaitingRendering>(true),
                m_weDirtyFormulae = GetComponentLookup<WETextDataDirtyFormulae>(false),
                m_interpolatedTransformLkp = GetComponentLookup<InterpolatedTransform>(true),
                frameCount = UnityEngine.Time.frameCount,
                minLodUpdateSetting = Mathf.CeilToInt(WriteEverywhereCS2Mod.WeData.RequiredLodForFormulaesUpdate)
            };



            Dependency = cullingActionJob.Schedule(data.Length, 1, deps);
            Dependency.Complete();
            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();

            if (m_availToDraw.IsCreated) m_availToDraw.Dispose();
            m_availToDraw = queueRender.ToArray(Allocator.Persistent);
            queueRender.Dispose();
#if DEBUG
            for (int i = 0; i < m_availToDraw.Length; i++)
            {
                EntityManager.SetComponentData(m_availToDraw[i].textDataEntity, m_availToDraw[i].mesh);
            }
#endif
        }

        private float GetLevelOfDetail(float levelOfDetail, IGameCameraController cameraController)
        {
            if (cameraController != null)
            {
                levelOfDetail *= 1f - 1f / (2f + 0.01f * cameraController.zoom);
            }
            return levelOfDetail;
        }


        private readonly FixedString32Bytes indexStartString = new($"$idx{VARIABLE_KV_SEPARATOR}");
#if BURST
        [BurstCompile]
#endif
        private struct WERenderingJob : IJobParallelFor
        {
            public ComponentLookup<WETextDataMain> m_weMainLookup;
            public BufferLookup<WESubTextRef> m_weSubRefLookup;
            public BufferLookup<WETextDataVariable> m_weVariablesLookup;
            public BufferLookup<WETemplateUpdater> m_weTemplateUpdaterLookup;
            public ComponentLookup<WETemplateForPrefab> m_weTemplateForPrefabLookup;
            public ComponentLookup<WEWaitingRendering> m_weWaitingRenderingLookup;
            public ComponentLookup<Game.Objects.Transform> m_transform;
            public ComponentLookup<InterpolatedTransform> m_interpolatedTransformLkp;
            public float4 m_LodParameters;
            public float3 m_CameraPosition;
            public float3 m_CameraDirection;
            public EntityTypeHandle m_EntityType;
            public NativeQueue<WERenderData>.ParallelWriter availToDraw;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
            public Entity m_selectedSubEntity;
            public Entity m_selectedEntity;
            public bool isAtWeEditor;
            public ComponentLookup<WETextDataMaterial> m_weMaterialLookup;
            public ComponentLookup<WETextDataMesh> m_weMeshLookup;
            public ComponentLookup<WETextDataTransform> m_weTransformLookup;
            public ComponentLookup<WEDrawing> m_WEDrawingLookup;
            public ComponentLookup<WETextDataDirtyFormulae> m_weDirtyFormulae;
            public NativeList<PreCullingData> m_CullingActions;
            public int frameCount;
            public int minLodUpdateSetting;
            public FixedString32Bytes indexStartString;

            private static readonly Bounds3 whiteTextureBounds = new(new(-.5f, -.5f, 0), new(.5f, .5f, 0));
            private static readonly Bounds3 whiteCubeBounds = new(new(-.5f, -.5f, -.5f), new(.5f, .5f, .5f));


            public void Execute(int index)
            {
                var cullingAction = m_CullingActions[index];
                if ((cullingAction.m_Flags & PreCullingFlags.PassedCulling) != 0)
                {
                    var entity = cullingAction.m_Entity;
                    if ((!m_weTemplateForPrefabLookup.TryGetComponent(entity, out var prefabWeLayout) || prefabWeLayout.childEntity == Entity.Null) & (!m_weSubRefLookup.TryGetBuffer(entity, out var subLayout) || subLayout.Length == 0)) return;


                    if (m_WEDrawingLookup.HasComponent(cullingAction.m_Entity))
                    {
                        m_CommandBuffer.SetComponentEnabled<WEDrawing>(index, entity, true);
                    }
                    else
                    {
                        m_CommandBuffer.AddComponent<WEDrawing>(index, entity);
                    }




                    Matrix4x4 itemBaseMatrix;
                    Matrix4x4 geomMatrix;
                    if (m_transform.TryGetComponent(entity, out var transform2))
                    {
                        var positionRef = transform2.m_Position;
                        var rotationRef = transform2.m_Rotation;

                        geomMatrix = itemBaseMatrix = Matrix4x4.TRS(positionRef, rotationRef, Vector3.one);
                    }
                    else
                    {
                        return;
                    }


                    if (m_interpolatedTransformLkp.HasComponent(entity))
                    {
                        itemBaseMatrix = Matrix4x4.identity;
                    }
                    else
                    {
                        geomMatrix = Matrix4x4.identity;
                    }
                    FixedString512Bytes variables = new();
                    PopulateVars(entity, ref variables);
                    if (prefabWeLayout.childEntity != Entity.Null)
                    {
                        DrawTree(entity, prefabWeLayout.childEntity, itemBaseMatrix, geomMatrix, index, variables, 0);
                    }
                    if (subLayout.IsCreated)
                    {
                        for (int j = 0; j < subLayout.Length; j++)
                        {
                            DrawTree(entity, subLayout[j].m_weTextData, itemBaseMatrix, geomMatrix, index, variables, 0);
                        }
                    }

                    return;
                }
                else
                {
                    FailedCulling(cullingAction, index);
                }
            }

            private void FailedCulling(PreCullingData cullingAction, int index)
            {
                if (m_WEDrawingLookup.HasComponent(cullingAction.m_Entity))
                {
                    m_CommandBuffer.SetComponentEnabled<WEDrawing>(index, cullingAction.m_Entity, false);
                }
            }

            private unsafe void PopulateVars(Entity entity, ref FixedString512Bytes varStr)
            {
                if (m_weVariablesLookup.TryGetBuffer(entity, out var variableBuffer) && !variableBuffer.IsEmpty)
                {
                    for (int i = 0; i < variableBuffer.Length; i++)
                    {
                        varStr.Append(variableBuffer[i].Key);
                        varStr.Append(VARIABLE_KV_SEPARATOR);
                        varStr.Append(variableBuffer[i].Value);
                        varStr.Append(VARIABLE_ITEM_SEPARATOR);
                    }
                }
            }

            private void CheckMesh(ref WETextDataMesh mesh, Entity nextEntity, int index)
            {
                switch (mesh.TextType)
                {
                    case WESimulationTextType.Text:
                    case WESimulationTextType.Image:
                        if (mesh.IsDirty() && !m_weWaitingRenderingLookup.HasEnabledComponent(nextEntity))
                        {
                            if (!m_weWaitingRenderingLookup.HasComponent(nextEntity))
                            {
                                m_CommandBuffer.AddComponent<WEWaitingRendering>(index, nextEntity);
                            }
                            else
                            {
                                m_CommandBuffer.SetComponentEnabled<WEWaitingRendering>(index, nextEntity, true);
                            }
                        }
                        break;
                    case WESimulationTextType.Placeholder:
                        if (mesh.IsTemplateDirty() && !m_weWaitingRenderingLookup.HasEnabledComponent(nextEntity))
                        {
                            if (!m_weWaitingRenderingLookup.HasComponent(nextEntity))
                            {
                                m_CommandBuffer.AddComponent<WEWaitingRendering>(index, nextEntity);
                            }
                            else
                            {
                                m_CommandBuffer.SetComponentEnabled<WEWaitingRendering>(index, nextEntity, true);
                            }
                        }
                        return;
                }
            }

            private void DrawTree(Entity geometryEntity, Entity nextEntity, Matrix4x4 prevMatrix, Matrix4x4 geomMatrix, int unfilteredChunkIndex, in FixedString512Bytes variables, int nthCall, bool parentIsPlaceholder = false)
            {
                if (nthCall >= 16) return;
                if (!m_weMeshLookup.TryGetComponent(nextEntity, out var mesh))
                {
                    DestroyRecursive(ref this, nextEntity, unfilteredChunkIndex);
                    return;
                }
                var transform = m_weTransformLookup[nextEntity];
                if (!m_weDirtyFormulae.HasComponent(nextEntity))
                {
                    // Need to make a copy here since we're modifying it
                    var mutableVars = variables;
                    PopulateVars(nextEntity, ref mutableVars);
                    m_CommandBuffer.AddComponent(unfilteredChunkIndex, nextEntity, new WETextDataDirtyFormulae
                    {
                        geometry = geometryEntity,
                        vars = mutableVars
                    });
                    return;
                }
                CheckMesh(ref mesh, nextEntity, unfilteredChunkIndex);

                if (transform.useFormulaeToCheckIfDraw && !transform.MustDraw)
                {
                    var mutableVars = variables;
                    PopulateVars(nextEntity, ref mutableVars);
                    CheckForUpdates(geometryEntity, nextEntity, unfilteredChunkIndex, in mutableVars, 2000);
                    return;
                }

                // Optimize: Make single copy for this level, reuse for all children
                var currentVars = variables;
                PopulateVars(nextEntity, ref currentVars);
                switch (mesh.TextType)
                {
                    case WESimulationTextType.MatrixTransform:
                        if (m_weSubRefLookup.TryGetBuffer(nextEntity, out var subLayoutSc))
                        {
                            var matrix = prevMatrix * Matrix4x4.TRS(mesh.OffsetPositionFormulae.EffectiveValue, Quaternion.Euler(mesh.OffsetRotationFormulae.EffectiveValue), mesh.ScaleFormulae.EffectiveValue);
                            CheckForUpdates(geometryEntity, nextEntity, unfilteredChunkIndex, in currentVars, 2000);
                            for (int j = 0; j < subLayoutSc.Length; j++)
                            {
                                DrawTree(geometryEntity, subLayoutSc[j].m_weTextData, matrix, geomMatrix, unfilteredChunkIndex, in currentVars, nthCall + 1);
                            }
                        }
                        break;
                    case WESimulationTextType.Archetype:
                        if (m_weSubRefLookup.TryGetBuffer(nextEntity, out var subLayout2))
                        {
                            prevMatrix *= Matrix4x4.TRS(new Vector3(0, 0, .001f), default, Vector3.one);
                            for (int j = 0; j < subLayout2.Length; j++)
                            {
                                DrawTree(geometryEntity, subLayout2[j].m_weTextData, prevMatrix, geomMatrix, unfilteredChunkIndex, in currentVars, nthCall + 1);
                            }
                        }
                        return;
                    case WESimulationTextType.Placeholder:
                        {
                            if (!m_weTemplateUpdaterLookup.TryGetBuffer(nextEntity, out var updaterBuff))
                            {
                                if (m_weWaitingRenderingLookup.HasComponent(nextEntity))
                                {

                                    m_CommandBuffer.SetComponentEnabled<WEWaitingRendering>(unfilteredChunkIndex, nextEntity, true);
                                }
                                else
                                {
                                    m_CommandBuffer.AddComponent<WEWaitingRendering>(unfilteredChunkIndex, nextEntity);
                                }
                                return;
                            }

                            int lod = CalculateLod(whiteTextureBounds, ref mesh, ref transform, geomMatrix * prevMatrix, out int minLod, ref this);
                            CheckForUpdates(geometryEntity, nextEntity, unfilteredChunkIndex, in currentVars, lod);
                            if (!mesh.ValueData.InitializedEffectiveText || lod >= minLod || (isAtWeEditor && geometryEntity == m_selectedEntity))
                            {
                                var scale2 = transform.scale;
                                // Optimize: Use already loaded mesh instead of re-looking up
                                var effectiveOffsetPosition = GetEffectiveOffsetPosition(mesh, transform);

                                availToDraw.Enqueue(new WERenderData
                                {
                                    transform = transform,
                                    textDataEntity = nextEntity,
                                    geometryEntity = geometryEntity,
                                    main = m_weMainLookup[nextEntity],
                                    material = m_weMaterialLookup[nextEntity],
                                    mesh = mesh,
                                    transformMatrix = prevMatrix * Matrix4x4.TRS(effectiveOffsetPosition + (float3)Matrix4x4.Rotate(transform.offsetRotation).MultiplyPoint(new float3(0, 0, -.001f)), transform.offsetRotation, scale2),
                                    variables = currentVars
                                });
                            }
                            if (transform.MustDraw)
                            {
                                // Optimize: Pre-calculate transform matrix outside loop
                                var childBaseMatrix = prevMatrix * Matrix4x4.TRS(transform.offsetPosition, transform.offsetRotation, Vector3.one);
                                for (int i = 0; i < updaterBuff.Length; i++)
                                {
                                    var updater = updaterBuff[i];
                                    if (updater.childEntity.Index < 0) continue;
                                    // Optimize: Build variable string - only copy once per child that needs it
                                    var layoutVars = new FixedString512Bytes(currentVars);
                                    layoutVars.Append(indexStartString);
                                    layoutVars.Append(i);
                                    layoutVars.Append(VARIABLE_ITEM_SEPARATOR);
                                    DrawTree(geometryEntity, updater.childEntity, childBaseMatrix, geomMatrix, unfilteredChunkIndex, in layoutVars, nthCall + 1, true);
                                }
                            }
                        }
                        break;
                    case WESimulationTextType.WhiteCube:
                        {
                            // Optimize: Cache material and main lookups
                            var cubeMaterial = m_weMaterialLookup[nextEntity];
                            var effRot = (Quaternion)transform.offsetRotation;
                            // Optimize: Calculate effectiveOffsetPosition using already loaded mesh
                            var effectiveOffsetPosition = GetEffectiveOffsetPosition(mesh, transform);

                            var WTmatrix = prevMatrix * Matrix4x4.TRS(effectiveOffsetPosition, effRot, Vector3.one) * Matrix4x4.Scale(transform.scale.xyz);
                            var lumMultiplier = GetEmissiveMultiplier(ref cubeMaterial);
                            int lod = CalculateLod(whiteCubeBounds * lumMultiplier, ref mesh, ref transform, geomMatrix * WTmatrix, out int minLod, ref this);
                            CheckForUpdates(geometryEntity, nextEntity, unfilteredChunkIndex, in currentVars, lod);
                            if (lod >= minLod || (isAtWeEditor && geometryEntity == m_selectedEntity))
                            {
                                // Optimize: Reuse mesh variable instead of re-looking up
                                availToDraw.Enqueue(new WERenderData
                                {
                                    transform = transform,
                                    textDataEntity = nextEntity,
                                    geometryEntity = geometryEntity,
                                    main = m_weMainLookup[nextEntity],
                                    material = cubeMaterial,
                                    mesh = mesh,
                                    transformMatrix = WTmatrix,
                                    variables = currentVars
                                });
                            }

                            if (m_weSubRefLookup.TryGetBuffer(nextEntity, out var subLayoutWt))
                            {
                                var itemMatrix = prevMatrix * Matrix4x4.TRS(effectiveOffsetPosition + (float3)Matrix4x4.Rotate(transform.offsetRotation).MultiplyPoint(new float3(0, 0, mesh.childrenRefersToFrontFace ? (transform.scale.z * .5f) + .001f : .001f)), transform.offsetRotation, Vector3.one);
                                for (int j = 0; j < subLayoutWt.Length; j++)
                                {
                                    DrawTree(geometryEntity, subLayoutWt[j].m_weTextData, itemMatrix, geomMatrix, unfilteredChunkIndex, in currentVars, nthCall + 1);
                                }
                            }
                        }
                        return;
                    case WESimulationTextType.WhiteTexture:
                        {
                            // Optimize: Cache material lookup and calculations
                            var textureMaterial = m_weMaterialLookup[nextEntity];
                            var isDecal = textureMaterial.CheckIsDecal(mesh);
                            var effRot = isDecal ? ((Quaternion)transform.offsetRotation) * Quaternion.Euler(new Vector3(-90, 180, 0)) : (Quaternion)transform.offsetRotation;
                            // Optimize: Use already loaded mesh instead of re-looking up
                            var effectiveOffsetPosition = GetEffectiveOffsetPosition(mesh, transform);

                            var WTmatrix = prevMatrix * Matrix4x4.TRS(effectiveOffsetPosition, effRot, Vector3.one) * Matrix4x4.Scale(isDecal ? transform.scale.xzy : new float3(transform.scale.xy, math.sign(transform.scale.z)));
                            var lumMultiplier = GetEmissiveMultiplier(ref textureMaterial);
                            int lod = CalculateLod(whiteTextureBounds * lumMultiplier, ref mesh, ref transform, geomMatrix * WTmatrix, out int minLod, ref this);
                            CheckForUpdates(geometryEntity, nextEntity, unfilteredChunkIndex, in currentVars, lod);
                            if (lod >= minLod || (isAtWeEditor && geometryEntity == m_selectedEntity))
                            {
                                // Optimize: Reuse mesh variable
                                availToDraw.Enqueue(new WERenderData
                                {
                                    transform = transform,
                                    textDataEntity = nextEntity,
                                    geometryEntity = geometryEntity,
                                    main = m_weMainLookup[nextEntity],
                                    material = textureMaterial,
                                    mesh = mesh,
                                    transformMatrix = WTmatrix,
                                    variables = currentVars
                                });
                            }

                            if (m_weSubRefLookup.TryGetBuffer(nextEntity, out var subLayoutWt))
                            {
                                // Optimize: Cache shader check result
                                var zOffset = textureMaterial.Shader == WEShader.Decal ? .002f : .001f;
                                var itemMatrix = prevMatrix * Matrix4x4.TRS(effectiveOffsetPosition + (float3)Matrix4x4.Rotate(transform.offsetRotation).MultiplyPoint(new float3(0, 0, zOffset)), transform.offsetRotation, Vector3.one);
                                for (int j = 0; j < subLayoutWt.Length; j++)
                                {
                                    DrawTree(geometryEntity, subLayoutWt[j].m_weTextData, itemMatrix, geomMatrix, unfilteredChunkIndex, in currentVars, nthCall + 1);
                                }
                            }
                        }
                        return;
                    default:
                        {
                            if (m_weTemplateUpdaterLookup.HasBuffer(nextEntity))
                            {
                                if (m_weWaitingRenderingLookup.HasComponent(nextEntity))
                                {

                                    m_CommandBuffer.SetComponentEnabled<WEWaitingRendering>(unfilteredChunkIndex, nextEntity, true);
                                }
                                else
                                {
                                    m_CommandBuffer.AddComponent<WEWaitingRendering>(unfilteredChunkIndex, nextEntity);
                                }
                                return;
                            }
                            var scale = transform.scale;
                            var defaultMaterial = m_weMaterialLookup[nextEntity];
                            var isDecal = defaultMaterial.CheckIsDecal(mesh);
                            // Optimize: Cache frequently accessed values
                            var meshTextType = mesh.TextType;
                            var hasBRI = mesh.HasBRI;

                            if (hasBRI)
                            {
                                if (meshTextType == WESimulationTextType.Image)
                                {
                                    var briWidth = mesh.BriWidthMetersUnscaled;
                                    if (transform.useAbsoluteSizeEditing)
                                    {
                                        scale.x /= briWidth;
                                    }
                                    if (isDecal)
                                    {
                                        scale.x /= briWidth;
                                    }
                                }
                                if (meshTextType == WESimulationTextType.Text && mesh.MaxWidthMeters.EffectiveValue > 0 && mesh.BriWidthMetersUnscaled * scale.x > mesh.MaxWidthMeters.EffectiveValue)
                                {
                                    var ratio = mesh.MaxWidthMeters.EffectiveValue / mesh.BriWidthMetersUnscaled;
                                    scale.x = ratio;
                                    if (mesh.RescaleHeightOnTextOverflow)
                                    {
                                        scale.y = ratio;
                                    }
                                }
                            }
                            // Optimize: Use already loaded mesh instead of re-looking up
                            var refPos = GetEffectiveOffsetPosition(mesh, transform.offsetPosition, transform.PivotAsFloat3, scale);
                            var refRot = parentIsPlaceholder ? default : transform.offsetRotation;
                            var effRot = (parentIsPlaceholder ? quaternion.identity : (Quaternion)refRot) * (isDecal ? Quaternion.Euler(new Vector3(-90, 180, 0)) : (Quaternion)quaternion.identity);
                            var matrix = prevMatrix * Matrix4x4.TRS(refPos, effRot, Vector3.one)
                                * Matrix4x4.Scale(isDecal ? (scale.xzy * new float3(meshTextType == WESimulationTextType.Image ? mesh.BriWidthMetersUnscaled : 1, 1, 1)) : new float3(scale.xy, math.sign(scale.z)));
                            var zeroedBounds = (Vector3)(mesh.Bounds.min - mesh.Bounds.max) == default;
                            var invalidBri = (mesh.EffectiveText.Length >= 0 && zeroedBounds);
                            if (hasBRI || invalidBri)
                            {
                                if (!float.IsNaN(matrix.m00) && !float.IsInfinity(matrix.m00))
                                {
                                    int minLod = -1;
                                    float lumMultiplier = GetEmissiveMultiplier(ref defaultMaterial);
                                    int lod = invalidBri ? 0 : CalculateLod(mesh.Bounds * lumMultiplier, ref mesh, ref transform, geomMatrix * matrix, out minLod, ref this);
                                    CheckForUpdates(geometryEntity, nextEntity, unfilteredChunkIndex, in currentVars, lod);
                                    if (lod >= minLod || (isAtWeEditor && geometryEntity == m_selectedEntity))
                                    {
                                        // Optimize: Reuse material and mesh variables
                                        availToDraw.Enqueue(new WERenderData
                                        {
                                            transform = transform,
                                            textDataEntity = nextEntity,
                                            geometryEntity = geometryEntity,
                                            main = m_weMainLookup[nextEntity],
                                            material = defaultMaterial,
                                            mesh = mesh,
                                            transformMatrix = matrix,
                                            variables = currentVars
                                        });
                                    }
                                }
                                else
                                {
                                    CheckForUpdates(geometryEntity, nextEntity, unfilteredChunkIndex, in currentVars, 2000);
                                }
                            }
                            else
                            {
                                CheckForUpdates(geometryEntity, nextEntity, unfilteredChunkIndex, in currentVars, 2000);
                                // Optimize: Reuse material and mesh variables
                                availToDraw.Enqueue(new WERenderData
                                {
                                    transform = transform,
                                    textDataEntity = nextEntity,
                                    geometryEntity = geometryEntity,
                                    main = m_weMainLookup[nextEntity],
                                    material = defaultMaterial,
                                    mesh = mesh,
                                    transformMatrix = matrix,
                                    variables = currentVars
                                });
                            }
                            if (m_weSubRefLookup.TryGetBuffer(nextEntity, out var subLayout))
                            {
                                // Optimize: Cache shader check
                                var zOffset = defaultMaterial.Shader == WEShader.Decal ? .002f : .001f;
                                var itemMatrix = prevMatrix * Matrix4x4.TRS(refPos + (float3)Matrix4x4.Rotate(refRot).MultiplyPoint(new float3(0, 0, zOffset)), refRot, Vector3.one);
                                for (int j = 0; j < subLayout.Length; j++)
                                {
                                    DrawTree(geometryEntity, subLayout[j].m_weTextData, itemMatrix, geomMatrix, unfilteredChunkIndex, in currentVars, nthCall + 1);
                                }
                            }
                        }
                        break;
                }
            }

            private void CheckForUpdates(Entity geometryEntity, Entity nextEntity, int unfilteredChunkIndex, in FixedString512Bytes variables, int currentLod)
            {
                if (minLodUpdateSetting > currentLod) return;
                if (m_weDirtyFormulae.HasEnabledComponent(nextEntity)) return;
                if (((frameCount + nextEntity.Index) & 0x1f) == 0 && m_weMainLookup[nextEntity].nextUpdateFrame < frameCount)
                {
                    m_CommandBuffer.SetComponent(unfilteredChunkIndex, nextEntity, new WETextDataDirtyFormulae
                    {
                        geometry = geometryEntity,
                        vars = variables
                    });
                    m_CommandBuffer.SetComponentEnabled<WETextDataDirtyFormulae>(unfilteredChunkIndex, nextEntity, true);
                }
            }

            private float GetEmissiveMultiplier(ref WETextDataMaterial material) => 1 << (int)math.ceil(math.clamp(material.EmissiveIntensityEffective, 0, 10) * .5f);

            private readonly float3 GetEffectiveOffsetPosition(WETextDataMesh meshData, WETextDataTransform transform)
            {
                return GetEffectiveOffsetPosition(meshData, transform.offsetPosition, transform.PivotAsFloat3, transform.scale);
            }

            private readonly float3 GetEffectiveOffsetPosition(WETextDataMesh meshData, float3 offsetPosition, float3 pivot, float3 scale)
            {
                var effectiveOffsetPosition = offsetPosition;
                var meshSize = meshData.Bounds.max - meshData.Bounds.min;
                effectiveOffsetPosition += (pivot - new float3(.5f, .5f, .5f)) * meshSize * scale;

                return effectiveOffsetPosition;
            }

            private int CalculateLod(Bounds3 meshBounds, ref WETextDataMesh meshData, ref WETextDataTransform transformData, Matrix4x4 matrix, out int minLod, ref WERenderingJob job)
            {
                var refBounds = new Bounds3(matrix.MultiplyPoint(meshBounds.min), matrix.MultiplyPoint(meshBounds.max));
                var minDist = RenderingUtils.CalculateMinDistance(refBounds, job.m_CameraPosition, job.m_CameraDirection, job.m_LodParameters);
                var isDirty = meshData.LodReferenceScale != transformData.scale;
                if (isDirty.x || isDirty.y || isDirty.z || meshData.MinLod <= 0)
                {
                    var maxDim = meshBounds * transformData.scale;
                    meshData.MinLod = RenderingUtils.CalculateLodLimit(math.csum(maxDim.max - maxDim.min) * 1f / 3f);
                    meshData.LodReferenceScale = transformData.scale;
                }
                minLod = meshData.MinLod;
                meshData.LastLod = RenderingUtils.CalculateLod(minDist * minDist, job.m_LodParameters);
                return meshData.LastLod;
            }

            private void DestroyRecursive(ref WERenderingJob job, Entity nextEntity, int unfilteredChunkIndex, Entity initialDelete = default)
            {
                if (nextEntity != initialDelete)
                {
                    if (initialDelete == default) initialDelete = nextEntity;
                    if (job.m_weTemplateForPrefabLookup.TryGetComponent(nextEntity, out var data))
                    {
                        DestroyRecursive(ref job, data.childEntity, unfilteredChunkIndex, initialDelete);
                    }
                    if (job.m_weTemplateUpdaterLookup.TryGetBuffer(nextEntity, out var updater))
                    {
                        for (int j = 0; j < updater.Length; j++)
                        {
                            DestroyRecursive(ref job, updater[j].childEntity, unfilteredChunkIndex, initialDelete);
                        }
                    }
                    if (job.m_weSubRefLookup.TryGetBuffer(nextEntity, out var subLayout))
                    {
                        for (int j = 0; j < subLayout.Length; j++)
                        {
                            DestroyRecursive(ref job, subLayout[j].m_weTextData, unfilteredChunkIndex, initialDelete);
                        }
                    }
                }
                job.m_CommandBuffer.AddComponent<Game.Common.Deleted>(unfilteredChunkIndex, nextEntity);
            }
        }
    }

    public struct WEDrawing : IComponentData, IEnableableComponent { }
}

/** Original method body for reference
```cs
protected override void OnUpdate(){
            bool loaded = this.GetLoaded();
            this.m_WriteDependencies.Complete();
            this.m_ReadDependencies.Complete();
            float3 @float = this.m_PrevCameraPosition;
            float3 float2 = this.m_PrevCameraDirection;
            float4 float3 = this.m_PrevLodParameters;
            LODParameters lodParameters;
            if (this.m_CameraUpdateSystem.TryGetLODParameters(out lodParameters))
            {
                @float = lodParameters.cameraPosition;
                IGameCameraController activeCameraController = this.m_CameraUpdateSystem.activeCameraController;
                float3 = RenderingUtils.CalculateLodParameters(this.m_BatchDataSystem.GetLevelOfDetail(this.m_RenderingSystem.frameLod, activeCameraController), lodParameters);
                float2 = this.m_CameraUpdateSystem.activeViewer.forward;
            }
            BoundsMask boundsMask = BoundsMask.NormalLayers;
            if (this.m_UndergroundViewSystem.pipelinesOn)
            {
                boundsMask |= BoundsMask.PipelineLayer;
            }
            if (this.m_UndergroundViewSystem.subPipelinesOn)
            {
                boundsMask |= BoundsMask.SubPipelineLayer;
            }
            if (this.m_UndergroundViewSystem.waterwaysOn)
            {
                boundsMask |= BoundsMask.WaterwayLayer;
            }
            if (this.m_RenderingSystem.markersVisible)
            {
                boundsMask |= BoundsMask.Debug;
            }
            if (this.m_ResetPrevious)
            {
                this.m_PrevCameraPosition = @float;
                this.m_PrevCameraDirection = float2;
                this.m_PrevLodParameters = float3;
                this.m_PrevVisibleMask = (BoundsMask)0;
                this.visibleMask = boundsMask;
                this.becameVisible = boundsMask;
                this.becameHidden = (BoundsMask)0;
            }
            else
            {
                this.visibleMask = boundsMask;
                this.becameVisible = (boundsMask & ~this.m_PrevVisibleMask);
                this.becameHidden = (this.m_PrevVisibleMask & ~boundsMask);
            }
            int length = this.m_CullingData.Length;
            NativeParallelQueue<PreCullingSystem.CullingAction> nativeParallelQueue = new NativeParallelQueue<PreCullingSystem.CullingAction>(Allocator.TempJob);
            NativeReference<int> cullingDataIndex = new NativeReference<int>(length, Allocator.TempJob);
            NativeQueue<PreCullingSystem.OverflowAction> overflowActions = new NativeQueue<PreCullingSystem.OverflowAction>(Allocator.TempJob);
            NativeArray<int> nodeBuffer = new NativeArray<int>(1536, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            NativeArray<int> subDataBuffer = new NativeArray<int>(1536, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            PreCullingSystem.TreeCullingJob1 treeCullingJob = default(PreCullingSystem.TreeCullingJob1);
            JobHandle job;
            treeCullingJob.m_ObjectSearchTree = this.m_ObjectSearchSystem.GetSearchTree(true, out job);
            JobHandle job2;
            treeCullingJob.m_NetSearchTree = this.m_NetSearchSystem.GetNetSearchTree(true, out job2);
            JobHandle job3;
            treeCullingJob.m_LaneSearchTree = this.m_NetSearchSystem.GetLaneSearchTree(true, out job3);
            treeCullingJob.m_LodParameters = float3;
            treeCullingJob.m_PrevLodParameters = this.m_PrevLodParameters;
            treeCullingJob.m_CameraPosition = @float;
            treeCullingJob.m_PrevCameraPosition = this.m_PrevCameraPosition;
            treeCullingJob.m_CameraDirection = float2;
            treeCullingJob.m_PrevCameraDirection = this.m_PrevCameraDirection;
            treeCullingJob.m_VisibleMask = boundsMask;
            treeCullingJob.m_PrevVisibleMask = this.m_PrevVisibleMask;
            treeCullingJob.m_NodeBuffer = nodeBuffer;
            treeCullingJob.m_SubDataBuffer = subDataBuffer;
            treeCullingJob.m_ActionQueue = nativeParallelQueue.AsWriter();
            PreCullingSystem.TreeCullingJob1 treeCullingJob2 = treeCullingJob;
            PreCullingSystem.TreeCullingJob2 treeCullingJob3 = default(PreCullingSystem.TreeCullingJob2);
            treeCullingJob3.m_ObjectSearchTree = treeCullingJob2.m_ObjectSearchTree;
            treeCullingJob3.m_NetSearchTree = treeCullingJob2.m_NetSearchTree;
            treeCullingJob3.m_LaneSearchTree = treeCullingJob2.m_LaneSearchTree;
            treeCullingJob3.m_LodParameters = float3;
            treeCullingJob3.m_PrevLodParameters = this.m_PrevLodParameters;
            treeCullingJob3.m_CameraPosition = @float;
            treeCullingJob3.m_PrevCameraPosition = this.m_PrevCameraPosition;
            treeCullingJob3.m_CameraDirection = float2;
            treeCullingJob3.m_PrevCameraDirection = this.m_PrevCameraDirection;
            treeCullingJob3.m_VisibleMask = boundsMask;
            treeCullingJob3.m_PrevVisibleMask = this.m_PrevVisibleMask;
            treeCullingJob3.m_NodeBuffer = nodeBuffer;
            treeCullingJob3.m_SubDataBuffer = subDataBuffer;
            treeCullingJob3.m_ActionQueue = nativeParallelQueue.AsWriter();
            PreCullingSystem.TreeCullingJob2 jobData = treeCullingJob3;
            JobHandle dependsOn = treeCullingJob2.Schedule(3, 1, JobHandle.CombineDependencies(job, job2, job3));
            JobHandle jobHandle = jobData.Schedule(nodeBuffer.Length, 1, dependsOn);
            JobHandle.ScheduleBatchedJobs();
            this.m_BatchMeshSystem.CompleteCaching();
            PreCullingSystem.QueryFlags queryFlags = this.GetQueryFlags();
            PreCullingSystem.InitializeCullingJob initializeCullingJob = default(PreCullingSystem.InitializeCullingJob);
            initializeCullingJob.m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle<UpdateFrame>(ref this.__TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, base.CheckedStateRef);
            initializeCullingJob.m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle<Owner>(ref this.__TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, base.CheckedStateRef);
            initializeCullingJob.m_UpdatedType = InternalCompilerInterface.GetComponentTypeHandle<Updated>(ref this.__TypeHandle.__Game_Common_Updated_RO_ComponentTypeHandle, base.CheckedStateRef);
            initializeCullingJob.m_BatchesUpdatedType = InternalCompilerInterface.GetComponentTypeHandle<BatchesUpdated>(ref this.__TypeHandle.__Game_Common_BatchesUpdated_RO_ComponentTypeHandle, base.CheckedStateRef);
            initializeCullingJob.m_OverriddenType = InternalCompilerInterface.GetComponentTypeHandle<Overridden>(ref this.__TypeHandle.__Game_Common_Overridden_RO_ComponentTypeHandle, base.CheckedStateRef);
            initializeCullingJob.m_TransformType = InternalCompilerInterface.GetComponentTypeHandle<Transform>(ref this.__TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, base.CheckedStateRef);
            initializeCullingJob.m_StackType = InternalCompilerInterface.GetComponentTypeHandle<Stack>(ref this.__TypeHandle.__Game_Objects_Stack_RO_ComponentTypeHandle, base.CheckedStateRef);
            initializeCullingJob.m_ObjectMarkerType = InternalCompilerInterface.GetComponentTypeHandle<Game.Objects.Marker>(ref this.__TypeHandle.__Game_Objects_Marker_RO_ComponentTypeHandle, base.CheckedStateRef);
            initializeCullingJob.m_OutsideConnectionType = InternalCompilerInterface.GetComponentTypeHandle<Game.Objects.OutsideConnection>(ref this.__TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentTypeHandle, base.CheckedStateRef);
            initializeCullingJob.m_UnspawnedType = InternalCompilerInterface.GetComponentTypeHandle<Unspawned>(ref this.__TypeHandle.__Game_Objects_Unspawned_RO_ComponentTypeHandle, base.CheckedStateRef);
            initializeCullingJob.m_NodeType = InternalCompilerInterface.GetComponentTypeHandle<Node>(ref this.__TypeHandle.__Game_Net_Node_RO_ComponentTypeHandle, base.CheckedStateRef);
            initializeCullingJob.m_EdgeType = InternalCompilerInterface.GetComponentTypeHandle<Edge>(ref this.__TypeHandle.__Game_Net_Edge_RO_ComponentTypeHandle, base.CheckedStateRef);
            initializeCullingJob.m_NodeGeometryType = InternalCompilerInterface.GetComponentTypeHandle<NodeGeometry>(ref this.__TypeHandle.__Game_Net_NodeGeometry_RO_ComponentTypeHandle, base.CheckedStateRef);
            initializeCullingJob.m_EdgeGeometryType = InternalCompilerInterface.GetComponentTypeHandle<EdgeGeometry>(ref this.__TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentTypeHandle, base.CheckedStateRef);
            initializeCullingJob.m_StartNodeGeometryType = InternalCompilerInterface.GetComponentTypeHandle<StartNodeGeometry>(ref this.__TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentTypeHandle, base.CheckedStateRef);
            initializeCullingJob.m_EndNodeGeometryType = InternalCompilerInterface.GetComponentTypeHandle<EndNodeGeometry>(ref this.__TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentTypeHandle, base.CheckedStateRef);
            initializeCullingJob.m_CompositionType = InternalCompilerInterface.GetComponentTypeHandle<Composition>(ref this.__TypeHandle.__Game_Net_Composition_RO_ComponentTypeHandle, base.CheckedStateRef);
            initializeCullingJob.m_OrphanType = InternalCompilerInterface.GetComponentTypeHandle<Orphan>(ref this.__TypeHandle.__Game_Net_Orphan_RO_ComponentTypeHandle, base.CheckedStateRef);
            initializeCullingJob.m_CurveType = InternalCompilerInterface.GetComponentTypeHandle<Curve>(ref this.__TypeHandle.__Game_Net_Curve_RO_ComponentTypeHandle, base.CheckedStateRef);
            initializeCullingJob.m_UtilityLaneType = InternalCompilerInterface.GetComponentTypeHandle<Game.Net.UtilityLane>(ref this.__TypeHandle.__Game_Net_UtilityLane_RO_ComponentTypeHandle, base.CheckedStateRef);
            initializeCullingJob.m_NetMarkerType = InternalCompilerInterface.GetComponentTypeHandle<Game.Net.Marker>(ref this.__TypeHandle.__Game_Net_Marker_RO_ComponentTypeHandle, base.CheckedStateRef);
            initializeCullingJob.m_ZoneBlockType = InternalCompilerInterface.GetComponentTypeHandle<Block>(ref this.__TypeHandle.__Game_Zones_Block_RO_ComponentTypeHandle, base.CheckedStateRef);
            initializeCullingJob.m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle<PrefabRef>(ref this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, base.CheckedStateRef);
            initializeCullingJob.m_TransformFrameType = InternalCompilerInterface.GetBufferTypeHandle<TransformFrame>(ref this.__TypeHandle.__Game_Objects_TransformFrame_RO_BufferTypeHandle, base.CheckedStateRef);
            initializeCullingJob.m_CullingInfoType = InternalCompilerInterface.GetComponentTypeHandle<CullingInfo>(ref this.__TypeHandle.__Game_Rendering_CullingInfo_RW_ComponentTypeHandle, base.CheckedStateRef);
            initializeCullingJob.m_PrefabRefData = InternalCompilerInterface.GetComponentLookup<PrefabRef>(ref this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, base.CheckedStateRef);
            initializeCullingJob.m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup<ObjectGeometryData>(ref this.__TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, base.CheckedStateRef);
            initializeCullingJob.m_PrefabStackData = InternalCompilerInterface.GetComponentLookup<StackData>(ref this.__TypeHandle.__Game_Prefabs_StackData_RO_ComponentLookup, base.CheckedStateRef);
            initializeCullingJob.m_PrefabLaneGeometryData = InternalCompilerInterface.GetComponentLookup<NetLaneGeometryData>(ref this.__TypeHandle.__Game_Prefabs_NetLaneGeometryData_RO_ComponentLookup, base.CheckedStateRef);
            initializeCullingJob.m_PrefabUtilityLaneData = InternalCompilerInterface.GetComponentLookup<UtilityLaneData>(ref this.__TypeHandle.__Game_Prefabs_UtilityLaneData_RO_ComponentLookup, base.CheckedStateRef);
            initializeCullingJob.m_PrefabCompositionData = InternalCompilerInterface.GetComponentLookup<NetCompositionData>(ref this.__TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup, base.CheckedStateRef);
            initializeCullingJob.m_PrefabCompositionMeshRef = InternalCompilerInterface.GetComponentLookup<NetCompositionMeshRef>(ref this.__TypeHandle.__Game_Prefabs_NetCompositionMeshRef_RO_ComponentLookup, base.CheckedStateRef);
            initializeCullingJob.m_PrefabCompositionMeshData = InternalCompilerInterface.GetComponentLookup<NetCompositionMeshData>(ref this.__TypeHandle.__Game_Prefabs_NetCompositionMeshData_RO_ComponentLookup, base.CheckedStateRef);
            initializeCullingJob.m_PrefabNetData = InternalCompilerInterface.GetComponentLookup<NetData>(ref this.__TypeHandle.__Game_Prefabs_NetData_RO_ComponentLookup, base.CheckedStateRef);
            initializeCullingJob.m_PrefabNetGeometryData = InternalCompilerInterface.GetComponentLookup<NetGeometryData>(ref this.__TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, base.CheckedStateRef);
            initializeCullingJob.m_EditorMode = this.m_ToolSystem.actionMode.IsEditor();
            initializeCullingJob.m_UpdateAll = loaded;
            initializeCullingJob.m_UnspawnedVisible = this.m_RenderingSystem.unspawnedVisible;
            initializeCullingJob.m_DilatedUtilityTypes = this.m_UndergroundViewSystem.utilityTypes;
            initializeCullingJob.m_TerrainHeightData = this.m_TerrainSystem.GetHeightData(false);
            initializeCullingJob.m_CullingData = this.m_CullingData;
            PreCullingSystem.InitializeCullingJob initializeCullingJob2 = initializeCullingJob;
            PreCullingSystem.EventCullingJob eventCullingJob = default(PreCullingSystem.EventCullingJob);
            eventCullingJob.m_RentersUpdatedType = InternalCompilerInterface.GetComponentTypeHandle<RentersUpdated>(ref this.__TypeHandle.__Game_Buildings_RentersUpdated_RO_ComponentTypeHandle, base.CheckedStateRef);
            eventCullingJob.m_ColorUpdatedType = InternalCompilerInterface.GetComponentTypeHandle<ColorUpdated>(ref this.__TypeHandle.__Game_Routes_ColorUpdated_RO_ComponentTypeHandle, base.CheckedStateRef);
            eventCullingJob.m_CullingInfoData = InternalCompilerInterface.GetComponentLookup<CullingInfo>(ref this.__TypeHandle.__Game_Rendering_CullingInfo_RO_ComponentLookup, base.CheckedStateRef);
            eventCullingJob.m_SubObjects = InternalCompilerInterface.GetBufferLookup<Game.Objects.SubObject>(ref this.__TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, base.CheckedStateRef);
            eventCullingJob.m_SubLanes = InternalCompilerInterface.GetBufferLookup<Game.Net.SubLane>(ref this.__TypeHandle.__Game_Net_SubLane_RO_BufferLookup, base.CheckedStateRef);
            eventCullingJob.m_RouteVehicles = InternalCompilerInterface.GetBufferLookup<RouteVehicle>(ref this.__TypeHandle.__Game_Routes_RouteVehicle_RO_BufferLookup, base.CheckedStateRef);
            eventCullingJob.m_LayoutElements = InternalCompilerInterface.GetBufferLookup<LayoutElement>(ref this.__TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, base.CheckedStateRef);
            eventCullingJob.m_CullingData = this.m_CullingData;
            PreCullingSystem.EventCullingJob jobData2 = eventCullingJob;
            PreCullingSystem.QueryCullingJob queryCullingJob = default(PreCullingSystem.QueryCullingJob);
            queryCullingJob.m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref this.__TypeHandle.__Unity_Entities_Entity_TypeHandle, base.CheckedStateRef);
            queryCullingJob.m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle<UpdateFrame>(ref this.__TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, base.CheckedStateRef);
            queryCullingJob.m_TransformType = InternalCompilerInterface.GetComponentTypeHandle<Transform>(ref this.__TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, base.CheckedStateRef);
            queryCullingJob.m_TransformFrameType = InternalCompilerInterface.GetBufferTypeHandle<TransformFrame>(ref this.__TypeHandle.__Game_Objects_TransformFrame_RO_BufferTypeHandle, base.CheckedStateRef);
            queryCullingJob.m_CullingInfoType = InternalCompilerInterface.GetComponentTypeHandle<CullingInfo>(ref this.__TypeHandle.__Game_Rendering_CullingInfo_RW_ComponentTypeHandle, base.CheckedStateRef);
            queryCullingJob.m_LodParameters = float3;
            queryCullingJob.m_CameraPosition = @float;
            queryCullingJob.m_CameraDirection = float2;
            queryCullingJob.m_FrameIndex = this.m_RenderingSystem.frameIndex;
            queryCullingJob.m_FrameTime = this.m_RenderingSystem.frameTime;
            queryCullingJob.m_VisibleMask = boundsMask;
            queryCullingJob.m_ActionQueue = nativeParallelQueue.AsWriter();
            PreCullingSystem.QueryCullingJob jobData3 = queryCullingJob;
            PreCullingSystem.QueryRemoveJob queryRemoveJob = default(PreCullingSystem.QueryRemoveJob);
            queryRemoveJob.m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref this.__TypeHandle.__Unity_Entities_Entity_TypeHandle, base.CheckedStateRef);
            queryRemoveJob.m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle<Deleted>(ref this.__TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, base.CheckedStateRef);
            queryRemoveJob.m_AppliedType = InternalCompilerInterface.GetComponentTypeHandle<Applied>(ref this.__TypeHandle.__Game_Common_Applied_RO_ComponentTypeHandle, base.CheckedStateRef);
            queryRemoveJob.m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle<UpdateFrame>(ref this.__TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, base.CheckedStateRef);
            queryRemoveJob.m_TransformFrameType = InternalCompilerInterface.GetBufferTypeHandle<TransformFrame>(ref this.__TypeHandle.__Game_Objects_TransformFrame_RO_BufferTypeHandle, base.CheckedStateRef);
            queryRemoveJob.m_CullingInfoType = InternalCompilerInterface.GetComponentTypeHandle<CullingInfo>(ref this.__TypeHandle.__Game_Rendering_CullingInfo_RW_ComponentTypeHandle, base.CheckedStateRef);
            queryRemoveJob.m_ActionQueue = nativeParallelQueue.AsWriter();
            PreCullingSystem.QueryRemoveJob jobData4 = queryRemoveJob;
            PreCullingSystem.RelativeCullingJob relativeCullingJob = default(PreCullingSystem.RelativeCullingJob);
            relativeCullingJob.m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref this.__TypeHandle.__Unity_Entities_Entity_TypeHandle, base.CheckedStateRef);
            relativeCullingJob.m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle<Owner>(ref this.__TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, base.CheckedStateRef);
            relativeCullingJob.m_CurrentVehicleType = InternalCompilerInterface.GetComponentTypeHandle<CurrentVehicle>(ref this.__TypeHandle.__Game_Creatures_CurrentVehicle_RO_ComponentTypeHandle, base.CheckedStateRef);
            relativeCullingJob.m_CullingInfoData = InternalCompilerInterface.GetComponentLookup<CullingInfo>(ref this.__TypeHandle.__Game_Rendering_CullingInfo_RW_ComponentLookup, base.CheckedStateRef);
            relativeCullingJob.m_LodParameters = float3;
            relativeCullingJob.m_CameraPosition = @float;
            relativeCullingJob.m_CameraDirection = float2;
            relativeCullingJob.m_VisibleMask = boundsMask;
            relativeCullingJob.m_ActionQueue = nativeParallelQueue.AsWriter();
            PreCullingSystem.RelativeCullingJob jobData5 = relativeCullingJob;
            PreCullingSystem.TempCullingJob tempCullingJob = default(PreCullingSystem.TempCullingJob);
            tempCullingJob.m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref this.__TypeHandle.__Unity_Entities_Entity_TypeHandle, base.CheckedStateRef);
            tempCullingJob.m_InterpolatedTransformType = InternalCompilerInterface.GetComponentTypeHandle<InterpolatedTransform>(ref this.__TypeHandle.__Game_Rendering_InterpolatedTransform_RO_ComponentTypeHandle, base.CheckedStateRef);
            tempCullingJob.m_TransformType = InternalCompilerInterface.GetComponentTypeHandle<Transform>(ref this.__TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, base.CheckedStateRef);
            tempCullingJob.m_StackType = InternalCompilerInterface.GetComponentTypeHandle<Stack>(ref this.__TypeHandle.__Game_Objects_Stack_RO_ComponentTypeHandle, base.CheckedStateRef);
            tempCullingJob.m_Type = InternalCompilerInterface.GetComponentTypeHandle<>(ref this.__TypeHandle.__Game_Objects__RO_ComponentTypeHandle, base.CheckedStateRef);
            tempCullingJob.m_StoppedType = InternalCompilerInterface.GetComponentTypeHandle<Stopped>(ref this.__TypeHandle.__Game_Objects_Stopped_RO_ComponentTypeHandle, base.CheckedStateRef);
            tempCullingJob.m_TempType = InternalCompilerInterface.GetComponentTypeHandle<Temp>(ref this.__TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, base.CheckedStateRef);
            tempCullingJob.m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle<PrefabRef>(ref this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, base.CheckedStateRef);
            tempCullingJob.m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup<ObjectGeometryData>(ref this.__TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, base.CheckedStateRef);
            tempCullingJob.m_PrefabStackData = InternalCompilerInterface.GetComponentLookup<StackData>(ref this.__TypeHandle.__Game_Prefabs_StackData_RO_ComponentLookup, base.CheckedStateRef);
            tempCullingJob.m_CullingInfoData = InternalCompilerInterface.GetComponentLookup<CullingInfo>(ref this.__TypeHandle.__Game_Rendering_CullingInfo_RW_ComponentLookup, base.CheckedStateRef);
            tempCullingJob.m_LodParameters = float3;
            tempCullingJob.m_CameraPosition = @float;
            tempCullingJob.m_CameraDirection = float2;
            tempCullingJob.m_VisibleMask = boundsMask;
            tempCullingJob.m_TerrainHeightData = initializeCullingJob2.m_TerrainHeightData;
            tempCullingJob.m_ActionQueue = nativeParallelQueue.AsWriter();
            PreCullingSystem.TempCullingJob jobData6 = tempCullingJob;
            EntityQuery query = loaded ? this.m_CullingInfoQuery : this.m_InitializeQuery;
            EntityQuery cullingQuery = this.GetCullingQuery(queryFlags);
            EntityQuery relativeQuery = this.GetRelativeQuery(queryFlags);
            EntityQuery removeQuery = this.GetRemoveQuery(this.m_PrevQueryFlags & ~queryFlags);
            JobHandle dependsOn2 = initializeCullingJob2.ScheduleParallel(query, base.Dependency);
            JobHandle dependsOn3 = jobData2.Schedule(this.m_EventQuery, dependsOn2);
            JobHandle dependsOn4 = jobData3.ScheduleParallel(cullingQuery, dependsOn3);
            JobHandle dependsOn5 = jobData4.ScheduleParallel(removeQuery, dependsOn4);
            JobHandle dependsOn6 = jobData5.ScheduleParallel(relativeQuery, dependsOn5);
            JobHandle jobHandle2 = jobData6.ScheduleParallel(this.m_TempQuery, dependsOn6);
            if (this.m_ResetPrevious || this.becameHidden != (BoundsMask)0)
            {
                PreCullingSystem.VerifyVisibleJob jobData7 = default(PreCullingSystem.VerifyVisibleJob);
                jobData7.m_CullingInfoData = InternalCompilerInterface.GetComponentLookup<CullingInfo>(ref this.__TypeHandle.__Game_Rendering_CullingInfo_RO_ComponentLookup, base.CheckedStateRef);
                jobData7.m_LodParameters = float3;
                jobData7.m_CameraPosition = @float;
                jobData7.m_CameraDirection = float2;
                jobData7.m_VisibleMask = boundsMask;
                jobData7.m_CullingData = this.m_CullingData;
                jobData7.m_ActionQueue = nativeParallelQueue.AsWriter();
                JobHandle job4 = jobData7.Schedule(length, 16, jobHandle2);
                this.m_WriteDependencies = JobHandle.CombineDependencies(jobHandle, job4);
            }
            else
            {
                this.m_WriteDependencies = JobHandle.CombineDependencies(jobHandle, jobHandle2);
            }
            PreCullingSystem.CullingActionJob cullingActionJob = default(PreCullingSystem.CullingActionJob);
            cullingActionJob.m_CullingActions = nativeParallelQueue.AsReader();
            cullingActionJob.m_OverflowActions = overflowActions.AsParallelWriter();
            cullingActionJob.m_CullingInfo = InternalCompilerInterface.GetComponentLookup<CullingInfo>(ref this.__TypeHandle.__Game_Rendering_CullingInfo_RW_ComponentLookup, base.CheckedStateRef);
            cullingActionJob.m_CullingData = this.m_CullingData;
            cullingActionJob.m_CullingDataIndex = cullingDataIndex;
            PreCullingSystem.CullingActionJob jobData8 = cullingActionJob;
            PreCullingSystem.ResizeCullingDataJob resizeCullingDataJob = default(PreCullingSystem.ResizeCullingDataJob);
            resizeCullingDataJob.m_CullingDataIndex = cullingDataIndex;
            resizeCullingDataJob.m_CullingData = this.m_CullingData;
            resizeCullingDataJob.m_UpdatedData = this.m_UpdatedData;
            resizeCullingDataJob.m_OverflowActions = overflowActions;
            PreCullingSystem.ResizeCullingDataJob jobData9 = resizeCullingDataJob;
            PreCullingSystem.FilterUpdatesJob filterUpdatesJob = default(PreCullingSystem.FilterUpdatesJob);
            filterUpdatesJob.m_CreatedData = InternalCompilerInterface.GetComponentLookup<Created>(ref this.__TypeHandle.__Game_Common_Created_RO_ComponentLookup, base.CheckedStateRef);
            filterUpdatesJob.m_UpdatedData = InternalCompilerInterface.GetComponentLookup<Updated>(ref this.__TypeHandle.__Game_Common_Updated_RO_ComponentLookup, base.CheckedStateRef);
            filterUpdatesJob.m_AppliedData = InternalCompilerInterface.GetComponentLookup<Applied>(ref this.__TypeHandle.__Game_Common_Applied_RO_ComponentLookup, base.CheckedStateRef);
            filterUpdatesJob.m_BatchesUpdatedData = InternalCompilerInterface.GetComponentLookup<BatchesUpdated>(ref this.__TypeHandle.__Game_Common_BatchesUpdated_RO_ComponentLookup, base.CheckedStateRef);
            filterUpdatesJob.m_InterpolatedTransformData = InternalCompilerInterface.GetComponentLookup<InterpolatedTransform>(ref this.__TypeHandle.__Game_Rendering_InterpolatedTransform_RO_ComponentLookup, base.CheckedStateRef);
            filterUpdatesJob.m_OwnerData = InternalCompilerInterface.GetComponentLookup<Owner>(ref this.__TypeHandle.__Game_Common_Owner_RO_ComponentLookup, base.CheckedStateRef);
            filterUpdatesJob.m_TempData = InternalCompilerInterface.GetComponentLookup<Temp>(ref this.__TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, base.CheckedStateRef);
            filterUpdatesJob.m_ObjectData = InternalCompilerInterface.GetComponentLookup<Game.Objects.Object>(ref this.__TypeHandle.__Game_Objects_Object_RO_ComponentLookup, base.CheckedStateRef);
            filterUpdatesJob.m_ObjectGeometryData = InternalCompilerInterface.GetComponentLookup<ObjectGeometry>(ref this.__TypeHandle.__Game_Objects_ObjectGeometry_RO_ComponentLookup, base.CheckedStateRef);
            filterUpdatesJob.m_ObjectColorData = InternalCompilerInterface.GetComponentLookup<Game.Objects.Color>(ref this.__TypeHandle.__Game_Objects_Color_RO_ComponentLookup, base.CheckedStateRef);
            filterUpdatesJob.m_PlantData = InternalCompilerInterface.GetComponentLookup<Plant>(ref this.__TypeHandle.__Game_Objects_Plant_RO_ComponentLookup, base.CheckedStateRef);
            filterUpdatesJob.m_TreeData = InternalCompilerInterface.GetComponentLookup<Tree>(ref this.__TypeHandle.__Game_Objects_Tree_RO_ComponentLookup, base.CheckedStateRef);
            filterUpdatesJob.m_RelativeData = InternalCompilerInterface.GetComponentLookup<Relative>(ref this.__TypeHandle.__Game_Objects_Relative_RO_ComponentLookup, base.CheckedStateRef);
            filterUpdatesJob.m_DamagedData = InternalCompilerInterface.GetComponentLookup<Damaged>(ref this.__TypeHandle.__Game_Objects_Damaged_RO_ComponentLookup, base.CheckedStateRef);
            filterUpdatesJob.m_BuildingData = InternalCompilerInterface.GetComponentLookup<Building>(ref this.__TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, base.CheckedStateRef);
            filterUpdatesJob.m_ExtensionData = InternalCompilerInterface.GetComponentLookup<Extension>(ref this.__TypeHandle.__Game_Buildings_Extension_RO_ComponentLookup, base.CheckedStateRef);
            filterUpdatesJob.m_EdgeData = InternalCompilerInterface.GetComponentLookup<Edge>(ref this.__TypeHandle.__Game_Net_Edge_RO_ComponentLookup, base.CheckedStateRef);
            filterUpdatesJob.m_NodeData = InternalCompilerInterface.GetComponentLookup<Node>(ref this.__TypeHandle.__Game_Net_Node_RO_ComponentLookup, base.CheckedStateRef);
            filterUpdatesJob.m_LaneData = InternalCompilerInterface.GetComponentLookup<Lane>(ref this.__TypeHandle.__Game_Net_Lane_RO_ComponentLookup, base.CheckedStateRef);
            filterUpdatesJob.m_NodeColorData = InternalCompilerInterface.GetComponentLookup<NodeColor>(ref this.__TypeHandle.__Game_Net_NodeColor_RO_ComponentLookup, base.CheckedStateRef);
            filterUpdatesJob.m_EdgeColorData = InternalCompilerInterface.GetComponentLookup<EdgeColor>(ref this.__TypeHandle.__Game_Net_EdgeColor_RO_ComponentLookup, base.CheckedStateRef);
            filterUpdatesJob.m_LaneColorData = InternalCompilerInterface.GetComponentLookup<LaneColor>(ref this.__TypeHandle.__Game_Net_LaneColor_RO_ComponentLookup, base.CheckedStateRef);
            filterUpdatesJob.m_LaneConditionData = InternalCompilerInterface.GetComponentLookup<LaneCondition>(ref this.__TypeHandle.__Game_Net_LaneCondition_RO_ComponentLookup, base.CheckedStateRef);
            filterUpdatesJob.m_ZoneData = InternalCompilerInterface.GetComponentLookup<Block>(ref this.__TypeHandle.__Game_Zones_Block_RO_ComponentLookup, base.CheckedStateRef);
            filterUpdatesJob.m_OnFireData = InternalCompilerInterface.GetComponentLookup<OnFire>(ref this.__TypeHandle.__Game_Events_OnFire_RO_ComponentLookup, base.CheckedStateRef);
            filterUpdatesJob.m_AnimatedData = InternalCompilerInterface.GetBufferLookup<Animated>(ref this.__TypeHandle.__Game_Rendering_Animated_RO_BufferLookup, base.CheckedStateRef);
            filterUpdatesJob.m_SkeletonData = InternalCompilerInterface.GetBufferLookup<Skeleton>(ref this.__TypeHandle.__Game_Rendering_Skeleton_RO_BufferLookup, base.CheckedStateRef);
            filterUpdatesJob.m_EmissiveData = InternalCompilerInterface.GetBufferLookup<Emissive>(ref this.__TypeHandle.__Game_Rendering_Emissive_RO_BufferLookup, base.CheckedStateRef);
            filterUpdatesJob.m_MeshColorData = InternalCompilerInterface.GetBufferLookup<MeshColor>(ref this.__TypeHandle.__Game_Rendering_MeshColor_RO_BufferLookup, base.CheckedStateRef);
            filterUpdatesJob.m_LayoutElements = InternalCompilerInterface.GetBufferLookup<LayoutElement>(ref this.__TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, base.CheckedStateRef);
            filterUpdatesJob.m_EffectInstances = InternalCompilerInterface.GetBufferLookup<EnabledEffect>(ref this.__TypeHandle.__Game_Effects_EnabledEffect_RO_BufferLookup, base.CheckedStateRef);
            filterUpdatesJob.m_TimerDelta = this.m_RenderingSystem.lodTimerDelta;
            filterUpdatesJob.m_CullingData = this.m_CullingData;
            filterUpdatesJob.m_UpdatedCullingData = this.m_UpdatedData.AsParallelWriter();
            PreCullingSystem.FilterUpdatesJob jobData10 = filterUpdatesJob;
            JobHandle jobHandle3 = jobData8.Schedule(nativeParallelQueue.HashRange, 1, this.m_WriteDependencies);
            JobHandle jobHandle4 = jobData9.Schedule(jobHandle3);
            JobHandle jobHandle5 = jobData10.Schedule(this.m_CullingData, 16, jobHandle4);
            this.m_ObjectSearchSystem.AddSearchTreeReader(jobHandle);
            this.m_NetSearchSystem.AddNetSearchTreeReader(jobHandle);
            this.m_NetSearchSystem.AddLaneSearchTreeReader(jobHandle);
            this.m_TerrainSystem.AddCPUHeightReader(jobHandle2);
            nativeParallelQueue.Dispose(jobHandle3);
            cullingDataIndex.Dispose(jobHandle4);
            overflowActions.Dispose(jobHandle4);
            nodeBuffer.Dispose(jobHandle);
            subDataBuffer.Dispose(jobHandle);
            this.m_PrevCameraPosition = @float;
            this.m_PrevCameraDirection = float2;
            this.m_PrevLodParameters = float3;
            this.m_PrevVisibleMask = boundsMask;
            this.m_PrevQueryFlags = queryFlags;
            this.m_ResetPrevious = false;
            this.m_WriteDependencies = jobHandle5;
            this.m_ReadDependencies = default(JobHandle);
            base.Dependency = jobHandle5;
}```
*/