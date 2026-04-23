using Belzont.Interfaces;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.Tools;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;


namespace BelzontWE
{
    /// <summary>
    /// Spawns and repositions GameProp instances at Modification2B (same phase as the game's SubObjectSystem).
    /// Running at this phase ensures entities created here are properly initialized by the game's
    /// Modification-phase initialization pipelines before rendering occurs.
    /// </summary>
    public partial class WEGamePropSpawnSystem : BelzontBasicSystem
    {
        protected override AllowedPhase UpdatePhase => AllowedPhase.Modification2B;

        private EntityQuery m_pendingGamePropEntities;
        private WETemplateManager m_templateManager;

        protected override void OnCreateWithBarrier()
        {
            m_pendingGamePropEntities = GetEntityQuery(new EntityQueryDesc[]
            {
                new()
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadWrite<WETextDataMain>(),
                        ComponentType.ReadWrite<WETextDataMesh>(),
                        ComponentType.ReadOnly<WEWaitingGamePropRendering>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Deleted>(),
                    }
                }
            });

            m_templateManager = World.GetOrCreateSystemManaged<WETemplateManager>();
            RequireAnyForUpdate(m_pendingGamePropEntities);
        }

        protected override void OnUpdate()
        {
            new WEGamePropSpawnJob
            {
                m_EntityType = GetEntityTypeHandle(),
                m_entityLookup = GetEntityStorageInfoLookup(),
                m_CommandBuffer = Barrier.CreateCommandBuffer().AsParallelWriter(),
                m_dataMainHdl = GetComponentTypeHandle<WETextDataMain>(true),
                m_dataMeshHdl = GetComponentTypeHandle<WETextDataMesh>(false),
                m_WeMainLkp = GetComponentLookup<WETextDataMain>(true),
                GamePropIndexPtr = GCHandle.Alloc(m_templateManager.WEGamePropIndex),
                m_SubObjectLkp = GetBufferLookup<WESubObject>(true),
                m_PrefabRefLkp = GetComponentLookup<PrefabRef>(true),
                m_WeTransformLkp = GetComponentLookup<WETextDataTransform>(true),
                m_GameTransformLkp = GetComponentLookup<Game.Objects.Transform>(true),
                m_InterpolatedTransformLkp = GetComponentLookup<InterpolatedTransform>(true),
            }.Schedule(m_pendingGamePropEntities, Dependency).Complete();
        }

        private struct WEGamePropSpawnJob : IJobChunk
        {
            public EntityTypeHandle m_EntityType;
            [ReadOnly] public ComponentTypeHandle<WETextDataMain> m_dataMainHdl;
            public ComponentTypeHandle<WETextDataMesh> m_dataMeshHdl;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
            [ReadOnly] public ComponentLookup<WETextDataMain> m_WeMainLkp;
            public EntityStorageInfoLookup m_entityLookup;
            public GCHandle GamePropIndexPtr;
            [ReadOnly] public BufferLookup<WESubObject> m_SubObjectLkp;
            [ReadOnly] public ComponentLookup<PrefabRef> m_PrefabRefLkp;
            [ReadOnly] public ComponentLookup<WETextDataTransform> m_WeTransformLkp;
            [ReadOnly] public ComponentLookup<Game.Objects.Transform> m_GameTransformLkp;
            [ReadOnly] public ComponentLookup<InterpolatedTransform> m_InterpolatedTransformLkp;
            [ReadOnly] public ComponentLookup<WEInheritedVarsCache> m_inheritedVarsCacheLkp;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(m_EntityType);
                var weTextDatas = chunk.GetNativeArray(ref m_dataMainHdl);
                var weMeshDatas = chunk.GetNativeArray(ref m_dataMeshHdl);

                for (var i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    var weCustomData = weTextDatas[i];
                    var weMeshData = weMeshDatas[i];

                    m_CommandBuffer.SetComponentEnabled<WEWaitingGamePropRendering>(unfilteredChunkIndex, entity, false);
                    if (weMeshData.TextType != WESimulationTextType.GameProp) continue;

                    if (!m_entityLookup.Exists(weCustomData.TargetEntity) || weCustomData.TargetEntity == Entity.Null)
                    {
                        m_CommandBuffer.AddComponent<Deleted>(unfilteredChunkIndex, entity);
                        continue;
                    }
                    var inheritedVars = m_inheritedVarsCacheLkp.TryGetComponent(entity, out var cache) ? cache : default;
                    SpawnOrUpdateGameProp(entity, ref weMeshData, unfilteredChunkIndex, m_CommandBuffer, inheritedVars);
                    m_CommandBuffer.SetComponent(unfilteredChunkIndex, entity, weMeshData);
                }
            }

            private void ClearGamePropSubObjects(Entity entity, int unfilteredChunkIndex, EntityCommandBuffer.ParallelWriter cmd, out DynamicBuffer<WESubObject> newBuf)
            {

                if (!m_SubObjectLkp.TryGetBuffer(entity, out newBuf))
                {
                    newBuf = cmd.AddBuffer<WESubObject>(unfilteredChunkIndex, entity);
                    return;
                }
                if (newBuf.Length == 0) return;
                for (int j = 0; j < newBuf.Length; j++)
                {
                    var sub = newBuf[j].m_SubObject;
                    if (sub != Entity.Null) cmd.AddComponent<Deleted>(unfilteredChunkIndex, sub);
                }
                newBuf = cmd.SetBuffer<WESubObject>(unfilteredChunkIndex, entity);
                newBuf.Clear();
            }

            private void SpawnOrUpdateGameProp(Entity entity, ref WETextDataMesh weMeshData, int unfilteredChunkIndex, EntityCommandBuffer.ParallelWriter cmd, WEInheritedVarsCache inheritedVars)
            {
                var gameIndex = GamePropIndexPtr.IsAllocated ? GamePropIndexPtr.Target as Dictionary<string, Entity> : null;
                if (gameIndex == null) return;

                var currentPrefabName = weMeshData.ValueData.EffectiveValue.ToString();
                gameIndex.TryGetValue(currentPrefabName, out var prefabEntity);
                var hasValidPrefab = !string.IsNullOrEmpty(currentPrefabName) && prefabEntity != Entity.Null;

                // Get geometry/text-node transform
                m_WeMainLkp.TryGetComponent(entity, out var main);
                var geomTransform = default(Game.Objects.Transform);
                m_GameTransformLkp.TryGetComponent(main.TargetEntity, out geomTransform);
                var hasGeom = main.TargetEntity != Entity.Null && m_entityLookup.Exists(main.TargetEntity);
                m_WeTransformLkp.TryGetComponent(entity, out var weTransform);

                // Read existing sub-objects
                m_SubObjectLkp.TryGetBuffer(entity, out var existingBuf);
                int existingCount = existingBuf.IsCreated ? existingBuf.Length : 0;

                // Block GameProp on Moveable Objects (InterpolatedTransform geometry)
                if (m_InterpolatedTransformLkp.HasComponent(main.TargetEntity))
                {
                    ClearGamePropSubObjects(entity, unfilteredChunkIndex, cmd, out _);
                    return;
                }

                if (!hasValidPrefab)
                {
                    ClearGamePropSubObjects(entity, unfilteredChunkIndex, cmd, out _);
                    return;
                }

                // Check if all existing sub-objects use the correct prefab
                bool allSamePrefab = existingCount > 0;
                for (int j = 0; allSamePrefab && j < existingCount; j++)
                {
                    var sub = existingBuf[j].m_SubObject;
                    if (!m_entityLookup.Exists(sub) || !m_PrefabRefLkp.TryGetComponent(sub, out var subPrefabRef) || subPrefabRef.m_Prefab != prefabEntity)
                    {
                        allSamePrefab = false;
                        break;
                    }
                }

                // Compute instancing layout
                var instCount = weTransform.InstanceCountFn.EffectiveValue;
                uint targetSize = instCount < 0
                    ? (uint)math.clamp(weTransform.ArrayInstancing.x * weTransform.ArrayInstancing.y * weTransform.ArrayInstancing.z, 1, 256)
                    : (uint)math.min(256, (uint)instCount);
                if (targetSize == 0) targetSize = 1;

                var spacingOffsets = weTransform.SpacingByAxisOrder;
                var instancingCount = (uint3)math.min(weTransform.InstanceCountByAxisOrder,
                    math.ceil(targetSize / new float3(1,
                        weTransform.InstanceCountByAxisOrder[0],
                        weTransform.InstanceCountByAxisOrder[0] * weTransform.InstanceCountByAxisOrder[1])));
                var totalArea = (weTransform.ArrayInstancing - 1) * weTransform.arrayInstancingGapMeters;
                var effectivePivot = weTransform.PivotAsFloat3 - (math.sign(totalArea.xyz) / 2) - .5f;
                var pivotOffset = effectivePivot * math.abs(totalArea);
                var alignmentByAxisOrder = weTransform.AlignmentByAxisOrder;

                var geomMtx = hasGeom
                    ? float4x4.TRS(geomTransform.m_Position, geomTransform.m_Rotation, new float3(1f))
                    : float4x4.identity;
                var baseRot = hasGeom
                    ? math.mul(geomTransform.m_Rotation, weTransform.offsetRotation)
                    : weTransform.offsetRotation;

                if (!allSamePrefab)
                {
                    // Prefab changed — clear all, then spawn fresh
                    ClearGamePropSubObjects(entity, unfilteredChunkIndex, cmd, out _);
                    SpawnAllInstances(entity, prefabEntity, (int)targetSize, instancingCount, spacingOffsets, pivotOffset, alignmentByAxisOrder, weTransform, geomMtx, baseRot, unfilteredChunkIndex, cmd, inheritedVars);
                    return;
                }

                if (existingCount == (int)targetSize)
                {
                    // Count matches, prefab matches — reposition only
                    RepositionInstances(existingBuf, existingCount, instancingCount, spacingOffsets, pivotOffset, alignmentByAxisOrder, weTransform, geomMtx, baseRot, (int)targetSize, hasGeom, unfilteredChunkIndex, cmd);
                    return;
                }

                if (existingCount > (int)targetSize)
                {
                    // Too many — delete excess, shrink buffer, reposition remaining
                    for (int j = (int)targetSize; j < existingCount; j++)
                    {
                        var sub = existingBuf[j].m_SubObject;
                        if (sub != Entity.Null) cmd.AddComponent<Deleted>(unfilteredChunkIndex, sub);
                    }
                    var newBuf = cmd.SetBuffer<WESubObject>(unfilteredChunkIndex, entity);
                    for (int j = 0; j < (int)targetSize; j++) newBuf.Add(existingBuf[j]);
                    RepositionInstances(existingBuf, (int)targetSize, instancingCount, spacingOffsets, pivotOffset, alignmentByAxisOrder, weTransform, geomMtx, baseRot, (int)targetSize, hasGeom, unfilteredChunkIndex, cmd);
                    return;
                }

                // existingCount < targetSize — spawn delta at correct grid positions, reposition existing
                RepositionInstances(existingBuf, existingCount, instancingCount, spacingOffsets, pivotOffset, alignmentByAxisOrder, weTransform, geomMtx, baseRot, (int)targetSize, hasGeom, unfilteredChunkIndex, cmd);
                SpawnAllInstances(entity, prefabEntity, (int)targetSize, instancingCount, spacingOffsets, pivotOffset, alignmentByAxisOrder, weTransform, geomMtx, baseRot, unfilteredChunkIndex, cmd, inheritedVars, existingCount);
            }

            private void SpawnAllInstances(Entity entity, Entity prefabEntity, int targetSize, uint3 instancingCount,
                float3[] spacingOffsets, float3 pivotOffset,
                (WEPlacementAlignment m, WEPlacementAlignment n, WEPlacementAlignment o) alignmentByAxisOrder,
                WETextDataTransform weTransform, float4x4 geomMtx, quaternion baseRot,
                int unfilteredChunkIndex, EntityCommandBuffer.ParallelWriter cmd, WEInheritedVarsCache inheritedVarsCache, int startIndex = 0)
            {
                if (spacingOffsets != null)
                {
                    int spawned = 0;
                    for (int o = 0; o < instancingCount.z && spawned < targetSize; o++)
                    {
                        var spacingO = spacingOffsets[2];
                        WETemplateManager.GetSpacingAndOffset((uint)(targetSize - spawned), instancingCount.z, instancingCount.y * instancingCount.x, alignmentByAxisOrder.o, ref spacingO, out var offsetO);
                        for (int n = 0; n < instancingCount.y && spawned < targetSize; n++)
                        {
                            var spacingN = spacingOffsets[1];
                            WETemplateManager.GetSpacingAndOffset((uint)(targetSize - spawned), instancingCount.y, instancingCount.x, alignmentByAxisOrder.n, ref spacingN, out var offsetN);
                            for (int m2 = 0; m2 < instancingCount.x && spawned < targetSize; m2++, spawned++)
                            {
                                if (spawned < startIndex) continue;
                                var spacingM = spacingOffsets[0];
                                WETemplateManager.GetSpacingAndOffset((uint)(targetSize - spawned), instancingCount.x, 1, alignmentByAxisOrder.m, ref spacingM, out var offsetM);
                                var localPos = weTransform.offsetPosition + pivotOffset + offsetM + offsetN + offsetO + (m2 * spacingM) + (n * spacingN) + (o * spacingO);
                                SpawnPropInstance(entity, prefabEntity, math.transform(geomMtx, localPos), baseRot, unfilteredChunkIndex, cmd, inheritedVarsCache);
                            }
                        }
                    }
                }
                else
                {
                    var pos = math.transform(geomMtx, weTransform.offsetPosition);
                    for (int idx = startIndex; idx < targetSize; idx++)
                        SpawnPropInstance(entity, prefabEntity, pos, baseRot, unfilteredChunkIndex, cmd, inheritedVarsCache);
                }
            }

            private void RepositionInstances(DynamicBuffer<WESubObject> existingBuf, int count, uint3 instancingCount,
                float3[] spacingOffsets, float3 pivotOffset,
                (WEPlacementAlignment m, WEPlacementAlignment n, WEPlacementAlignment o) alignmentByAxisOrder,
                WETextDataTransform weTransform, float4x4 geomMtx, quaternion baseRot,
                int totalTarget, bool hasGeom, int unfilteredChunkIndex, EntityCommandBuffer.ParallelWriter cmd)
            {
                if (count == 0 || !hasGeom) return;
                if (spacingOffsets != null)
                {
                    int idx = 0;
                    for (int o = 0; o < instancingCount.z && idx < count; o++)
                    {
                        var spacingO = spacingOffsets[2];
                        WETemplateManager.GetSpacingAndOffset((uint)(totalTarget - idx), instancingCount.z, instancingCount.y * instancingCount.x, alignmentByAxisOrder.o, ref spacingO, out var offsetO);
                        for (int n = 0; n < instancingCount.y && idx < count; n++)
                        {
                            var spacingN = spacingOffsets[1];
                            WETemplateManager.GetSpacingAndOffset((uint)(totalTarget - idx), instancingCount.y, instancingCount.x, alignmentByAxisOrder.n, ref spacingN, out var offsetN);
                            for (int m2 = 0; m2 < instancingCount.x && idx < count; m2++, idx++)
                            {
                                var spacingM = spacingOffsets[0];
                                WETemplateManager.GetSpacingAndOffset((uint)(totalTarget - idx), instancingCount.x, 1, alignmentByAxisOrder.m, ref spacingM, out var offsetM);
                                var localPos = weTransform.offsetPosition + pivotOffset + offsetM + offsetN + offsetO + (m2 * spacingM) + (n * spacingN) + (o * spacingO);
                                cmd.SetComponent(unfilteredChunkIndex, existingBuf[idx].m_SubObject, new Game.Objects.Transform(math.transform(geomMtx, localPos), baseRot));
                            }
                        }
                    }
                }
                else
                {
                    var worldPos = math.transform(geomMtx, weTransform.offsetPosition);
                    for (int j = 0; j < count; j++)
                        cmd.SetComponent(unfilteredChunkIndex, existingBuf[j].m_SubObject, new Game.Objects.Transform(worldPos, baseRot));
                }
            }

            private void SpawnPropInstance(Entity textNode, Entity prefabEntity, float3 worldPos, quaternion worldRot, int unfilteredChunkIndex, EntityCommandBuffer.ParallelWriter cmd, WEInheritedVarsCache inheritedVarsCache)
            {
                var spawnedEntity = cmd.Instantiate(unfilteredChunkIndex, prefabEntity);
                cmd.SetComponent(unfilteredChunkIndex, spawnedEntity, new Game.Objects.Transform(worldPos, worldRot));
                cmd.AddComponent(unfilteredChunkIndex, spawnedEntity, new WEOwner { m_weOwnerEntity = textNode });
                cmd.AddComponent<WEChild>(unfilteredChunkIndex, spawnedEntity);
                cmd.AddComponent<WEInheritedVarsCache>(unfilteredChunkIndex, spawnedEntity);
                cmd.AddComponent<Secondary>(unfilteredChunkIndex, spawnedEntity);
                cmd.AddComponent(unfilteredChunkIndex, spawnedEntity, inheritedVarsCache);
                cmd.AppendToBuffer(unfilteredChunkIndex, textNode, new WESubObject { m_SubObject = spawnedEntity });
            }
        }
    }
}
