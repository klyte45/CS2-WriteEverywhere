using Game.Prefabs;
using Unity.Burst.Intrinsics;
using Unity.Entities;
using Unity.Collections;
using System;


#if BURST
using Unity.Burst;
#endif

namespace BelzontWE
{
    public partial class WETemplateManager
    {
#if BURST
        [BurstCompile]
#endif
        private unsafe struct WEPrefabTemplateFilterJob : IJobChunk
        {
            public EntityTypeHandle m_EntityType;
            public ComponentTypeHandle<PrefabRef> m_prefabRefHdl;
            public ComponentLookup<PrefabData> m_prefabDataLkp;
            public ComponentLookup<WETemplateForPrefabEmpty> m_prefabEmptyLkp;
            public ComponentLookup<WETemplateForPrefabDirty> m_prefabDirtyLkp;
            public ComponentLookup<WETemplateForPrefab> m_prefabLayoutLkp;
            public ComponentLookup<WETemplateUpdater> m_templateUpdaterLkp;
            public ComponentLookup<WETextDataMain> m_TextDataLkp;
            public BufferLookup<WESubTextRef> m_subRefLkp;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
            public NativeHashMap<long, Colossal.Hash128> m_indexesWithLayout;
            public unsafe void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(m_EntityType);
                var prefabRefs = chunk.GetNativeArray(ref m_prefabRefHdl);
                var checkCount = Math.Min(2, entities.Length);
                for (int i = 0; i < checkCount; i++)
                {
                    var entity = entities[i];
                    var prefabRef = prefabRefs[i];
                    if (m_prefabDataLkp.TryGetComponent(prefabRef.m_Prefab, out var prefabData))
                    {
                        bool needUpdate = !m_prefabLayoutLkp.HasComponent(entity);
                        if (m_prefabLayoutLkp.TryGetComponent(entity, out var layoutData) && (m_indexesWithLayout.TryGetValue(prefabData.m_Index, out var newTemplate) ? layoutData.templateRef != newTemplate : layoutData.templateRef != default))
                        {
                            if (layoutData.childEntity != Entity.Null) DestroyRecursive(layoutData.childEntity, unfilteredChunkIndex);
                            m_CommandBuffer.RemoveComponent<WETemplateForPrefab>(unfilteredChunkIndex, entities[i]);
                            needUpdate = true;
                        }
                        if (needUpdate)
                        {

                            if (m_indexesWithLayout.TryGetValue(prefabData.m_Index, out newTemplate))
                            {
                                m_CommandBuffer.AddComponent<WETemplateForPrefabToRunOnMain>(unfilteredChunkIndex, entity);
                            }
                            else
                            {
                                m_CommandBuffer.AddComponent<WETemplateForPrefab>(unfilteredChunkIndex, entities[i], new()
                                {
                                    templateRef = default,
                                    childEntity = Entity.Null
                                });
                                if (!m_prefabEmptyLkp.HasComponent(entity)) m_CommandBuffer.AddComponent<WETemplateForPrefabEmpty>(unfilteredChunkIndex, entity);
                            }

                        }
                    }
                    else if (!m_prefabEmptyLkp.HasComponent(entity))
                    {
                        m_CommandBuffer.AddComponent<WETemplateForPrefabEmpty>(unfilteredChunkIndex, entity);
                    }
                    if (m_prefabDirtyLkp.HasComponent(entity))
                    {
                        m_CommandBuffer.RemoveComponent<WETemplateForPrefabDirty>(unfilteredChunkIndex, entity);
                    }
                }
            }
            private void DestroyRecursive(Entity nextEntity, int unfilteredChunkIndex, Entity initialDelete = default, int iterationCounter = 0)
            {
                if (iterationCounter > 64) return;
                if (nextEntity == Entity.Null)
                {
                    return;
                }
                if (nextEntity != initialDelete)
                {
                    if (initialDelete == default) initialDelete = nextEntity;
                    if (m_prefabLayoutLkp.TryGetComponent(nextEntity, out var data))
                    {
                        DestroyRecursive(data.childEntity, unfilteredChunkIndex, initialDelete, iterationCounter + 1);
                    }
                    if (m_templateUpdaterLkp.TryGetComponent(nextEntity, out var updater))
                    {
                        DestroyRecursive(updater.childEntity, unfilteredChunkIndex, initialDelete, iterationCounter + 1);
                    }
                    if (m_subRefLkp.TryGetBuffer(nextEntity, out var subLayout))
                    {
                        for (int j = 0; j < subLayout.Length; j++)
                        {
                            DestroyRecursive(subLayout[j].m_weTextData, unfilteredChunkIndex, initialDelete, iterationCounter + 1);
                        }
                    }
                    m_CommandBuffer.AddComponent<Game.Common.Deleted>(unfilteredChunkIndex, nextEntity);
                }
            }
        }

    }
}