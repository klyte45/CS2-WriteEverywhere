using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;

namespace BelzontWE
{
    public partial class WETemplateManager
    {
        private unsafe struct WETemplateDisposalJob : IJobChunk
        {
            public EntityTypeHandle m_EntityType;
            [ReadOnly] public ComponentTypeHandle<WETextDataMaterial> m_MaterialDataHdl;
            [ReadOnly] public ComponentTypeHandle<WETextDataMesh> m_MeshDataHdl;
            [ReadOnly] public ComponentLookup<WETemplateForPrefab> m_WETemplateForPrefabLkp;
            [ReadOnly] public BufferLookup<WETemplateUpdater> m_UpdaterDataLkp;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(m_EntityType);

                // Dispose managed resources on main thread-accessible components
                if (chunk.Has(ref m_MaterialDataHdl))
                {
                    var materialData = chunk.GetNativeArray(ref m_MaterialDataHdl);
                    for (int i = 0; i < materialData.Length; i++)
                    {
                        var data = materialData[i];
                        data.Dispose();
                    }
                }

                if (chunk.Has(ref m_MeshDataHdl))
                {
                    var meshData = chunk.GetNativeArray(ref m_MeshDataHdl);
                    for (int i = 0; i < meshData.Length; i++)
                    {
                        var data = meshData[i];
                        data.Dispose();
                    }
                }

                // Queue component removal and entity destruction commands
                for (int i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];


                    m_CommandBuffer.RemoveComponent<WETextDataMesh>(unfilteredChunkIndex, entity);
                    m_CommandBuffer.RemoveComponent<WETextDataMaterial>(unfilteredChunkIndex, entity);

                    if (m_WETemplateForPrefabLkp.TryGetComponent(entity, out var prefabData))
                    {
                        m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, prefabData.childEntity);
                    }

                    if (m_UpdaterDataLkp.TryGetBuffer(entity, out var buff))
                    {
                        for (int j = 0; j < buff.Length; j++)
                        {
                            m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, buff[j].childEntity);
                        }
                    }

                    m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, entity);
                }
            }
        }

    }
}