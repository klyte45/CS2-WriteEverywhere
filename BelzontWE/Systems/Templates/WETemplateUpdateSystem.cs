using Game;
using Game.Common;
using Game.Prefabs;
using Game.Rendering;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace BelzontWE
{
    /// <summary>
    /// Handles entity updates, template application, and formula recalculation.
    /// This system processes entities that need template updates and schedules
    /// parallel jobs for efficient entity processing.
    /// </summary>
    [UpdateAfter(typeof(WETemplateManager))]
    public partial class WETemplateUpdateSystem : SystemBase
    {
        private WETemplateManager m_manager;
        private PrefabSystem m_prefabSystem;
        private EndFrameBarrier m_endFrameBarrier;

        // Entity queries moved from WETemplateManager
        private EntityQuery m_templateBasedEntities;
        private EntityQuery m_entitiesWaitingRendering;
        private EntityQuery m_uncheckedWePrefabLayoutQuery;
        private EntityQuery m_dirtyWePrefabLayoutQuery;
        private EntityQuery m_dirtyInstancingWeQuery;
        private EntityQuery m_entitiesToBeUpdatedInMain;
        private EntityQuery m_prefabArchetypesToBeUpdatedInMain;
        private EntityQuery m_textDataDirtyQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_manager = World.GetExistingSystemManaged<WETemplateManager>();
            m_prefabSystem = World.GetExistingSystemManaged<PrefabSystem>();
            m_endFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();

            // Initialize entity queries
            m_templateBasedEntities = GetEntityQuery(new EntityQueryDesc[]
            {
                new ()
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<WEIsPlaceholder>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<WEWaitingRendering>(),
                        ComponentType.ReadOnly<Deleted>(),
                    }
                }
            });
            m_entitiesWaitingRendering = GetEntityQuery(new EntityQueryDesc[]
            {
                new ()
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<WEWaitingRendering>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                    }
                }
            });

            m_uncheckedWePrefabLayoutQuery = GetEntityQuery(new EntityQueryDesc[]
            {
                new ()
                {
                    All = new[]
                    {
                        ComponentType.ReadOnly<PrefabRef>()
                    },
                    Any = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Game.Objects.Transform>(),
                        ComponentType.ReadOnly<InterpolatedTransform>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<WETemplateForPrefabEmpty>(),
                        ComponentType.ReadOnly<WETemplateForPrefabDirty>(),
                        ComponentType.ReadOnly<WETemplateForPrefab>(),
                        ComponentType.ReadOnly<Deleted>(),
                    }
                }
            });

            m_dirtyWePrefabLayoutQuery = GetEntityQuery(new EntityQueryDesc[]
            {
                new()
                {
                    Any = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Game.Objects.Transform>(),
                        ComponentType.ReadOnly<InterpolatedTransform>(),
                    },
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<PrefabRef>(),
                        ComponentType.ReadOnly<WETemplateForPrefabDirty>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                    }
                }
            });

            m_dirtyInstancingWeQuery = GetEntityQuery(new EntityQueryDesc[]
            {
                new()
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<WEIsPlaceholder>(),
                        ComponentType.ReadOnly<WETemplateDirtyInstancing>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<WEWaitingRendering>(),
                    }
                }
            });

            m_entitiesToBeUpdatedInMain = GetEntityQuery(new EntityQueryDesc[]
            {
                new ()
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<WETextComponentValid>(),
                        ComponentType.ReadOnly<WETextDataMain>(),
                        ComponentType.ReadOnly<WETextDataMaterial>(),
                        ComponentType.ReadOnly<WETextDataMesh>(),
                        ComponentType.ReadOnly<WETextDataTransform>(),
                        ComponentType.ReadOnly<WEPlaceholderToBeProcessedInMain>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                    }
                }
            });

            m_prefabArchetypesToBeUpdatedInMain = GetEntityQuery(new EntityQueryDesc[]
            {
                new ()
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<PrefabRef>(),
                        ComponentType.ReadOnly<WETemplateForPrefabToRunOnMain>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<WETemplateForPrefab>(),
                    }
                }
            });

            m_textDataDirtyQuery = GetEntityQuery(new EntityQueryDesc[]
            {
                new ()
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadWrite<WETextDataMain>(),
                        ComponentType.ReadWrite<WETextDataMaterial>(),
                        ComponentType.ReadWrite<WETextDataTransform>(),
                        ComponentType.ReadWrite<WETextDataMesh>(),
                        ComponentType.ReadOnly<WETextDataDirtyFormulae>(),
                        ComponentType.ReadOnly<WETextComponentValid>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<WEWaitingRendering>(),
                        ComponentType.ReadOnly<Deleted>(),
                    }
                }
            });

        }

#if BURST
        [Unity.Burst.BurstCompile]
#endif
        private struct WEReadFirstEntities : IJobChunk
        {
            public NativeArray<Entity> output;
            public EntityTypeHandle m_EntityType;
            public int entitiesEachChunk;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(m_EntityType);
                var baseIndex = entitiesEachChunk * unfilteredChunkIndex;
                for (int i = 0; i < entitiesEachChunk && i < entities.Length; i++)
                {
                    output[baseIndex + i] = entities[i];
                }
            }
        }

        protected override void OnUpdate()
        {
            if (m_manager.IsGameLoadingOrInitializing || m_manager.IsLoadingLayouts || !WriteEverywhereCS2Mod.IsInitializationComplete) return;

            // Process entity updates in priority order
            if (m_manager.UpdatingEntitiesOnMain == null)
            {

                if (m_manager.TemplatesDirty && m_entitiesWaitingRendering.IsEmpty)
                {
                    EntityManager.AddComponent<WEWaitingRendering>(m_templateBasedEntities);
                    EntityManager.SetComponentEnabled<WEWaitingRendering>(m_templateBasedEntities, true);
                    m_manager.ClearTemplatesDirty();
                    return;
                }

                if (!m_dirtyWePrefabLayoutQuery.IsEmpty)
                {
                    var keysWithTemplate = new NativeHashMap<long, Colossal.Hash128>(0, Allocator.TempJob);
                    foreach (var i in m_manager.GetPrefabTemplatesReadOnly())
                    {
                        keysWithTemplate[i.Key] = i.Value.Guid;
                    }
                    Dependency = new WEPrefabTemplateDirtyJob
                    {
                        m_EntityType = GetEntityTypeHandle(),
                        m_prefabEmptyLkp = GetComponentLookup<WETemplateForPrefabEmpty>(true),
                        m_prefabDirtyLkp = GetComponentLookup<WETemplateForPrefabDirty>(true),
                        m_prefabLayoutLkp = GetComponentLookup<WETemplateForPrefab>(true),
                        m_subRefLkp = GetBufferLookup<WESubTextRef>(true),
                        m_CommandBuffer = m_endFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                        m_indexesWithLayout = keysWithTemplate,
                        m_templateUpdaterLkp = GetBufferLookup<WETemplateUpdater>(true),
                    }.ScheduleParallel(m_dirtyWePrefabLayoutQuery, Dependency);
                    keysWithTemplate.Dispose(Dependency);
                    return;
                }

                if (!m_prefabArchetypesToBeUpdatedInMain.IsEmpty)
                {
                    NativeArray<Entity> outputArray;
                    if (m_prefabArchetypesToBeUpdatedInMain.CalculateEntityCount() > 10_000)
                    {
                        var chunkCount = m_prefabArchetypesToBeUpdatedInMain.CalculateChunkCountWithoutFiltering();
                        var sizeEachIterationPerChunk = 10_000 / chunkCount;
                        outputArray = new NativeArray<Entity>(sizeEachIterationPerChunk * chunkCount, Allocator.Temp);
                        new WEReadFirstEntities
                        {
                            entitiesEachChunk = sizeEachIterationPerChunk,
                            m_EntityType = GetEntityTypeHandle(),
                            output = outputArray
                        }.ScheduleParallel(m_prefabArchetypesToBeUpdatedInMain, Dependency).Complete();
                    }
                    else
                    {
                        outputArray = m_prefabArchetypesToBeUpdatedInMain.ToEntityArray(Allocator.Temp);
                    }
                    m_manager.UpdatePrefabArchetypes(outputArray);
                    outputArray.Dispose();
                    return;
                }
                if (!m_entitiesToBeUpdatedInMain.IsEmpty)
                {
                    NativeArray<Entity> outputArray;
                    if (m_entitiesToBeUpdatedInMain.CalculateEntityCount() > 10_000)
                    {
                        var chunkCount = m_entitiesToBeUpdatedInMain.CalculateChunkCountWithoutFiltering();
                        var sizeEachIterationPerChunk = 10_000 / chunkCount;
                        outputArray = new NativeArray<Entity>(sizeEachIterationPerChunk * chunkCount, Allocator.Temp);
                        new WEReadFirstEntities
                        {
                            entitiesEachChunk = sizeEachIterationPerChunk,
                            m_EntityType = GetEntityTypeHandle(),
                            output = outputArray
                        }.ScheduleParallel(m_entitiesToBeUpdatedInMain, Dependency).Complete();
                    }
                    else
                    {
                        outputArray = m_entitiesToBeUpdatedInMain.ToEntityArray(Allocator.Temp);
                    }

                    m_manager.UpdateLayouts(outputArray);
                    outputArray.Dispose();
                    return;
                }
                if (!m_uncheckedWePrefabLayoutQuery.IsEmpty)
                {
                    var keysWithTemplate = new NativeHashMap<long, Colossal.Hash128>(0, Allocator.TempJob);
                    foreach (var i in m_manager.GetPrefabTemplatesReadOnly())
                    {
                        keysWithTemplate[i.Key] = i.Value.Guid;
                    }
                    Dependency = new WEPrefabTemplateFilterJob
                    {
                        m_tempLkp = GetComponentLookup<Temp>(true),
                        m_EntityType = GetEntityTypeHandle(),
                        m_prefabRefHdl = GetComponentTypeHandle<PrefabRef>(true),
                        m_prefabDataLkp = GetComponentLookup<PrefabData>(true),
                        m_CommandBuffer = m_endFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                        m_indexesWithLayout = keysWithTemplate,
                    }.ScheduleParallel(m_uncheckedWePrefabLayoutQuery, Dependency);
                    keysWithTemplate.Dispose(Dependency);
                }

                if (!m_dirtyInstancingWeQuery.IsEmpty)
                {
                    var chunks = m_dirtyInstancingWeQuery.ToArchetypeChunkArray(Allocator.Persistent);
                    EntityCommandBuffer cmd = m_endFrameBarrier.CreateCommandBuffer();
                    for (int h = 0; h < chunks.Length; h++)
                    {
                        var chunk = chunks[h];
                        var entities = chunk.GetNativeArray(GetEntityTypeHandle());
                        for (int i = 0; i < entities.Length; i++)
                        {
                            var e = entities[i];
                            var buff = cmd.SetBuffer<WETemplateUpdater>(e);
                            for (int j = 0; j < buff.Length; j++)
                            {
                                cmd.DestroyEntity(buff[j].childEntity);
                            }
                            buff.Clear();
                            cmd.SetComponentEnabled<WETemplateDirtyInstancing>(e, false);
                            cmd.AddComponent<WEWaitingRendering>(e);
                            cmd.SetComponentEnabled<WEWaitingRendering>(e, true);
                        }
                    }
                    chunks.Dispose();
                }
                if (!m_textDataDirtyQuery.IsEmpty)
                {
                    using var tempArr = m_textDataDirtyQuery.ToArchetypeChunkArray(Allocator.Temp);
                    var job = new WEUpdateFormulaesJob
                    {
                        m_MainDataHdl = GetComponentTypeHandle<WETextDataMain>(false),
                        m_MaterialDataHdl = GetComponentTypeHandle<WETextDataMaterial>(false),
                        m_TransformDataHdl = GetComponentTypeHandle<WETextDataTransform>(false),
                        m_MeshDataHdl = GetComponentTypeHandle<WETextDataMesh>(false),
                        m_DirtyFormulaeHdl = GetComponentTypeHandle<WETextDataDirtyFormulae>(true),
                        m_EntityType = GetEntityTypeHandle(),
                        m_CommandBuffer = m_endFrameBarrier.CreateCommandBuffer(),
                        em = EntityManager,
                        nextUpdateFrame = UnityEngine.Time.frameCount + WEModData.InstanceWE.FramesCheckUpdateVal,
                        intervalUpdate = WEModData.InstanceWE.FramesCheckUpdateVal
                    };
                    for (int i = 0; i < tempArr.Length; i++)
                    {
                        job.Execute(tempArr[i]);
                    }
                }
            }

            Dependency.Complete();
        }

        /// <summary>
        /// Job for updating formulae on text data entities
        /// </summary>
        private struct WEUpdateFormulaesJob
        {
            public ComponentTypeHandle<WETextDataMain> m_MainDataHdl;
            public ComponentTypeHandle<WETextDataMaterial> m_MaterialDataHdl;
            public ComponentTypeHandle<WETextDataTransform> m_TransformDataHdl;
            public ComponentTypeHandle<WETextDataMesh> m_MeshDataHdl;
            public ComponentTypeHandle<WETextDataDirtyFormulae> m_DirtyFormulaeHdl;
            public EntityTypeHandle m_EntityType;
            public EntityCommandBuffer m_CommandBuffer;
            public EntityManager em;
            public int nextUpdateFrame;
            internal int intervalUpdate;

            public void Execute(in ArchetypeChunk chunk)
            {
                var entities = chunk.GetNativeArray(m_EntityType);
                var mainData = chunk.GetNativeArray(ref m_MainDataHdl);
                var materialData = chunk.GetNativeArray(ref m_MaterialDataHdl);
                var transformData = chunk.GetNativeArray(ref m_TransformDataHdl);
                var meshData = chunk.GetNativeArray(ref m_MeshDataHdl);
                var dirtyFormulae = chunk.GetNativeArray(ref m_DirtyFormulaeHdl);
                for (int i = 0; i < entities.Length; i++)
                {
                    var main = mainData[i];
                    var material = materialData[i];
                    var transform = transformData[i];
                    var mesh = meshData[i];
                    var dirtyData = dirtyFormulae[i];

                    var anyChanged = material.UpdateFormulaes(em, dirtyData.geometry, dirtyData.vars);
                    var canMultiply = mesh.TextType == WESimulationTextType.Placeholder;
                    var transformChanged = transform.UpdateFormulae(em, dirtyData.geometry, dirtyData.vars, canMultiply);
                    anyChanged |= transformChanged | mesh.UpdateFormulaes(em, dirtyData.geometry, dirtyData.vars);
                    if (anyChanged)
                    {
                        main.lastChangeFrame = main.nextUpdateFrame;
                    }
                    main.nextUpdateFrame = nextUpdateFrame + intervalUpdate + (i % intervalUpdate);
                    mainData[i] = main;
                    materialData[i] = material;
                    transformData[i] = transform;
                    meshData[i] = mesh;
                    if (canMultiply && transformChanged)
                    {
                        m_CommandBuffer.AddComponent<WETemplateDirtyInstancing>(entities[i]);
                        m_CommandBuffer.SetComponentEnabled<WETemplateDirtyInstancing>(entities[i], true);
                    }
                    m_CommandBuffer.SetComponentEnabled<WETextDataDirtyFormulae>(entities[i], false);
                }
            }
        }
    }
}
