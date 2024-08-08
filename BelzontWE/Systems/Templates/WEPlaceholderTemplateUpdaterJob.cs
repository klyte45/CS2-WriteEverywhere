using Unity.Burst.Intrinsics;
using Unity.Collections;
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
        private unsafe struct WEPlaceholderTemplateUpdaterJob : IJobChunk
        {
            public EntityTypeHandle m_EntityType;
            public ComponentTypeHandle<WETemplateUpdater> m_prefabUpdaterHdl;
            public NativeHashSet<Entity> m_obsoleteTemplateList;
            public EntityCommandBuffer m_CommandBuffer;
            public unsafe void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(m_EntityType);
                var updaters = chunk.GetNativeArray(ref m_prefabUpdaterHdl);

                for (int i = 0; i < updaters.Length; i++)
                {
                    if (m_obsoleteTemplateList.Contains(updaters[i].templateEntity))
                    {
                        m_CommandBuffer.AddComponent<WEWaitingRenderingPlaceholder>(entities[i]);
                    }
                }
            }
        }

    }
}