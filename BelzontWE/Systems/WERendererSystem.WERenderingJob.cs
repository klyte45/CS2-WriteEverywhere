﻿using Game.Rendering;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Burst.Intrinsics;
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
            public ComponentLookup<WETextData> m_weDataLookup;
            public BufferLookup<WESubTextRef> m_weSubRefLookup;
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
            public Entity m_selectedEntity;
            public bool isAtWeEditor;

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
                if (!m_weDataLookup.TryGetComponent(nextEntity, out var weCustomData))
                {
                    DestroyRecursive(nextEntity, unfilteredChunkIndex);
                    return;
                }

                switch (weCustomData.TextType)
                {
                    case WESimulationTextType.Archetype:
                        if (m_weSubRefLookup.TryGetBuffer(nextEntity, out var subLayout2))
                        {
                            for (int j = 0; j < subLayout2.Length; j++)
                            {
                                DrawTree(geometryEntity, subLayout2[j].m_weTextData, prevMatrix, unfilteredChunkIndex);
                            }
                        }
                        return;
                    case WESimulationTextType.Placeholder:

                        if (!m_weTemplateUpdaterLookup.TryGetComponent(nextEntity, out var updater))
                        {
                            m_CommandBuffer.AddComponent<WEWaitingRenderingPlaceholder>(unfilteredChunkIndex, nextEntity);
                            return;
                        }
                        if (isAtWeEditor)
                        {
                            var scale2 = weCustomData.scale * weCustomData.BriOffsetScaleX / weCustomData.BriPixelDensity;
                            availToDraw.Enqueue(new WERenderData
                            {
                                textDataEntity = nextEntity,
                                geometryEntity = geometryEntity,
                                weComponent = weCustomData,
                                transformMatrix = prevMatrix * Matrix4x4.TRS(weCustomData.offsetPosition, weCustomData.offsetRotation, scale2)
                            });
                        }

                        DrawTree(geometryEntity, updater.childEntity, prevMatrix * Matrix4x4.TRS(weCustomData.offsetPosition, weCustomData.offsetRotation, Vector3.one), unfilteredChunkIndex, true);
                        return;
                    default:
                        if (m_weTemplateUpdaterLookup.HasComponent(nextEntity))
                        {
                            m_CommandBuffer.AddComponent<WEWaitingRendering>(unfilteredChunkIndex, nextEntity);
                            return;
                        }
                        var scale = weCustomData.scale * weCustomData.BriOffsetScaleX / weCustomData.BriPixelDensity;
                        if (weCustomData.HasBRI && weCustomData.TextType == WESimulationTextType.Text && weCustomData.maxWidthMeters > 0 && weCustomData.BriWidthMetersUnscaled * scale.x > weCustomData.maxWidthMeters)
                        {
                            scale.x = weCustomData.maxWidthMeters / weCustomData.BriWidthMetersUnscaled;
                        }
                        var refPos = parentIsPlaceholder ? default : weCustomData.offsetPosition;
                        var refRot = parentIsPlaceholder ? default : weCustomData.offsetRotation;

                        if (weCustomData.HasBRI)
                        {
                            var matrix = prevMatrix * Matrix4x4.TRS(refPos, refRot, scale);
                            var refBounds = new Colossal.Mathematics.Bounds3(matrix.MultiplyPoint(weCustomData.Bounds.min), matrix.MultiplyPoint(weCustomData.Bounds.max));
                            float minDist = RenderingUtils.CalculateMinDistance(refBounds, m_CameraPosition, m_CameraDirection, m_LodParameters);

                            int lod = RenderingUtils.CalculateLod(minDist * minDist, m_LodParameters);
                            var minLod = RenderingUtils.CalculateLodLimit(RenderingUtils.GetRenderingSize((refBounds.max - refBounds.min) * 8));
                       //     if (doLog) Debug.Log($"G {geometryEntity.Index} {geometryEntity.Version} | E {nextEntity.Index} {nextEntity.Version}: minDist = {minDist} - refBounds = {refBounds.min} {refBounds.max} - lod = {lod} - minLod = {minLod} - m_LodParameters = {m_LodParameters}");
                            if (lod >= minLod || (isAtWeEditor && geometryEntity == m_selectedEntity))
                            {
                                availToDraw.Enqueue(new WERenderData
                                {
                                    textDataEntity = nextEntity,
                                    geometryEntity = geometryEntity,
                                    weComponent = weCustomData,
                                    transformMatrix = matrix
                                });
                            }
                        }
                        else
                        {
                            availToDraw.Enqueue(new WERenderData
                            {
                                textDataEntity = nextEntity,
                                geometryEntity = geometryEntity,
                                weComponent = weCustomData,
                                transformMatrix = prevMatrix * Matrix4x4.TRS(refPos, refRot, scale)
                            });
                        }
                        if (m_weSubRefLookup.TryGetBuffer(nextEntity, out var subLayout))
                        {
                            var itemMatrix = prevMatrix * Matrix4x4.TRS(refPos, refRot, Vector3.one);
                            for (int j = 0; j < subLayout.Length; j++)
                            {
                                DrawTree(geometryEntity, subLayout[j].m_weTextData, itemMatrix, unfilteredChunkIndex);
                            }
                        }
                        break;
                }
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
                m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, nextEntity);
            }
        }
    }

}