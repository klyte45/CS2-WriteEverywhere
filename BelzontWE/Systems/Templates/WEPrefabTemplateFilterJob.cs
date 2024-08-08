using Game.Prefabs;
using Unity.Burst.Intrinsics;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;


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
            public ComponentLookup<WETextData> m_TextDataLkp;
            public BufferLookup<WESubTextRef> m_subRefLkp;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
            public UnsafeParallelHashMap<long, Entity> m_indexesWithLayout;
            public unsafe void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(m_EntityType);
                var prefabRefs = chunk.GetNativeArray(ref m_prefabRefHdl);
                //    UnityEngine.Debug.Log($"WEPrefabTemplateFilterJob SIZE: {entities.Length}");
                for (int i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    var prefabRef = prefabRefs[i];
                    if (m_prefabDataLkp.TryGetComponent(prefabRef.m_Prefab, out var prefabData) && m_indexesWithLayout.TryGetValue(prefabData.m_Index, out Entity newTemplate))
                    {
                        if (!m_prefabLayoutLkp.TryGetComponent(newTemplate, out var layoutData) || layoutData.templateRef != newTemplate)
                        {
                            if (m_prefabLayoutLkp.TryGetComponent(newTemplate, out var oldComponent))
                            {
                                DestroyRecursive(oldComponent.childEntity, unfilteredChunkIndex);
                                m_CommandBuffer.RemoveComponent<WETemplateForPrefab>(unfilteredChunkIndex, entities[i]);
                            }
                            var childEntity = WELayoutUtility.DoCloneTextItemReferenceSelf(newTemplate, entities[i], ref m_TextDataLkp, ref m_subRefLkp, unfilteredChunkIndex, m_CommandBuffer, true);
                            m_CommandBuffer.AddComponent<WETemplateForPrefab>(unfilteredChunkIndex, entities[i], new()
                            {
                                templateRef = newTemplate,
                                childEntity = childEntity
                            });
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
            private void DestroyRecursive(Entity nextEntity, int unfilteredChunkIndex, Entity initialDelete = default)
            {
                if (nextEntity != initialDelete)
                {
                    if (initialDelete == default) initialDelete = nextEntity;
                    if (m_prefabLayoutLkp.TryGetComponent(nextEntity, out var data))
                    {
                        DestroyRecursive(data.childEntity, unfilteredChunkIndex, initialDelete);
                    }
                    if (m_templateUpdaterLkp.TryGetComponent(nextEntity, out var updater))
                    {
                        DestroyRecursive(updater.childEntity, unfilteredChunkIndex, initialDelete);
                    }
                    if (m_subRefLkp.TryGetBuffer(nextEntity, out var subLayout))
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