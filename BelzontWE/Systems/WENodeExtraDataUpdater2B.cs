using Game.Common;
using Game.Net;
using Unity.Burst.Intrinsics;
using Unity.Entities;
using Unity.Jobs;
using Game;
using Game.Tools;
using static BelzontWE.WENodeExtraDataUpdater;

#if BURST
using Unity.Burst;
#endif
namespace BelzontWE
{
    public partial class WENodeExtraDataUpdater2B : GameSystemBase
    {
        private EntityQuery m_ModifiedQuery;
        private ModificationBarrier2B m_ModifiedBarrier2B;


        protected override void OnCreate()
        {
            base.OnCreate();
            this.m_ModifiedQuery = base.GetEntityQuery(new EntityQueryDesc[]
           {
                new() {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Edge>(),
                    },
                    Any = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Created>(),
                        ComponentType.ReadOnly<Updated>(),
                        ComponentType.ReadOnly<Deleted>()
                    },
                    None =  new ComponentType[]
                    {
                        ComponentType.ReadOnly<Temp>()
                    },
                }
           });

            RequireForUpdate(m_ModifiedQuery);
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            m_ModifiedBarrier2B = World.GetExistingSystemManaged<ModificationBarrier2B>();
        }

        protected override void OnUpdate()
        {
            if (!m_ModifiedQuery.IsEmpty)
            {
                new NodeCacheEraserFromDeletedAggregated
                {
                    m_CommandBuffer = m_ModifiedBarrier2B.CreateCommandBuffer().AsParallelWriter(),
                    m_EntityTypeHandle = GetEntityTypeHandle(),
                    m_aggregateElementLkp = GetBufferLookup<AggregateElement>(true),
                    m_EdgeLookup = GetComponentLookup<Edge>(true),
                    m_DeletedLookup = GetComponentLookup<Deleted>(true),
                    m_aggregatedLkp = GetComponentLookup<Aggregated>(true)
                }.ScheduleParallel(m_ModifiedQuery, Dependency).Complete();
            }
        }

#if BURST
        [Unity.Burst.BurstCompile]
#endif
        private struct NodeCacheEraserFromDeletedAggregated : IJobChunk
        {
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
            public EntityTypeHandle m_EntityTypeHandle;
            public BufferLookup<AggregateElement> m_aggregateElementLkp;
            public ComponentLookup<Aggregated> m_aggregatedLkp;
            public ComponentLookup<Edge> m_EdgeLookup;
            public ComponentLookup<Deleted> m_DeletedLookup;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {

                var entities = chunk.GetNativeArray(m_EntityTypeHandle);
                for (int i = 0; i < entities.Length; i++)
                {
                    var initEdge = entities[i];

                    if (m_aggregatedLkp.TryGetComponent(initEdge, out var agg) && m_aggregateElementLkp.TryGetBuffer(agg.m_Aggregate, out var elements))
                    {
                        for (int j = 0; j < elements.Length; j++)
                        {
                            var element = elements[j];
                            if (initEdge == element.m_Edge) continue;
                            var edge = m_EdgeLookup[element.m_Edge];
                            if (edge.m_End != Entity.Null && !m_DeletedLookup.HasComponent(edge.m_End))
                            {
                                m_CommandBuffer.RemoveComponent<WENetNodeInformation>(unfilteredChunkIndex, edge.m_End);

                            }
                            if (edge.m_Start != Entity.Null && !m_DeletedLookup.HasComponent(edge.m_Start))
                            {
                                m_CommandBuffer.RemoveComponent<WENetNodeInformation>(unfilteredChunkIndex, edge.m_End);
                            }
                        }

                    }
                }
            }
        }
    }
}
