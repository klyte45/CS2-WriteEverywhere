

using Unity.Collections;
using Unity.Entities;

namespace BelzontWE.Utils
{

#if BURST
        [Unity.Burst.BurstCompile]
#endif
    public struct WEAddAndEnableComponentJob<T> : IJobChunk where T : unmanaged, IComponentData, IEnableableComponent
    {
        [ReadOnly]
        public EntityTypeHandle m_EntityType;
        public ComponentLookup<T> m_componentLkp;

        public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in Unity.Burst.Intrinsics.v128 chunkEnabledMask)
        {
            var entities = chunk.GetNativeArray(m_EntityType);

            for (int i = 0; i < entities.Length; i++)
            {
                if (m_componentLkp.HasComponent(entities[i]))
                {
                    m_CommandBuffer.SetComponentEnabled<T>(unfilteredChunkIndex, entities[i], true);
                }
                else
                {
                    m_CommandBuffer.AddComponent<T>(unfilteredChunkIndex, entities[i]);
                }
            }
        }
    }
}