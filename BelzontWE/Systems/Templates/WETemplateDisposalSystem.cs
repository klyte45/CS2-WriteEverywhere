using Game;
using Game.Prefabs;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;

namespace BelzontWE
{
    /// <summary>
    /// System responsible for disposing entities and components marked for removal.
    /// Handles proper cleanup of WETextData components and their associated resources.
    /// </summary>
    public partial class WETemplateDisposalSystem : GameSystemBase
    {
        private EndFrameBarrier m_endFrameBarrier;
        private EntityQuery m_componentsToDispose;
        private EntityQuery m_templatesToDispose;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_endFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();

            // Query for entities that need to be disposed
            // Matches entities with WE components but no WETextComponentValid marker
            // OR entities with WETemplateForPrefab but no PrefabRef
            m_componentsToDispose = GetEntityQuery(new EntityQueryDesc[]
            {
                new()
                {
                    Any = new ComponentType[]
                    {
                        ComponentType.ReadOnly<WETextDataMain>(),
                        ComponentType.ReadOnly<WETextDataMaterial>(),
                        ComponentType.ReadOnly<WETemplateUpdater>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<WETextComponentValid>(),
                    }
                },
            });
            m_templatesToDispose = GetEntityQuery(new EntityQueryDesc[]
           {
                new ()
                {
                    Any = new ComponentType[]
                    {
                        ComponentType.ReadOnly<WETemplateForPrefab>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<PrefabRef>(),
                    }
                }
           });
            RequireAnyForUpdate(m_componentsToDispose, m_templatesToDispose);
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 256;
        }

        protected override void OnUpdate()
        {
            // Check if there are any entities to dispose using chunk count
            // This prevents the assertion error when scheduling parallel jobs on empty queries
            if (m_componentsToDispose.CalculateChunkCount() > 0)
            {
                // Create temporary queues for collecting dispose operations
                var disposeQueueMesh = new NativeQueue<WETextDataMesh>(Allocator.Temp);
                var disposeQueueMaterial = new NativeQueue<WETextDataMaterial>(Allocator.Temp);

                // Schedule the disposal job
                new WEComponentDisposalJob
                {
                    m_EntityType = GetEntityTypeHandle(),
                    m_CommandBuffer = m_endFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                    m_MaterialDataHdl = GetComponentTypeHandle<WETextDataMaterial>(true),
                    m_MeshDataHdl = GetComponentTypeHandle<WETextDataMesh>(true),
                    m_DisposeQueueMesh = disposeQueueMesh.AsParallelWriter(),
                    m_DisposeQueueMaterial = disposeQueueMaterial.AsParallelWriter(),
                }.ScheduleParallel(m_componentsToDispose, Dependency).Complete();

                // Process disposal queues on main thread
                while (disposeQueueMesh.TryDequeue(out var meshData))
                {
                    meshData.Dispose();
                }
                disposeQueueMesh.Dispose();

                while (disposeQueueMaterial.TryDequeue(out var materialData))
                {
                    materialData.Dispose();
                }
                disposeQueueMaterial.Dispose();
            }
            if (m_templatesToDispose.CalculateChunkCount() > 0)
            {

                // Schedule the disposal job
                new WETemplateDisposalJob
                {
                    m_EntityType = GetEntityTypeHandle(),
                    m_CommandBuffer = m_endFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                    m_WETemplateForPrefabLkp = GetComponentLookup<WETemplateForPrefab>(true),
                }.ScheduleParallel(m_templatesToDispose, Dependency).Complete();

            }
        }
        private unsafe struct WEComponentDisposalJob : IJobChunk
        {
            public EntityTypeHandle m_EntityType;
            [ReadOnly] public ComponentTypeHandle<WETextDataMaterial> m_MaterialDataHdl;
            [ReadOnly] public ComponentTypeHandle<WETextDataMesh> m_MeshDataHdl;
            public NativeQueue<WETextDataMesh>.ParallelWriter m_DisposeQueueMesh;
            public NativeQueue<WETextDataMaterial>.ParallelWriter m_DisposeQueueMaterial;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(m_EntityType);
                var materialData = chunk.GetNativeArray(ref m_MaterialDataHdl);
                var meshData = chunk.GetNativeArray(ref m_MeshDataHdl);

                for (int i = 0; i < materialData.Length; i++)
                {
                    var data = materialData[i];
                    m_DisposeQueueMaterial.Enqueue(data);
                }

                for (int i = 0; i < meshData.Length; i++)
                {
                    var data = meshData[i];
                    m_DisposeQueueMesh.Enqueue(data);
                }

                // Queue component removal and entity destruction commands
                for (int i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    m_CommandBuffer.RemoveComponent<WETextDataMesh>(unfilteredChunkIndex, entity);
                    m_CommandBuffer.RemoveComponent<WETextDataMaterial>(unfilteredChunkIndex, entity);
                    m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, entity);
                }
            }
        }
        private unsafe struct WETemplateDisposalJob : IJobChunk
        {
            public EntityTypeHandle m_EntityType;
            [ReadOnly] public ComponentLookup<WETemplateForPrefab> m_WETemplateForPrefabLkp;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(m_EntityType);

                // Queue component removal and entity destruction commands
                for (int i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];

                    m_CommandBuffer.RemoveComponent<WETextDataMesh>(unfilteredChunkIndex, entity);
                    m_CommandBuffer.RemoveComponent<WETextDataMaterial>(unfilteredChunkIndex, entity);

                    if (m_WETemplateForPrefabLkp.TryGetComponent(entity, out var prefabData))
                    {
                        m_CommandBuffer.RemoveComponent<WETemplateForPrefab>(unfilteredChunkIndex, entity);
                        m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, prefabData.childEntity);
                    }

                    m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, entity);
                }
            }
        }
    }
}
