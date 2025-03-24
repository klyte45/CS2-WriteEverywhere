using Unity.Burst.Intrinsics;
using Unity.Entities;


#if BURST
#endif

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
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
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
                    if (m_TransformDataLkp.TryGetComponent(entity, out var data2))
                    {
                        data2.Dispose();
                        m_CommandBuffer.RemoveComponent<WETextDataMaterial>(unfilteredChunkIndex, entity);
                    }
                    if (m_MeshDataLkp.TryGetComponent(entity, out var data3))
                    {
                        data3.Dispose();
                        m_CommandBuffer.RemoveComponent<WETextDataMaterial>(unfilteredChunkIndex, entity);
                    }
                    m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, entity);
                }
            }
        }

    }
}