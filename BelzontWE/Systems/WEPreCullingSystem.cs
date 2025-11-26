using Colossal.Entities;
using Colossal.Mathematics;
using Game.Rendering;
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
            public Matrix4x4 transformMatrix;
            public byte lastLod;
        }

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

        private readonly NativeQueue<WERenderData> m_newItemsRender = new(Allocator.Persistent);
        private readonly NativeParallelHashSet<Entity> m_unmodifiedEntities = new(1024, Allocator.Persistent);
        private readonly NativeParallelHashSet<Entity> m_geomEntitiesLastFrame = new(1024, Allocator.Persistent);

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
            m_newItemsRender.Clear();
            m_unmodifiedEntities.Clear();

            m_geomEntitiesLastFrame.Clear();
            for (var i = 0; i < m_availToDraw.Length; i++)
            {
                m_geomEntitiesLastFrame.Add(m_availToDraw[i].geometryEntity);
            }

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
                m_newItemsRender = m_newItemsRender.AsParallelWriter(),
                m_unmodifiedEntities = m_unmodifiedEntities.AsParallelWriter(),
                m_geomEntitiesLastFrame = m_geomEntitiesLastFrame.AsReadOnly(),
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
                minLodUpdateSetting = Mathf.CeilToInt(WriteEverywhereCS2Mod.WeData.RequiredLodForFormulaesUpdate),
                indexStartString = indexStartString,
            };

            cullingActionJob.Schedule(data.Length, 1, JobHandle.CombineDependencies(deps, Dependency)).Complete();
            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();

            // First job: Filter unmodified entities (only if there are items to filter)
            if (m_availToDraw.IsCreated && m_availToDraw.Length > 0)
            {
                var filterJob = new WERenderFilterUnmodifiedEntitiesJob
                {
                    m_availToDraw = m_availToDraw,
                    m_unmodifiedEntities = m_unmodifiedEntities.AsReadOnly(),
                    m_newItemsRender = m_newItemsRender.AsParallelWriter()
                };
                filterJob.Schedule(m_availToDraw.Length, 64).Complete();
            }

            if (m_availToDraw.IsCreated) m_availToDraw.Dispose();
            m_availToDraw = m_newItemsRender.ToArray(Allocator.Persistent);

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
        [Unity.Burst.BurstCompile]
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
            public NativeQueue<WERenderData>.ParallelWriter m_newItemsRender;
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
            internal NativeParallelHashSet<Entity>.ParallelWriter m_unmodifiedEntities;
            internal NativeParallelHashSet<Entity>.ReadOnly m_geomEntitiesLastFrame;

            private static readonly Bounds3 whiteTextureBounds = new(new(-.5f, -.5f, 0), new(.5f, .5f, 0));
            private static readonly Bounds3 whiteCubeBounds = new(new(-.5f, -.5f, -.5f), new(.5f, .5f, .5f));


            public void Execute(int index)
            {
                var cullingAction = m_CullingActions[index];
                if ((cullingAction.m_Flags & PreCullingFlags.PassedCulling) != 0)
                {
                    var entity = cullingAction.m_Entity;
                    if ((!m_weTemplateForPrefabLookup.TryGetComponent(entity, out var prefabWeLayout) || prefabWeLayout.childEntity == Entity.Null) & (!m_weSubRefLookup.TryGetBuffer(entity, out var subLayout) || subLayout.Length == 0)) return;

                    if (!m_WEDrawingLookup.HasEnabledComponent(entity))
                    {
                        if (m_WEDrawingLookup.HasComponent(cullingAction.m_Entity))
                        {
                            m_CommandBuffer.SetComponentEnabled<WEDrawing>(index, entity, true);
                        }
                        else
                        {
                            m_CommandBuffer.AddComponent<WEDrawing>(index, entity);
                        }
                    }

                    if (m_geomEntitiesLastFrame.Contains(entity) && (entity.Index & 0x1f) != (frameCount & 0x1f) && (!isAtWeEditor || entity != m_selectedEntity))
                    {
                        m_unmodifiedEntities.Add(entity);
                        return;
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
                    PopulateVars(entity, ref variables, out _);
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
                if (m_WEDrawingLookup.HasEnabledComponent(cullingAction.m_Entity))
                {
                    m_CommandBuffer.SetComponentEnabled<WEDrawing>(index, cullingAction.m_Entity, false);
                }
            }

            private unsafe void PopulateVars(Entity entity, ref FixedString512Bytes inheritableVars, out FixedString512Bytes localVars)
            {
                localVars = new FixedString512Bytes();
                localVars.Append(inheritableVars);
                if (m_weVariablesLookup.TryGetBuffer(entity, out var variableBuffer) && !variableBuffer.IsEmpty)
                {
                    for (int i = 0; i < variableBuffer.Length; i++)
                    {
                        if (variableBuffer[i].Key[0] != '!')
                        {
                            inheritableVars.Append(variableBuffer[i].Key);
                            inheritableVars.Append(VARIABLE_KV_SEPARATOR);
                            inheritableVars.Append(variableBuffer[i].Value);
                            inheritableVars.Append(VARIABLE_ITEM_SEPARATOR);
                        }
                        localVars.Append(variableBuffer[i].Key);
                        localVars.Append(VARIABLE_KV_SEPARATOR);
                        localVars.Append(variableBuffer[i].Value);
                        localVars.Append(VARIABLE_ITEM_SEPARATOR);
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
                    PopulateVars(nextEntity, ref mutableVars, out FixedString512Bytes lclVars);
                    m_CommandBuffer.AddComponent(unfilteredChunkIndex, nextEntity, new WETextDataDirtyFormulae
                    {
                        geometry = geometryEntity,
                        vars = lclVars,
                    });
                    return;
                }
                CheckMesh(ref mesh, nextEntity, unfilteredChunkIndex);

                if (transform.useFormulaeToCheckIfDraw && !transform.MustDraw)
                {
                    var mutableVars = variables;
                    PopulateVars(nextEntity, ref mutableVars, out var lclVars);
                    CheckForUpdates(geometryEntity, nextEntity, unfilteredChunkIndex, in lclVars, 2000);
                    return;
                }

                // Optimize: Make single copy for this level, reuse for all children
                var inheritableVars = variables;
                PopulateVars(nextEntity, ref inheritableVars, out var currentVars);
                switch (mesh.TextType)
                {
                    case WESimulationTextType.MatrixTransform:
                        if (m_weSubRefLookup.TryGetBuffer(nextEntity, out var subLayoutSc))
                        {
                            var matrix = prevMatrix * Matrix4x4.TRS(mesh.OffsetPositionFormulae.EffectiveValue, Quaternion.Euler(mesh.OffsetRotationFormulae.EffectiveValue), mesh.ScaleFormulae.EffectiveValue);
                            CheckForUpdates(geometryEntity, nextEntity, unfilteredChunkIndex, in currentVars, 2000);
                            for (int j = 0; j < subLayoutSc.Length; j++)
                            {
                                DrawTree(geometryEntity, subLayoutSc[j].m_weTextData, matrix, geomMatrix, unfilteredChunkIndex, in inheritableVars, nthCall + 1);
                            }
                        }
                        break;
                    case WESimulationTextType.Archetype:
                        if (m_weSubRefLookup.TryGetBuffer(nextEntity, out var subLayout2))
                        {
                            prevMatrix *= Matrix4x4.TRS(new Vector3(0, 0, .001f), default, Vector3.one);
                            for (int j = 0; j < subLayout2.Length; j++)
                            {
                                DrawTree(geometryEntity, subLayout2[j].m_weTextData, prevMatrix, geomMatrix, unfilteredChunkIndex, in inheritableVars, nthCall + 1);
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

                                m_newItemsRender.Enqueue(new WERenderData
                                {
                                    textDataEntity = nextEntity,
                                    geometryEntity = geometryEntity,
                                    transformMatrix = prevMatrix * Matrix4x4.TRS(effectiveOffsetPosition + (float3)Matrix4x4.Rotate(transform.offsetRotation).MultiplyPoint(new float3(0, 0, -.001f)), transform.offsetRotation, scale2),
                                    lastLod = (byte)lod
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
                                    var layoutVars = new FixedString512Bytes(inheritableVars);
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
                                m_newItemsRender.Enqueue(new WERenderData
                                {
                                    textDataEntity = nextEntity,
                                    geometryEntity = geometryEntity,
                                    transformMatrix = WTmatrix,
                                    lastLod = (byte)lod
                                });
                            }

                            if (m_weSubRefLookup.TryGetBuffer(nextEntity, out var subLayoutWt))
                            {
                                var itemMatrix = prevMatrix * Matrix4x4.TRS(effectiveOffsetPosition + (float3)Matrix4x4.Rotate(transform.offsetRotation).MultiplyPoint(new float3(0, 0, mesh.childrenRefersToFrontFace ? (transform.scale.z * .5f) + .001f : .001f)), transform.offsetRotation, Vector3.one);
                                for (int j = 0; j < subLayoutWt.Length; j++)
                                {
                                    DrawTree(geometryEntity, subLayoutWt[j].m_weTextData, itemMatrix, geomMatrix, unfilteredChunkIndex, in inheritableVars, nthCall + 1);
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
                                m_newItemsRender.Enqueue(new WERenderData
                                {
                                    textDataEntity = nextEntity,
                                    geometryEntity = geometryEntity,
                                    transformMatrix = WTmatrix,
                                    lastLod = (byte)lod
                                });
                            }

                            if (m_weSubRefLookup.TryGetBuffer(nextEntity, out var subLayoutWt))
                            {
                                // Optimize: Cache shader check result
                                var zOffset = textureMaterial.Shader == WEShader.Decal ? .002f : .001f;
                                var itemMatrix = prevMatrix * Matrix4x4.TRS(effectiveOffsetPosition + (float3)Matrix4x4.Rotate(transform.offsetRotation).MultiplyPoint(new float3(0, 0, zOffset)), transform.offsetRotation, Vector3.one);
                                for (int j = 0; j < subLayoutWt.Length; j++)
                                {
                                    DrawTree(geometryEntity, subLayoutWt[j].m_weTextData, itemMatrix, geomMatrix, unfilteredChunkIndex, in inheritableVars, nthCall + 1);
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
                                        m_newItemsRender.Enqueue(new WERenderData
                                        {
                                            textDataEntity = nextEntity,
                                            geometryEntity = geometryEntity,
                                            transformMatrix = matrix,
                                            lastLod = (byte)lod
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
                            }
                            if (m_weSubRefLookup.TryGetBuffer(nextEntity, out var subLayout))
                            {
                                // Optimize: Cache shader check
                                var zOffset = defaultMaterial.Shader == WEShader.Decal ? .002f : .001f;
                                var itemMatrix = prevMatrix * Matrix4x4.TRS(refPos + (float3)Matrix4x4.Rotate(refRot).MultiplyPoint(new float3(0, 0, zOffset)), refRot, Vector3.one);
                                for (int j = 0; j < subLayout.Length; j++)
                                {
                                    DrawTree(geometryEntity, subLayout[j].m_weTextData, itemMatrix, geomMatrix, unfilteredChunkIndex, in inheritableVars, nthCall + 1);
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
                if (m_weMainLookup[nextEntity].nextUpdateFrame < frameCount)
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

#if BURST
        [Unity.Burst.BurstCompile]
#endif
        private struct WERenderFilterUnmodifiedEntitiesJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<WERenderData> m_availToDraw;
            [ReadOnly] public NativeParallelHashSet<Entity>.ReadOnly m_unmodifiedEntities;
            public NativeQueue<WERenderData>.ParallelWriter m_newItemsRender;

            public void Execute(int index)
            {
                var item = m_availToDraw[index];
                if (m_unmodifiedEntities.Contains(item.geometryEntity))
                {
                    m_newItemsRender.Enqueue(item);
                }
            }
        }
    }

    public struct WEDrawing : IComponentData, IEnableableComponent { }
}