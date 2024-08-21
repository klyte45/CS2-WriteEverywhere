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
            public UnsafeParallelHashMap<long, WETextDataTreeStruct> m_indexesWithLayout;
            public unsafe void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(m_EntityType);
                var prefabRefs = chunk.GetNativeArray(ref m_prefabRefHdl);
                //    UnityEngine.Debug.Log($"WEPrefabTemplateFilterJob SIZE: {entities.Length}");
                for (int i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    var prefabRef = prefabRefs[i];
                    if (m_prefabDataLkp.TryGetComponent(prefabRef.m_Prefab, out var prefabData))
                    {
                        if (!m_prefabLayoutLkp.TryGetComponent(entity, out var layoutData) || (m_indexesWithLayout.TryGetValue(prefabData.m_Index, out var newTemplate) ? layoutData.templateRef != newTemplate.Guid : layoutData.templateRef != default))
                        {
                            if (m_prefabLayoutLkp.HasComponent(entity))
                            {
                                DestroyRecursive(layoutData.childEntity, unfilteredChunkIndex);
                                m_CommandBuffer.RemoveComponent<WETemplateForPrefab>(unfilteredChunkIndex, entities[i]);
                            }
                            if (m_indexesWithLayout.TryGetValue(prefabData.m_Index, out newTemplate))
                            {
                                var childEntity = WELayoutUtility.DoCreateLayoutItem(newTemplate, entities[i], Entity.Null, ref m_TextDataLkp, ref m_subRefLkp, unfilteredChunkIndex, m_CommandBuffer, WELayoutUtility.ParentEntityMode.TARGET_IS_PARENT, true);
                                if (m_prefabEmptyLkp.HasComponent(entity)) m_CommandBuffer.RemoveComponent<WETemplateForPrefabEmpty>(unfilteredChunkIndex, entity);
                                m_CommandBuffer.AddComponent<WETemplateForPrefab>(unfilteredChunkIndex, entities[i], new()
                                {
                                    templateRef = newTemplate.Guid,
                                    childEntity = childEntity
                                });
                            }
                            else
                            {
                                m_CommandBuffer.AddComponent<WETemplateForPrefab>(unfilteredChunkIndex, entities[i], new()
                                {
                                    templateRef = default,
                                    childEntity = Entity.Null
                                });
                                m_CommandBuffer.AddComponent<WETemplateForPrefabEmpty>(unfilteredChunkIndex, entity);
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
                if (iterationCounter > 256) return;
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
                }
                m_CommandBuffer.AddComponent<Game.Common.Deleted>(unfilteredChunkIndex, nextEntity);
            }
        }

    }
}