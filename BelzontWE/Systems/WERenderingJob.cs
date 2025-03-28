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
            public BufferLookup<WETextDataVariable> m_weVariablesLookup;
            public BufferLookup<WETemplateUpdater> m_weTemplateUpdaterLookup;
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
            public ComponentLookup<WETextDataMaterial> m_weMaterialLookup;
            public ComponentLookup<WETextDataMesh> m_weMeshLookup;
            public ComponentLookup<WETextDataTransform> m_weTransformLookup;

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
                    FixedString512Bytes variables = new FixedString512Bytes();
                    PopulateVars(entity, ref variables);
                    if (m_weTemplateForPrefabLookup.TryGetComponent(entity, out var prefabWeLayout))
                    {
                        DrawTree(entity, prefabWeLayout.childEntity, baseMatrix, unfilteredChunkIndex, ref variables, 0);
                    }
                    if (m_weSubRefLookup.TryGetBuffer(entity, out var subLayout))
                    {
                        for (int j = 0; j < subLayout.Length; j++)
                        {
                            DrawTree(entity, subLayout[j].m_weTextData, baseMatrix, unfilteredChunkIndex, ref variables, 0);
                        }

                    }
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

            private void DrawTree(Entity geometryEntity, Entity nextEntity, Matrix4x4 prevMatrix, int unfilteredChunkIndex, ref FixedString512Bytes variables, int nthCall, bool parentIsPlaceholder = false)
            {
                if (nthCall >= 16) return;
                if (!m_weMeshLookup.TryGetComponent(nextEntity, out var mesh))
                {
                    DestroyRecursive(ref this, nextEntity, unfilteredChunkIndex);
                    return;
                }
                var transform = m_weTransformLookup[nextEntity];
                if (transform.useFormulaeToCheckIfDraw && !transform.MustDraw)
                {
                    availToDraw.Enqueue(new WERenderData
                    {
                        transform = transform,
                        textDataEntity = nextEntity,
                        geometryEntity = geometryEntity,
                        main = m_weMainLookup[nextEntity],
                        material = m_weMaterialLookup[nextEntity],
                        mesh = m_weMeshLookup[nextEntity],
                        transformMatrix = default,
                        variables = variables
                    });

                    return;
                }

                PopulateVars(nextEntity, ref variables);
                switch (mesh.TextType)
                {
                    case WESimulationTextType.Archetype:
                        if (m_weSubRefLookup.TryGetBuffer(nextEntity, out var subLayout2))
                        {
                            prevMatrix *= Matrix4x4.TRS(new Vector3(0, 0, .001f), default, Vector3.one);
                            for (int j = 0; j < subLayout2.Length; j++)
                            {
                                DrawTree(geometryEntity, subLayout2[j].m_weTextData, prevMatrix, unfilteredChunkIndex, ref variables, nthCall + 1);
                            }
                        }
                        return;
                    case WESimulationTextType.Placeholder:
                        {
                            if (!m_weTemplateUpdaterLookup.TryGetBuffer(nextEntity, out var updaterBuff))
                            {
                                m_CommandBuffer.AddComponent<WEWaitingRendering>(unfilteredChunkIndex, nextEntity);
                                return;
                            }

                            int lod = CalculateLod(whiteTextureBounds, ref mesh, ref transform, ref prevMatrix, out int minLod, ref this);
                            if (lod >= minLod || (isAtWeEditor && geometryEntity == m_selectedEntity))
                            {
                                var scale2 = transform.scale;
                                var effectiveOffsetPosition = GetEffectiveOffsetPosition(m_weMeshLookup[nextEntity], transform.offsetPosition, new float2(1, 1) - transform.PivotAsFloat2, transform.scale);

                                availToDraw.Enqueue(new WERenderData
                                {
                                    transform = transform,
                                    textDataEntity = nextEntity,
                                    geometryEntity = geometryEntity,
                                    main = m_weMainLookup[nextEntity],
                                    material = m_weMaterialLookup[nextEntity],
                                    mesh = m_weMeshLookup[nextEntity],
                                    transformMatrix = prevMatrix * Matrix4x4.TRS(effectiveOffsetPosition + (float3)Matrix4x4.Rotate(transform.offsetRotation).MultiplyPoint(new float3(0, 0, -.001f)), transform.offsetRotation, scale2),
                                    variables = variables
                                });
                            }
                            for (int i = 0; i < updaterBuff.Length; i++)
                            {
                                var updater = updaterBuff[i];
                                if (updater.childEntity.Index < 0) continue;
                                var layoutVars = new FixedString512Bytes(variables);
                                layoutVars.Append('$');
                                layoutVars.Append('i');
                                layoutVars.Append('d');
                                layoutVars.Append('x');
                                layoutVars.Append(VARIABLE_KV_SEPARATOR);
                                layoutVars.Append(i);
                                layoutVars.Append(VARIABLE_ITEM_SEPARATOR);
                                DrawTree(geometryEntity, updater.childEntity, prevMatrix * Matrix4x4.TRS(transform.offsetPosition, transform.offsetRotation, Vector3.one), unfilteredChunkIndex, ref layoutVars, nthCall + 1, true);
                            }
                        }
                        break;
                    case WESimulationTextType.WhiteTexture:
                        {
                            var material = m_weMaterialLookup[nextEntity];
                            var isDecal = material.CheckIsDecal(mesh);
                            var effRot = isDecal ? ((Quaternion)transform.offsetRotation) * Quaternion.Euler(new Vector3(-90, 180, 0)) : (Quaternion)transform.offsetRotation;
                            var effectiveOffsetPosition = GetEffectiveOffsetPosition(m_weMeshLookup[nextEntity], transform);

                            var WTmatrix = prevMatrix * Matrix4x4.TRS(effectiveOffsetPosition, effRot, Vector3.one) * Matrix4x4.Scale(isDecal ? transform.scale.xzy : new float3(transform.scale.xy, 1));
                            int lod = CalculateLod(whiteTextureBounds, ref mesh, ref transform, ref WTmatrix, out int minLod, ref this);
                            if (lod >= minLod || (isAtWeEditor && geometryEntity == m_selectedEntity))
                            {

                                availToDraw.Enqueue(new WERenderData
                                {
                                    transform = transform,
                                    textDataEntity = nextEntity,
                                    geometryEntity = geometryEntity,
                                    main = m_weMainLookup[nextEntity],
                                    material = material,
                                    mesh = m_weMeshLookup[nextEntity],
                                    transformMatrix = WTmatrix,
                                    variables = variables
                                });
                            }

                            if (m_weSubRefLookup.TryGetBuffer(nextEntity, out var subLayoutWt))
                            {
                                var itemMatrix = prevMatrix * Matrix4x4.TRS(effectiveOffsetPosition + (float3)Matrix4x4.Rotate(transform.offsetRotation).MultiplyPoint(new float3(0, 0, material.Shader == WEShader.Decal ? .002f : .001f)), transform.offsetRotation, Vector3.one);
                                for (int j = 0; j < subLayoutWt.Length; j++)
                                {
                                    DrawTree(geometryEntity, subLayoutWt[j].m_weTextData, itemMatrix, unfilteredChunkIndex, ref variables, nthCall + 1);
                                }
                            }
                        }
                        return;
                    default:
                        {
                            if (m_weTemplateUpdaterLookup.HasBuffer(nextEntity))
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
                            var refPos = GetEffectiveOffsetPosition(m_weMeshLookup[nextEntity], transform.offsetPosition, transform.PivotAsFloat2, scale);
                            var refRot = parentIsPlaceholder ? default : transform.offsetRotation;
                            var material = m_weMaterialLookup[nextEntity];
                            var isDecal = material.CheckIsDecal(mesh);
                            var effRot = parentIsPlaceholder ? default : isDecal ? refRot * Quaternion.Euler(new Vector3(-90, 180, 0)) : (Quaternion)refRot;
                            var matrix = prevMatrix * Matrix4x4.TRS(refPos, effRot, Vector3.one) * Matrix4x4.Scale(isDecal ? (scale.xzy * new float3(mesh.TextType == WESimulationTextType.Image ? mesh.BriWidthMetersUnscaled : 1, 1, 1)) : new float3(scale.xy, 1));
                            if (mesh.HasBRI)
                            {
                                if (!float.IsNaN(matrix.m00) && !float.IsInfinity(matrix.m00))
                                {
                                    int lod = CalculateLod(mesh.Bounds, ref mesh, ref transform, ref matrix, out int minLod, ref this);
                                    if (lod >= minLod || (isAtWeEditor && geometryEntity == m_selectedEntity))
                                    {
                                        availToDraw.Enqueue(new WERenderData
                                        {
                                            transform = transform,
                                            textDataEntity = nextEntity,
                                            geometryEntity = geometryEntity,
                                            main = m_weMainLookup[nextEntity],
                                            material = m_weMaterialLookup[nextEntity],
                                            mesh = mesh,
                                            transformMatrix = matrix,
                                            variables = variables
                                        });
                                    }
                                }
                            }
                            else
                            {
                                availToDraw.Enqueue(new WERenderData
                                {
                                    transform = transform,
                                    textDataEntity = nextEntity,
                                    geometryEntity = geometryEntity,
                                    main = m_weMainLookup[nextEntity],
                                    material = m_weMaterialLookup[nextEntity],
                                    mesh = mesh,
                                    transformMatrix = matrix,
                                    variables = variables
                                });
                            }
                            if (m_weSubRefLookup.TryGetBuffer(nextEntity, out var subLayout))
                            {
                                var itemMatrix = prevMatrix * Matrix4x4.TRS(refPos + (float3)Matrix4x4.Rotate(refRot).MultiplyPoint(new float3(0, 0, material.Shader == WEShader.Decal ? .002f : .001f)), refRot, Vector3.one);
                                for (int j = 0; j < subLayout.Length; j++)
                                {
                                    DrawTree(geometryEntity, subLayout[j].m_weTextData, itemMatrix, unfilteredChunkIndex, ref variables, nthCall + 1);
                                }
                            }
                        }
                        break;
                }
            }

            private readonly float3 GetEffectiveOffsetPosition(WETextDataMesh meshData, WETextDataTransform transform)
            {
                return GetEffectiveOffsetPosition(meshData, transform.offsetPosition, transform.PivotAsFloat2, transform.scale);
            }

            private readonly float3 GetEffectiveOffsetPosition(WETextDataMesh meshData, float3 offsetPosition, float2 pivot, float3 scale)
            {
                var effectiveOffsetPosition = offsetPosition;
                var meshSize = meshData.Bounds.max - meshData.Bounds.min;
                effectiveOffsetPosition += new float3(pivot - new float2(.5f, .5f), 0) * meshSize * scale;

                return effectiveOffsetPosition;
            }

            private static int CalculateLod(Bounds3 meshBounds, ref WETextDataMesh meshData, ref WETextDataTransform transformData, ref Matrix4x4 matrix, out int minLod, ref WERenderingJob job)
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

            private static void DestroyRecursive(ref WERenderingJob job, Entity nextEntity, int unfilteredChunkIndex, Entity initialDelete = default)
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

}