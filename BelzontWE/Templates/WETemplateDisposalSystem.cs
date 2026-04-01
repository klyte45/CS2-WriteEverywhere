using Game;
using Game.Prefabs;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

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
        private EntityQuery m_templateUpdaterToDispose;
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
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<WETextComponentValid>(),
                    }
                },
            });
            m_templateUpdaterToDispose = GetEntityQuery(new EntityQueryDesc[]
           {
                new ()
                {
                    Any = new ComponentType[]
                    {
                        ComponentType.ReadOnly<WETemplateUpdater>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<WETextComponentValid>(),
                    }
                }
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
            if (!m_componentsToDispose.IsEmpty)
            {
                // Create temporary queues for collecting dispose operations
                var disposeQueueMesh = new NativeQueue<WETextDataMesh>(Allocator.Persistent);
                var disposeQueueMaterial = new NativeQueue<WETextDataMaterial>(Allocator.Persistent);

                using var entitiesToDispose = m_componentsToDispose.ToEntityArray(Allocator.TempJob);
                // Schedule the disposal job
                if (entitiesToDispose.Length > 0)
                {
                    new WEComponentDisposalJob
                    {
                        entities = entitiesToDispose,
                        m_CommandBuffer = m_endFrameBarrier.CreateCommandBuffer(),
                        m_MaterialDataLkp = GetComponentLookup<WETextDataMaterial>(true),
                        m_MeshDataLkp = GetComponentLookup<WETextDataMesh>(true),
                        m_DisposeQueueMesh = disposeQueueMesh,
                        m_DisposeQueueMaterial = disposeQueueMaterial,
                    }.Schedule(entitiesToDispose.Length, Dependency).Complete();

                    if (!disposeQueueMesh.IsEmpty())
                    {
                        // Process disposal queues on main thread
                        while (disposeQueueMesh.TryDequeue(out var meshData))
                        {
                            meshData.Dispose();
                        }
                    }
                    disposeQueueMesh.Dispose();
                    if (!disposeQueueMaterial.IsEmpty())
                    {
                        while (disposeQueueMaterial.TryDequeue(out var materialData))
                        {
                            materialData.Dispose();
                        }
                    }
                    disposeQueueMaterial.Dispose();
                }
            }
            if (m_templatesToDispose.CalculateChunkCount() > 0)
            {

                // Schedule the disposal job
                new WETemplateDisposalJob
                {
                    m_EntityType = GetEntityTypeHandle(),
                    m_CommandBuffer = m_endFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                    m_WETemplateForPrefabLkp = GetComponentTypeHandle<WETemplateForPrefab>(true),
                }.ScheduleParallel(m_templatesToDispose, Dependency).Complete();

            }
            if (m_templateUpdaterToDispose.CalculateChunkCount() > 0)
            {

                // Schedule the disposal job
                new WETemplateUpdaterDisposalJob
                {
                    m_EntityType = GetEntityTypeHandle(),
                    m_CommandBuffer = m_endFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                    m_WETemplateForPrefabHnd = GetBufferTypeHandle<WETemplateUpdater>(true),
                }.ScheduleParallel(m_templateUpdaterToDispose, Dependency).Complete();

            }
        }
        private unsafe struct WEComponentDisposalJob : IJobFor
        {
            [ReadOnly] public ComponentLookup<WETextDataMaterial> m_MaterialDataLkp;
            [ReadOnly] public ComponentLookup<WETextDataMesh> m_MeshDataLkp;
            public NativeQueue<WETextDataMesh> m_DisposeQueueMesh;
            public NativeQueue<WETextDataMaterial> m_DisposeQueueMaterial;
            public EntityCommandBuffer m_CommandBuffer;
            public NativeArray<Entity> entities;

            public void Execute(int i)
            {
                if (i < entities.Length)
                {
                    var data = entities[i];
                    if (m_MaterialDataLkp.TryGetComponent(data, out var materialData))
                        m_DisposeQueueMaterial.Enqueue(materialData);
                    if (m_MeshDataLkp.TryGetComponent(data, out var meshData))
                        m_DisposeQueueMesh.Enqueue(meshData);
                    m_CommandBuffer.RemoveComponent<WETextDataMesh>(data);
                    m_CommandBuffer.RemoveComponent<WETextDataMaterial>(data);
                    m_CommandBuffer.DestroyEntity(data);
                }
            }
        }

        private unsafe struct WETemplateDisposalJob : IJobChunk
        {
            public EntityTypeHandle m_EntityType;
            [ReadOnly] public ComponentTypeHandle<WETemplateForPrefab> m_WETemplateForPrefabLkp;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(m_EntityType);
                var templateDatas = chunk.GetNativeArray(ref m_WETemplateForPrefabLkp);

                // Queue component removal and entity destruction commands
                for (int i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    var templateData = templateDatas[i];

                    m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, templateData.childEntity);
                    m_CommandBuffer.RemoveComponent<WETemplateForPrefab>(unfilteredChunkIndex, entity);
                    m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, entity);
                }
            }
        }
        private unsafe struct WETemplateUpdaterDisposalJob : IJobChunk
        {
            public EntityTypeHandle m_EntityType;
            [ReadOnly] public BufferTypeHandle<WETemplateUpdater> m_WETemplateForPrefabHnd;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(m_EntityType);
                var templateUpdaters = chunk.GetBufferAccessor(ref m_WETemplateForPrefabHnd);
                for (int i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    var updaterBuffer = templateUpdaters[i];

                    for (var j = 0; j < updaterBuffer.Length; j++)
                    {
                        var updater = updaterBuffer[j];
                        m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, updater.childEntity);
                    }

                    var buffer = m_CommandBuffer.SetBuffer<WETemplateUpdater>(unfilteredChunkIndex, entity);
                    buffer.Clear();
                    m_CommandBuffer.RemoveComponent<WETemplateUpdater>(unfilteredChunkIndex, entity);
                    m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, entity);
                }
            }
        }
    }
}
