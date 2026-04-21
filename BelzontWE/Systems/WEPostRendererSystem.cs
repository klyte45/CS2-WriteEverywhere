using Belzont.Interfaces;
using Belzont.Utils;
using BelzontWE.Font;
using BelzontWE.Font.Utility;
using BelzontWE.Sprites;
using Game;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.SceneFlow;
using Game.Tools;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;


#if BURST
#endif


namespace BelzontWE
{
    //System that will prepare the next frame meshes
    public partial class WEPostRendererSystem : BelzontBasicSystem
    {
        protected override AllowedPhase UpdatePhase => AllowedPhase.EndFrame;
        private EntityQuery m_pendingQueueEntities;
        private WETemplateManager m_templateManager;

        protected override void OnCreateWithBarrier()
        {
            m_pendingQueueEntities = GetEntityQuery(new EntityQueryDesc[]
            {
                new ()
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadWrite<WETextDataMain>(),
                        ComponentType.ReadWrite<WETextDataMesh>(),
                        ComponentType.ReadOnly<WEWaitingRendering>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Deleted>(),
                    }
                }
            });

            m_templateManager = World.GetOrCreateSystemManaged<WETemplateManager>();
            RequireAnyForUpdate(m_pendingQueueEntities);
        }
        protected override void OnUpdate()
        {
            if (GameManager.instance.isGameLoading) return;
            if (!m_pendingQueueEntities.IsEmpty)
            {
                var cmdBuff = Barrier.CreateCommandBuffer();
                var layoutsAvailable = new NativeArray<FixedString128Bytes>(m_templateManager.GetTemplateAvailableKeys(), Allocator.TempJob);
                new WETextImageDataUpdateJob
                {
                    m_EntityType = GetEntityTypeHandle(),
                    m_entityLookup = GetEntityStorageInfoLookup(),
                    m_templateUpdaterLkp = GetBufferLookup<WETemplateUpdater>(true),
                    FontDictPtr = FontServer.Instance.DictPtr,
                    m_CommandBuffer = cmdBuff.AsParallelWriter(),
                    m_dataMainHdl = GetComponentTypeHandle<WETextDataMain>(),
                    m_dataMeshHdl = GetComponentTypeHandle<WETextDataMesh>(),
                    m_WeMeshLkp = GetComponentLookup<WETextDataMesh>(true),
                    m_WeMainLkp = GetComponentLookup<WETextDataMain>(true),
                    m_WeIsPlaceholderLkp = GetComponentLookup<WEIsPlaceholder>(true),
                    m_templateManagerEntries = layoutsAvailable,
                    GamePropIndexPtr = GCHandle.Alloc(m_templateManager.WEGamePropIndex),
                    m_SubObjectLkp = GetBufferLookup<WESubObject>(true),
                    m_PrefabRefLkp = GetComponentLookup<PrefabRef>(true),
                    m_WeTransformLkp = GetComponentLookup<WETextDataTransform>(true),
                    m_GameTransformLkp = GetComponentLookup<Game.Objects.Transform>(true),
                }.Schedule(m_pendingQueueEntities, Dependency).Complete();

                layoutsAvailable.Dispose();
            }
        }

        private static IBasicRenderInformation cachedEmpty;

        private unsafe struct WETextImageDataUpdateJob : IJobChunk
        {
            public EntityTypeHandle m_EntityType;
            public ComponentTypeHandle<WETextDataMain> m_dataMainHdl;
            public ComponentTypeHandle<WETextDataMesh> m_dataMeshHdl;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
            public ComponentLookup<WETextDataMain> m_WeMainLkp;
            public ComponentLookup<WETextDataMesh> m_WeMeshLkp;
            public ComponentLookup<WEIsPlaceholder> m_WeIsPlaceholderLkp;
            public GCHandle FontDictPtr;
            public EntityStorageInfoLookup m_entityLookup;
            public BufferLookup<WETemplateUpdater> m_templateUpdaterLkp;
            public NativeArray<FixedString128Bytes> m_templateManagerEntries;
            public GCHandle GamePropIndexPtr;
            [ReadOnly] public BufferLookup<WESubObject> m_SubObjectLkp;
            [ReadOnly] public ComponentLookup<PrefabRef> m_PrefabRefLkp;
            [ReadOnly] public ComponentLookup<WETextDataTransform> m_WeTransformLkp;
            [ReadOnly] public ComponentLookup<Game.Objects.Transform> m_GameTransformLkp;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(m_EntityType);
                var weTextDatas = chunk.GetNativeArray(ref m_dataMainHdl);
                var weMeshDatas = chunk.GetNativeArray(ref m_dataMeshHdl);
                var fontDict = FontDictPtr.Target as Dictionary<FixedString64Bytes, FontSystemData>;

                for (var i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    var weCustomData = weTextDatas[i];
                    var weMeshData = weMeshDatas[i];
                    if (!m_entityLookup.Exists(weCustomData.TargetEntity) || weCustomData.TargetEntity == Entity.Null)
                    {
                        if (BasicIMod.DebugMode) LogUtils.DoLog($"Destroy Entity! {entity} - Target doesntExists");
                        m_CommandBuffer.AddComponent<Game.Common.Deleted>(unfilteredChunkIndex, entity);
                        return;
                    }

                    weMeshData.ClearTemplateDirty();
                    switch (weMeshData.TextType)
                    {
                        case WESimulationTextType.Text:
                            if (UpdateTextMesh(entity, ref weMeshData, weMeshData.ValueData.EffectiveValue.ToString(), unfilteredChunkIndex, m_CommandBuffer, fontDict))
                            {
                                if (m_WeIsPlaceholderLkp.HasComponent(entity)) m_CommandBuffer.SetComponentEnabled<WEIsPlaceholder>(unfilteredChunkIndex, entity, false);
                                m_CommandBuffer.SetComponent(unfilteredChunkIndex, entity, weMeshData);
                                m_CommandBuffer.SetComponentEnabled<WEWaitingRendering>(unfilteredChunkIndex, entity, false);
                            }
                            ClearGamePropSubObjects(entity, unfilteredChunkIndex, m_CommandBuffer);
                            break;
                        case WESimulationTextType.Image:
                            if (UpdateImageMesh(entity, ref weMeshData, weMeshData.ValueData.EffectiveValue.ToString(), unfilteredChunkIndex, m_CommandBuffer))
                            {
                                if (m_WeIsPlaceholderLkp.HasComponent(entity)) m_CommandBuffer.SetComponentEnabled<WEIsPlaceholder>(unfilteredChunkIndex, entity, false);
                                m_CommandBuffer.SetComponent(unfilteredChunkIndex, entity, weMeshData);
                                m_CommandBuffer.SetComponentEnabled<WEWaitingRendering>(unfilteredChunkIndex, entity, false);
                            }
                            ClearGamePropSubObjects(entity, unfilteredChunkIndex, m_CommandBuffer);
                            break;
                        case WESimulationTextType.Placeholder:
                            if (UpdatePlaceholder(entity, ref weCustomData, weMeshData.ValueData.EffectiveValue.ToString(), unfilteredChunkIndex, m_CommandBuffer))
                            {
                                if (!m_WeIsPlaceholderLkp.HasComponent(entity))
                                {
                                    m_CommandBuffer.AddComponent<WEIsPlaceholder>(unfilteredChunkIndex, entity);
                                    m_CommandBuffer.SetComponentEnabled<WEIsPlaceholder>(unfilteredChunkIndex, entity, true);
                                    m_CommandBuffer.AddComponent<WETemplateDirtyInstancing>(unfilteredChunkIndex, entity);
                                    m_CommandBuffer.SetComponentEnabled<WETemplateDirtyInstancing>(unfilteredChunkIndex, entity, true);
                                }
                                m_CommandBuffer.SetComponent(unfilteredChunkIndex, entity, weCustomData);
                                m_CommandBuffer.SetComponent(unfilteredChunkIndex, entity, weMeshData);
                                m_CommandBuffer.SetComponentEnabled<WEWaitingRendering>(unfilteredChunkIndex, entity, false);
                            }
                            ClearGamePropSubObjects(entity, unfilteredChunkIndex, m_CommandBuffer);
                            break;
                        case WESimulationTextType.GameProp:
                            SpawnOrUpdateGameProp(entity, ref weMeshData, unfilteredChunkIndex, m_CommandBuffer);
                            if (m_WeIsPlaceholderLkp.HasComponent(entity)) m_CommandBuffer.SetComponentEnabled<WEIsPlaceholder>(unfilteredChunkIndex, entity, false);
                            m_CommandBuffer.SetComponent(unfilteredChunkIndex, entity, weMeshData);
                            m_CommandBuffer.SetComponentEnabled<WEWaitingRendering>(unfilteredChunkIndex, entity, false);
                            break;
                        default:
                            if (m_WeIsPlaceholderLkp.HasComponent(entity)) m_CommandBuffer.SetComponentEnabled<WEIsPlaceholder>(unfilteredChunkIndex, entity, false);
                            m_CommandBuffer.SetComponent(unfilteredChunkIndex, entity, weMeshData);
                            ClearGamePropSubObjects(entity, unfilteredChunkIndex, m_CommandBuffer);
                            m_CommandBuffer.SetComponentEnabled<WEWaitingRendering>(unfilteredChunkIndex, entity, false);
                            break;

                    }
                }
            }
            private void ClearGamePropSubObjects(Entity entity, int unfilteredChunkIndex, EntityCommandBuffer.ParallelWriter cmd)
            {
                if (!m_SubObjectLkp.TryGetBuffer(entity, out var subBuf) || subBuf.Length == 0) return;
                for (int j = 0; j < subBuf.Length; j++)
                {
                    var sub = subBuf[j].m_SubObject;
                    if (sub != Entity.Null) cmd.AddComponent<Game.Common.Deleted>(unfilteredChunkIndex, sub);
                }
                cmd.SetBuffer<WESubObject>(unfilteredChunkIndex, entity).Clear();
            }

            private void SpawnOrUpdateGameProp(Entity entity, ref WETextDataMesh weMeshData, int unfilteredChunkIndex, EntityCommandBuffer.ParallelWriter cmd)
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

                if (!hasValidPrefab)
                {
                    if (existingCount > 0) ClearGamePropSubObjects(entity, unfilteredChunkIndex, cmd);
                    return;
                }

                // Check if all existing sub-objects use the correct prefab
                bool allSamePrefab = existingCount > 0;
                for (int j = 0; allSamePrefab && j < existingCount; j++)
                {
                    var sub = existingBuf[j].m_SubObject;
                    if (!m_entityLookup.Exists(sub) || !m_PrefabRefLkp.TryGetComponent(sub, out var subPrefabRef) || subPrefabRef.m_Prefab != prefabEntity)
                        allSamePrefab = false;
                }

                // Compute instancing layout
                var instCount = weTransform.InstanceCountFn.EffectiveValue;
                uint targetSize = instCount < 0
                    ? (uint)Unity.Mathematics.math.clamp(weTransform.ArrayInstancing.x * weTransform.ArrayInstancing.y * weTransform.ArrayInstancing.z, 1, 256)
                    : (uint)Unity.Mathematics.math.min(256, (uint)instCount);
                if (targetSize == 0) targetSize = 1;

                var spacingOffsets = weTransform.SpacingByAxisOrder;
                var instancingCount = (uint3)Unity.Mathematics.math.min(weTransform.InstanceCountByAxisOrder,
                    Unity.Mathematics.math.ceil(targetSize / new Unity.Mathematics.float3(1,
                        weTransform.InstanceCountByAxisOrder[0],
                        weTransform.InstanceCountByAxisOrder[0] * weTransform.InstanceCountByAxisOrder[1])));
                var totalArea = (weTransform.ArrayInstancing - 1) * weTransform.arrayInstancingGapMeters;
                var effectivePivot = weTransform.PivotAsFloat3 - (Unity.Mathematics.math.sign(totalArea.xyz) / 2) - .5f;
                var pivotOffset = effectivePivot * Unity.Mathematics.math.abs(totalArea);
                var alignmentByAxisOrder = weTransform.AlignmentByAxisOrder;

                var geomMtx = hasGeom
                    ? Unity.Mathematics.float4x4.TRS(geomTransform.m_Position, geomTransform.m_Rotation, new Unity.Mathematics.float3(1f))
                    : Unity.Mathematics.float4x4.identity;
                var baseRot = hasGeom
                    ? Unity.Mathematics.math.mul(geomTransform.m_Rotation, weTransform.offsetRotation)
                    : weTransform.offsetRotation;

                if (!allSamePrefab)
                {
                    // Prefab changed — clear all, then spawn fresh
                    if (existingCount > 0) ClearGamePropSubObjects(entity, unfilteredChunkIndex, cmd);
                    SpawnAllInstances(entity, prefabEntity, (int)targetSize, instancingCount, spacingOffsets, pivotOffset, alignmentByAxisOrder, weTransform, geomMtx, baseRot, unfilteredChunkIndex, cmd);
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
                        if (sub != Entity.Null) cmd.AddComponent<Game.Common.Deleted>(unfilteredChunkIndex, sub);
                    }
                    var newBuf = cmd.SetBuffer<WESubObject>(unfilteredChunkIndex, entity);
                    for (int j = 0; j < (int)targetSize; j++) newBuf.Add(existingBuf[j]);
                    RepositionInstances(existingBuf, (int)targetSize, instancingCount, spacingOffsets, pivotOffset, alignmentByAxisOrder, weTransform, geomMtx, baseRot, (int)targetSize, hasGeom, unfilteredChunkIndex, cmd);
                    return;
                }

                // existingCount < targetSize — spawn delta, reposition existing in buffer
                RepositionInstances(existingBuf, existingCount, instancingCount, spacingOffsets, pivotOffset, alignmentByAxisOrder, weTransform, geomMtx, baseRot, (int)targetSize, hasGeom, unfilteredChunkIndex, cmd);
                int missing = (int)targetSize - existingCount;
                var fallbackPos = Unity.Mathematics.math.transform(geomMtx, weTransform.offsetPosition);
                for (int j = 0; j < missing; j++)
                    SpawnPropInstance(entity, prefabEntity, fallbackPos, baseRot, unfilteredChunkIndex, cmd);
            }

            private void SpawnAllInstances(Entity entity, Entity prefabEntity, int targetSize, uint3 instancingCount,
                Unity.Mathematics.float3[] spacingOffsets, Unity.Mathematics.float3 pivotOffset,
                (WEPlacementAlignment m, WEPlacementAlignment n, WEPlacementAlignment o) alignmentByAxisOrder,
                WETextDataTransform weTransform, Unity.Mathematics.float4x4 geomMtx, Unity.Mathematics.quaternion baseRot,
                int unfilteredChunkIndex, EntityCommandBuffer.ParallelWriter cmd)
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
                                var spacingM = spacingOffsets[0];
                                WETemplateManager.GetSpacingAndOffset((uint)(targetSize - spawned), instancingCount.x, 1, alignmentByAxisOrder.m, ref spacingM, out var offsetM);
                                var localPos = weTransform.offsetPosition + pivotOffset + offsetM + offsetN + offsetO + (m2 * spacingM) + (n * spacingN) + (o * spacingO);
                                SpawnPropInstance(entity, prefabEntity, Unity.Mathematics.math.transform(geomMtx, localPos), baseRot, unfilteredChunkIndex, cmd);
                            }
                        }
                    }
                }
                else
                {
                    var pos = Unity.Mathematics.math.transform(geomMtx, weTransform.offsetPosition);
                    for (int idx = 0; idx < targetSize; idx++)
                        SpawnPropInstance(entity, prefabEntity, pos, baseRot, unfilteredChunkIndex, cmd);
                }
            }

            private void RepositionInstances(DynamicBuffer<WESubObject> existingBuf, int count, uint3 instancingCount,
                Unity.Mathematics.float3[] spacingOffsets, Unity.Mathematics.float3 pivotOffset,
                (WEPlacementAlignment m, WEPlacementAlignment n, WEPlacementAlignment o) alignmentByAxisOrder,
                WETextDataTransform weTransform, Unity.Mathematics.float4x4 geomMtx, Unity.Mathematics.quaternion baseRot,
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
                                cmd.SetComponent(unfilteredChunkIndex, existingBuf[idx].m_SubObject, new Game.Objects.Transform(Unity.Mathematics.math.transform(geomMtx, localPos), baseRot));
                            }
                        }
                    }
                }
                else
                {
                    var worldPos = Unity.Mathematics.math.transform(geomMtx, weTransform.offsetPosition);
                    for (int j = 0; j < count; j++)
                        cmd.SetComponent(unfilteredChunkIndex, existingBuf[j].m_SubObject, new Game.Objects.Transform(worldPos, baseRot));
                }
            }

            private void SpawnPropInstance(Entity textNode, Entity prefabEntity, Unity.Mathematics.float3 worldPos, Unity.Mathematics.quaternion worldRot, int unfilteredChunkIndex, EntityCommandBuffer.ParallelWriter cmd)
            {
                var spawnedEntity = cmd.Instantiate(unfilteredChunkIndex, prefabEntity);
                cmd.SetComponent(unfilteredChunkIndex, spawnedEntity, new Game.Objects.Transform(worldPos, worldRot));
                cmd.AddComponent(unfilteredChunkIndex, spawnedEntity, new WEOwner { m_weOwnerEntity = textNode });
                cmd.AddComponent<WEChild>(unfilteredChunkIndex, spawnedEntity);
                cmd.AddComponent<WEInheritedVarsCache>(unfilteredChunkIndex, spawnedEntity);
                cmd.SetComponentEnabled<WEInheritedVarsCache>(unfilteredChunkIndex, spawnedEntity, false);
                cmd.AddComponent<Secondary>(unfilteredChunkIndex, spawnedEntity);
                cmd.AppendToBuffer(unfilteredChunkIndex, textNode, new WESubObject { m_SubObject = spawnedEntity });
            }

            private bool UpdatePlaceholder(Entity e, ref WETextDataMain weCustomData, string templateName, int unfilteredChunkIndex, EntityCommandBuffer.ParallelWriter cmd)
            {
                if (!m_entityLookup.Exists(weCustomData.TargetEntity)
                      || (m_WeMeshLkp.TryGetComponent(weCustomData.ParentEntity, out var weDataParent) && weDataParent.TextType == WESimulationTextType.Placeholder)
                      || (m_WeMeshLkp.TryGetComponent(weCustomData.TargetEntity, out weDataParent) && weDataParent.TextType == WESimulationTextType.Placeholder)
                      || (weCustomData.TargetEntity == Entity.Null))
                {
#if !BURST
                    if (BasicIMod.DebugMode) LogUtils.DoLog($"Destroy Entity! {e} - Target doesntExists");
#endif
                    cmd.DestroyEntity(unfilteredChunkIndex, e);
                }
                else
                {
                    if (!m_templateUpdaterLkp.TryGetBuffer(e, out var templateUpdatedBuff))
                    {
                        templateUpdatedBuff = m_CommandBuffer.AddBuffer<WETemplateUpdater>(unfilteredChunkIndex, e);
                    }
                    var templateIsValid = m_templateManagerEntries.Contains(templateName);

                    if (templateIsValid)
                    {
                        m_CommandBuffer.AddComponent<WEPlaceholderToBeProcessedInMain>(unfilteredChunkIndex, e, new() { layoutName = templateName });
                    }
                    else
                    {
                        for (int i = 0; i < templateUpdatedBuff.Length; i++)
                        {
                            var templateUpdated = templateUpdatedBuff[i];
                            if (templateUpdated.childEntity != Entity.Null)
                            {
#if !BURST
                                if (BasicIMod.DebugMode) LogUtils.DoLog($"Destroy Entity! {templateUpdated.childEntity} - Target outdated child");
#endif 
                                cmd.DestroyEntity(unfilteredChunkIndex, templateUpdated.childEntity);
                            }
                        }
                        templateUpdatedBuff.Clear();
                    }

                }

                return true;
            }
            private bool UpdateImageMesh(Entity e, ref WETextDataMesh weCustomData, string text, int unfilteredChunkIndex, EntityCommandBuffer.ParallelWriter cmd)
            {
                if (m_templateUpdaterLkp.HasBuffer(e)) cmd.RemoveComponent<WETemplateUpdater>(unfilteredChunkIndex, e);
                IBasicRenderInformation bri = null;
                if (!weCustomData.CustomMeshName.EffectiveValue.IsEmpty)
                {
                    bri = WECustomMeshLibrary.Instance.GetMesh(weCustomData.CustomMeshName.EffectiveValue.ToString(), weCustomData.Atlas.ToString(), text);
                }
                bri ??= WEAtlasesLibrary.Instance.GetFromAvailableAtlases(weCustomData.Atlas.ToString(), text, true);
                if (bri == null)
                {
                    if (BasicIMod.TraceMode) LogUtils.DoTraceLog("IMAGE BRI STILL NULL!!!");
                    return false;
                }
                if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"Image returned: {bri} {text} (a={weCustomData.Atlas}, m={weCustomData.CustomMeshName.EffectiveValue})");
                weCustomData = weCustomData.UpdateBRI(bri, text);
                return true;
            }



            private bool UpdateTextMesh(Entity e, ref WETextDataMesh weCustomData, string text, int unfilteredChunkIndex, EntityCommandBuffer.ParallelWriter cmd, Dictionary<FixedString64Bytes, FontSystemData> fontDict)
            {
                if (m_templateUpdaterLkp.HasBuffer(e)) cmd.RemoveComponent<WETemplateUpdater>(unfilteredChunkIndex, e);
                if (text.Trim() == "")
                {
                    weCustomData = weCustomData.UpdateBRI(cachedEmpty ??= new PrimitiveRenderInformation("", [], [], [], default, null), "");
                    return true;
                }
                var font = fontDict.TryGetValue(weCustomData.FontName, out var fsd) ? fsd : FontServer.Instance.DefaultFont;
                if (font.Font == null)
                {
                    if (BasicIMod.DebugMode) LogUtils.DoLog("Font not initialized!!!");
                    return false;
                }
                var bri = font.FontSystem.DrawText(text);
                if (bri == null || bri == PrimitiveRenderInformation.LOADING_PLACEHOLDER)
                {
                    if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"BRI STILL NULL!!! ({text})");
                    return false;
                }
                weCustomData = weCustomData.UpdateBRI(bri, text);
                return true;

            }
        }
    }
}
