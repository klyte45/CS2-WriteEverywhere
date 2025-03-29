using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Entities;

namespace BelzontWE
{
    public partial class WETemplateManager
    {
        private unsafe struct WETemplateDisposalJob : IJobChunk
        {
            public EntityTypeHandle m_EntityType;
            public ComponentLookup<WETextDataMaterial> m_MaterialDataLkp;
            public ComponentLookup<WETextDataTransform> m_TransformDataLkp;
            public ComponentLookup<WETextDataMesh> m_MeshDataLkp;
            public BufferLookup<WETemplateUpdater> m_UpdaterDataLkp;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
            internal ComponentLookup<WETemplateForPrefab> m_WETemplateForPrefabLkp;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(m_EntityType);
                for (int i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    if (m_MaterialDataLkp.TryGetComponent(entity, out var data1))
                    {
                        data1.Dispose();
                        m_CommandBuffer.RemoveComponent<WETextDataMaterial>(unfilteredChunkIndex, entity);
                    }
                    if (m_MeshDataLkp.TryGetComponent(entity, out var data3))
                    {
                        data3.Dispose();
                        m_CommandBuffer.RemoveComponent<WETextDataMesh>(unfilteredChunkIndex, entity);
                    }
                    if (m_WETemplateForPrefabLkp.TryGetComponent(entity, out var data2))
                    {
                        m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, data2.childEntity);
                        m_CommandBuffer.RemoveComponent<WETemplateForPrefab>(unfilteredChunkIndex, entity);
                    }
                    if (m_UpdaterDataLkp.TryGetBuffer(entity, out var buff))
                    {
                        for(int j = 0; j < buff.Length; j++)
                        {
                            m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, buff[j].childEntity) ;
                        }
                        m_CommandBuffer.RemoveComponent<WETemplateUpdater>(unfilteredChunkIndex, entity);
                    }
                    m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, entity);
                }
            }
        }

    }
}