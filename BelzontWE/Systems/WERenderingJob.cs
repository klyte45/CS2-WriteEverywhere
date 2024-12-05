using Game.Rendering;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Burst.Intrinsics;
using Colossal.Mathematics;


#if BURST
using Unity.Burst;
#else
using Belzont.Interfaces;
using Belzont.Utils;
#endif

namespace BelzontWE
{
    public partial class WERendererSystem
    {
#if BURST
        [BurstCompile]
#endif
        private struct WERenderingJob : IJobChunk
        {
            public ComponentTypeHandle<CullingInfo> m_cullingInfo;
            public ComponentLookup<WETextDataMain> m_weMainLookup;
            public BufferLookup<WESubTextRef> m_weSubRefLookup;
            public ComponentLookup<WETemplateUpdater> m_weTemplateUpdaterLookup;
            public ComponentLookup<WETemplateForPrefab> m_weTemplateForPrefabLookup;
            public ComponentLookup<InterpolatedTransform> m_iTransform;
            public ComponentLookup<Game.Objects.Transform> m_transform;
            public float4 m_LodParameters;
            public float3 m_CameraPosition;
            public float3 m_CameraDirection;
            public EntityTypeHandle m_EntityType;
            public NativeQueue<WERenderData>.ParallelWriter availToDraw;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
            public Entity m_selectedSubEntity;
            public Entity m_selectedEntity;
            public bool isAtWeEditor;
            public bool doLog;
            public ComponentLookup<WETextDataMaterial> m_weMaterialLookup;
            public ComponentLookup<WETextDataMesh> m_weMeshLookup;
            public ComponentLookup<WETextDataTransform> m_weTransformLookup;
            public ComponentLookup<WETextDataSourceMod> m_weSourceModLookup;

            private static readonly Bounds3 whiteTextureBounds = new(new(-.5f, -.5f, 0), new(.5f, .5f, 0));

            public unsafe void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(m_EntityType);
                var cullInfos = chunk.GetNativeArray(ref m_cullingInfo);

                for (int i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    var cullInfo = cullInfos[i];
                    if (cullInfo.m_PassedCulling != 1 && (!isAtWeEditor || m_selectedEntity != entity))
                    {

                        continue;
                    }
                    float3 positionRef;
                    quaternion rotationRef;
                    if (m_iTransform.TryGetComponent(entity, out var transform))
                    {
                        positionRef = transform.m_Position;
                        rotationRef = transform.m_Rotation;
                    }
                    else if (m_transform.TryGetComponent(entity, out var transform2))
                    {
                        positionRef = transform2.m_Position;
                        rotationRef = transform2.m_Rotation;
                    }
                    else
                    {
                        continue;
                    }
                    var baseMatrix = Matrix4x4.TRS(positionRef, rotationRef, Vector3.one);
                    if (m_weTemplateForPrefabLookup.TryGetComponent(entity, out var prefabWeLayout))
                    {
                        DrawTree(entity, prefabWeLayout.childEntity, baseMatrix, unfilteredChunkIndex);
                    }
                    if (m_weSubRefLookup.TryGetBuffer(entity, out var subLayout))
                    {
                        for (int j = 0; j < subLayout.Length; j++)
                        {
                            DrawTree(entity, subLayout[j].m_weTextData, baseMatrix, unfilteredChunkIndex);
                        }

                    }
                }
            }
            private void DrawTree(Entity geometryEntity, Entity nextEntity, Matrix4x4 prevMatrix, int unfilteredChunkIndex, bool parentIsPlaceholder = false)
            {
                if (!m_weMeshLookup.TryGetComponent(nextEntity, out var mesh))
                {
                    DestroyRecursive(nextEntity, unfilteredChunkIndex);
                    return;
                }
                var transform = m_weTransformLookup[nextEntity];
                var sourceMod = m_weSourceModLookup.TryGetComponent(nextEntity, out var src) ? src.modName : default;

                switch (mesh.TextType)
                {
                    case WESimulationTextType.Archetype:
                        if (m_weSubRefLookup.TryGetBuffer(nextEntity, out var subLayout2))
                        {
                            prevMatrix *= Matrix4x4.TRS(new Vector3(0, 0, .0008f), default, Vector3.one);
                            for (int j = 0; j < subLayout2.Length; j++)
                            {
                                DrawTree(geometryEntity, subLayout2[j].m_weTextData, prevMatrix, unfilteredChunkIndex);
                            }
                        }
                        return;
                    case WESimulationTextType.Placeholder:
                        {
                            if (!m_weTemplateUpdaterLookup.TryGetComponent(nextEntity, out var updater))
                            {
                                m_CommandBuffer.AddComponent<WEWaitingRendering>(unfilteredChunkIndex, nextEntity);
                                return;
                            }

                            int lod = CalculateLod(whiteTextureBounds, ref mesh, ref transform, ref prevMatrix, out _, out _, out _, out int minLod);
                            if (lod >= minLod || (isAtWeEditor && geometryEntity == m_selectedEntity))
                            {
                                var scale2 = transform.scale;
                                availToDraw.Enqueue(new WERenderData
                                {
                                    textDataEntity = nextEntity,
                                    geometryEntity = geometryEntity,
                                    main = m_weMainLookup[nextEntity],
                                    material = m_weMaterialLookup[nextEntity],
                                    modSource = sourceMod,
                                    mesh = m_weMeshLookup[nextEntity],
                                    transformMatrix = prevMatrix * Matrix4x4.TRS(transform.offsetPosition + (float3)Matrix4x4.Rotate(transform.offsetRotation).MultiplyPoint(new float3(0, 0, -.001f)), transform.offsetRotation, scale2)
                                });
                            }

                            DrawTree(geometryEntity, updater.childEntity, prevMatrix * Matrix4x4.TRS(transform.offsetPosition, transform.offsetRotation, Vector3.one), unfilteredChunkIndex, true);
                        }
                        break;
                    case WESimulationTextType.WhiteTexture:
                        {
                            var WTmatrix = prevMatrix * Matrix4x4.TRS(transform.offsetPosition, transform.offsetRotation, transform.scale);
                            int lod = CalculateLod(whiteTextureBounds, ref mesh, ref transform, ref WTmatrix, out _, out _, out _, out int minLod);
                            if (lod >= minLod || (isAtWeEditor && geometryEntity == m_selectedEntity))
                            {
                                availToDraw.Enqueue(new WERenderData
                                {
                                    textDataEntity = nextEntity,
                                    geometryEntity = geometryEntity,
                                    main = m_weMainLookup[nextEntity],
                                    material = m_weMaterialLookup[nextEntity],
                                    mesh = m_weMeshLookup[nextEntity],
                                    modSource = sourceMod,
                                    transformMatrix = WTmatrix
                                });
                            }

                            if (m_weSubRefLookup.TryGetBuffer(nextEntity, out var subLayoutWt))
                            {
                                var itemMatrix = prevMatrix * Matrix4x4.TRS(transform.offsetPosition + (float3)Matrix4x4.Rotate(transform.offsetRotation).MultiplyPoint(new float3(0, 0, .001f)), transform.offsetRotation, Vector3.one);
                                for (int j = 0; j < subLayoutWt.Length; j++)
                                {
                                    DrawTree(geometryEntity, subLayoutWt[j].m_weTextData, itemMatrix, unfilteredChunkIndex);
                                }
                            }
                        }
                        return;
                    default:
                        if (m_weTemplateUpdaterLookup.HasComponent(nextEntity))
                        {
                            m_CommandBuffer.AddComponent<WEWaitingRendering>(unfilteredChunkIndex, nextEntity);
                            return;
                        }
                        var scale = transform.scale;
                        if (mesh.HasBRI)
                        {
                            if (mesh.TextType == WESimulationTextType.Image && transform.useAbsoluteSizeEditing)
                            {
                                scale.x /= mesh.BriWidthMetersUnscaled;
                            }
                            if (mesh.TextType == WESimulationTextType.Text && mesh.MaxWidthMeters > 0 && mesh.BriWidthMetersUnscaled * scale.x > mesh.MaxWidthMeters)
                            {
                                scale.x = mesh.MaxWidthMeters / mesh.BriWidthMetersUnscaled;
                            }
                        }
                        var refPos = parentIsPlaceholder ? default : transform.offsetPosition;
                        var refRot = parentIsPlaceholder ? default : transform.offsetRotation;
                        var matrix = prevMatrix * Matrix4x4.TRS(refPos, refRot, scale);
                        if (mesh.HasBRI)
                        {
                            if (!float.IsNaN(matrix.m00) && !float.IsInfinity(matrix.m00))
                            {
                                int lod = CalculateLod(mesh.Bounds, ref mesh, ref transform, ref matrix, out Bounds3 refBounds, out float minDist, out float3 boundsSize, out int minLod);
                                if (doLog) Debug.Log($"G {geometryEntity.Index} {geometryEntity.Version} | E {nextEntity.Index} {nextEntity.Version}: minDist = {minDist} - refBounds = {refBounds.min} {refBounds.max} ({boundsSize}) - lod = {lod} - minLod = {minLod} - m_LodParameters = {m_LodParameters}");
                                if (lod >= minLod || (isAtWeEditor && geometryEntity == m_selectedEntity))
                                {
                                    availToDraw.Enqueue(new WERenderData
                                    {
                                        textDataEntity = nextEntity,
                                        geometryEntity = geometryEntity,
                                        main = m_weMainLookup[nextEntity],
                                        material = m_weMaterialLookup[nextEntity],
                                        modSource = sourceMod,
                                        mesh = mesh,
                                        transformMatrix = matrix
                                    });
                                }
                            }
                        }
                        else
                        {
                            availToDraw.Enqueue(new WERenderData
                            {
                                textDataEntity = nextEntity,
                                geometryEntity = geometryEntity,
                                main = m_weMainLookup[nextEntity],
                                material = m_weMaterialLookup[nextEntity],
                                modSource = sourceMod,
                                mesh = mesh,
                                transformMatrix = matrix
                            });
                        }
                        if (m_weSubRefLookup.TryGetBuffer(nextEntity, out var subLayout))
                        {
                            var itemMatrix = prevMatrix * Matrix4x4.TRS(refPos + (float3)Matrix4x4.Rotate(refRot).MultiplyPoint(new float3(0, 0, .00075f)), refRot, Vector3.one);
                            for (int j = 0; j < subLayout.Length; j++)
                            {
                                DrawTree(geometryEntity, subLayout[j].m_weTextData, itemMatrix, unfilteredChunkIndex);
                            }
                        }
                        break;
                }
            }

            private readonly int CalculateLod(Bounds3 meshBounds, ref WETextDataMesh meshData, ref WETextDataTransform transformData, ref Matrix4x4 matrix, out Bounds3 refBounds, out float minDist, out float3 boundsSize, out int minLod)
            {
                refBounds = new Bounds3(matrix.MultiplyPoint(meshBounds.min), matrix.MultiplyPoint(meshBounds.max));
                minDist = RenderingUtils.CalculateMinDistance(refBounds, m_CameraPosition, m_CameraDirection, m_LodParameters);
                boundsSize = refBounds.max - refBounds.min;
                var isDirty = meshData.LodReferenceScale != transformData.scale;
                if (isDirty.x || isDirty.y || isDirty.z || meshData.MinLod <= 0)
                {
                    var maxDim = meshBounds * transformData.scale;
                    meshData.MinLod = RenderingUtils.CalculateLodLimit(math.csum(maxDim.max - maxDim.min) * 1f / 3f);
                    meshData.LodReferenceScale = transformData.scale;
                }
                minLod = meshData.MinLod;
                meshData.LastLod = RenderingUtils.CalculateLod(minDist * minDist, m_LodParameters);
                return meshData.LastLod;
            }

            private void DestroyRecursive(Entity nextEntity, int unfilteredChunkIndex, Entity initialDelete = default)
            {
                if (nextEntity != initialDelete)
                {
                    if (initialDelete == default) initialDelete = nextEntity;
                    if (m_weTemplateForPrefabLookup.TryGetComponent(nextEntity, out var data))
                    {
                        DestroyRecursive(data.childEntity, unfilteredChunkIndex, initialDelete);
                    }
                    if (m_weTemplateUpdaterLookup.TryGetComponent(nextEntity, out var updater))
                    {
                        DestroyRecursive(updater.childEntity, unfilteredChunkIndex, initialDelete);
                    }
                    if (m_weSubRefLookup.TryGetBuffer(nextEntity, out var subLayout))
                    {
                        for (int j = 0; j < subLayout.Length; j++)
                        {
                            DestroyRecursive(subLayout[j].m_weTextData, unfilteredChunkIndex, initialDelete);
                        }
                    }
                }
                m_CommandBuffer.AddComponent<Game.Common.Deleted>(unfilteredChunkIndex, nextEntity);
            }
        }
    }

}