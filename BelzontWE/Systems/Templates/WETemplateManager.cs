using Belzont.Interfaces;
using Belzont.Serialization;
using Belzont.Utils;
using Colossal;
using Colossal.Serialization.Entities;
using Game;
using Game.Common;
using Game.Prefabs;
using Game.Rendering;
using Game.SceneFlow;
using Game.Tools;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;


#if BURST
#endif

namespace BelzontWE
{
    public partial class WETemplateManager : SystemBase, IBelzontSerializableSingleton<WETemplateManager>
    {
        public const string SIMPLE_LAYOUT_EXTENSION = "welayout.xml";
        public const string PREFAB_LAYOUT_EXTENSION = "wedefault.xml";
        public static readonly string SAVED_PREFABS_FOLDER = Path.Combine(BasicIMod.ModSettingsRootFolder, "prefabs");

        public const int CURRENT_VERSION = 0;

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out int version);
            if (version > CURRENT_VERSION)
            {
                LogUtils.DoWarnLog($"Invalid version for {GetType()}: {version}");
                return;
            }
            reader.Read(out int length);
            RegisteredTemplates.Clear();
            for (var i = 0; i < length; i++)
            {
                reader.Read(out string key);
                reader.Read(out Entity value);
                this[key] = value;
            }
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(CURRENT_VERSION);
            var length = RegisteredTemplates.Count();
            writer.Write(length);
            var keys = RegisteredTemplates.GetKeyArray(Allocator.Temp);
            for (var i = 0; i < length; i++)
            {
                writer.Write(keys[i].ToString());
                writer.Write(RegisteredTemplates[keys[i]]);
            }
            keys.Dispose();
        }

        private UnsafeParallelHashMap<FixedString32Bytes, Entity> RegisteredTemplates;
        private NativeHashSet<Entity> m_obsoleteTemplateList;
        private PrefabSystem m_prefabSystem;
        private EndFrameBarrier m_endFrameBarrier;
        private EntityQuery m_templateBasedEntities;
        private EntityQuery m_uncheckedWePrefabLayoutQuery;
        private EntityQuery m_dirtyWePrefabLayoutQuery;
        private EntityQuery m_prefabsToMarkDirty;
        private UnsafeParallelHashMap<long, Entity> PrefabTemplates;

        public ref UnsafeParallelHashMap<FixedString32Bytes, Entity> RegisteredTemplatesRef => ref RegisteredTemplates;

        public Entity this[FixedString32Bytes idx]
        {
            get
            {
                var value = RegisteredTemplates.TryGetValue(idx, out var entity) ? entity : Entity.Null;
                if (BasicIMod.DebugMode) LogUtils.DoLog($"Loaded {value} @ {idx}");
                return value;
            }
            set
            {
                if (RegisteredTemplates.TryGetValue(idx, out var obsoleteTemplate))
                {
                    m_obsoleteTemplateList.Add(obsoleteTemplate);
                    RegisteredTemplates.Remove(idx);
                }
                else if (value != Entity.Null)
                {
                    m_obsoleteTemplateList.Add(Entity.Null);
                }
                if (BasicIMod.DebugMode) LogUtils.DoLog($"Saved {value} @ {idx}");
                if (value != Entity.Null)
                {
                    RegisteredTemplates.Add(idx, value);
                }
            }
        }

        protected override void OnCreate()
        {
            RegisteredTemplates = new UnsafeParallelHashMap<FixedString32Bytes, Entity>(0, Allocator.Persistent);
            PrefabTemplates = new UnsafeParallelHashMap<long, Entity>(0, Allocator.Persistent);
            m_obsoleteTemplateList = new NativeHashSet<Entity>(10, Allocator.Persistent);
            m_prefabSystem = World.GetExistingSystemManaged<PrefabSystem>();
            m_endFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
            m_templateBasedEntities = GetEntityQuery(new EntityQueryDesc[]
              {
                    new ()
                    {
                        All = new ComponentType[]
                        {
                            ComponentType.ReadOnly<WETemplateUpdater>(),
                        },
                        None = new ComponentType[]
                        {
                            ComponentType.ReadOnly<WEWaitingRendering>(),
                            ComponentType.ReadOnly<Temp>(),
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
                            ComponentType.ReadOnly<WETemplateForPrefab>(),
                            ComponentType.ReadOnly<Temp>(),
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
                                ComponentType.ReadOnly<Temp>(),
                                ComponentType.ReadOnly<Deleted>(),
                            }
                        }
              });
            m_prefabsToMarkDirty = GetEntityQuery(new EntityQueryDesc[]
            {
                    new ()
                    {
                        Any = new ComponentType[]
                        {
                            ComponentType.ReadOnly<WETemplateForPrefabEmpty>(),
                            ComponentType.ReadOnly<WETemplateForPrefab>(),
                        },
                        None = new ComponentType[]
                        {
                            ComponentType.ReadOnly<WETemplateForPrefabDirty>(),
                            ComponentType.ReadOnly<Temp>(),
                            ComponentType.ReadOnly<Deleted>(),
                        }
                    }
            });
        }

        protected override void OnDestroy()
        {
            RegisteredTemplates.Dispose();
            m_obsoleteTemplateList.Dispose();
            PrefabTemplates.Dispose();
        }

        protected override void OnUpdate()
        {
            if (GameManager.instance.isLoading || GameManager.instance.isGameLoading)
            {
                EntityManager.AddComponent<WETemplateForPrefabDirty>(m_prefabsToMarkDirty);
                return;
            }

            UpdatePrefabIndexDictionary();
            if (!m_obsoleteTemplateList.IsEmpty)
            {
                if (!m_templateBasedEntities.IsEmpty)
                {
                    var job = new WEPlaceholderTemplateUpdaterJob
                    {
                        m_EntityType = GetEntityTypeHandle(),
                        m_prefabUpdaterHdl = GetComponentTypeHandle<WETemplateUpdater>(true),
                        m_CommandBuffer = m_endFrameBarrier.CreateCommandBuffer(),
                        m_obsoleteTemplateList = m_obsoleteTemplateList,
                    };
                    var schedule = job.Schedule(m_templateBasedEntities, Dependency);
                    Dependency = schedule;
                    schedule.GetAwaiter().OnCompleted(() => m_obsoleteTemplateList.Clear());
                }
            }
            if (!m_uncheckedWePrefabLayoutQuery.IsEmpty)
            {
                Dependency = new WEPrefabTemplateFilterJob
                {
                    m_EntityType = GetEntityTypeHandle(),
                    m_prefabRefHdl = GetComponentTypeHandle<PrefabRef>(true),
                    m_prefabDataLkp = GetComponentLookup<PrefabData>(true),
                    m_prefabEmptyLkp = GetComponentLookup<WETemplateForPrefabEmpty>(true),
                    m_prefabDirtyLkp = GetComponentLookup<WETemplateForPrefabDirty>(true),
                    m_prefabLayoutLkp = GetComponentLookup<WETemplateForPrefab>(true),
                    m_subRefLkp = GetBufferLookup<WESubTextRef>(true),
                    m_TextDataLkp = GetComponentLookup<WETextData>(true),
                    m_CommandBuffer = m_endFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                    m_indexesWithLayout = PrefabTemplates,
                }.ScheduleParallel(m_uncheckedWePrefabLayoutQuery, Dependency);
            }
            if (!m_dirtyWePrefabLayoutQuery.IsEmpty)
            {
                Dependency = new WEPrefabTemplateFilterJob
                {
                    m_EntityType = GetEntityTypeHandle(),
                    m_prefabRefHdl = GetComponentTypeHandle<PrefabRef>(true),
                    m_prefabDataLkp = GetComponentLookup<PrefabData>(true),
                    m_prefabEmptyLkp = GetComponentLookup<WETemplateForPrefabEmpty>(true),
                    m_prefabDirtyLkp = GetComponentLookup<WETemplateForPrefabDirty>(true),
                    m_prefabLayoutLkp = GetComponentLookup<WETemplateForPrefab>(true),
                    m_subRefLkp = GetBufferLookup<WESubTextRef>(true),
                    m_TextDataLkp = GetComponentLookup<WETextData>(true),
                    m_CommandBuffer = m_endFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                    m_indexesWithLayout = PrefabTemplates,
                }.ScheduleParallel(m_dirtyWePrefabLayoutQuery, Dependency);
            }
        }

        public JobHandle SetDefaults(Context context)
        {
            RegisteredTemplates.Clear();
            return Dependency;
        }

        public void MarkPrefabsDirty() => isPrefabListDirty = true;
        private readonly Dictionary<string, long> PrefabNameToIndex = new();
        private bool isPrefabListDirty = true;
        private void UpdatePrefabIndexDictionary()
        {
            if (!isPrefabListDirty) return;
            PrefabNameToIndex.Clear();
            var prefabs = PrefabSystemOverrides.LoadedPrefabBaseList(m_prefabSystem);
            var entities = PrefabSystemOverrides.LoadedPrefabEntitiesList(m_prefabSystem);
            foreach (var prefab in prefabs)
            {
                var data = EntityManager.GetComponentData<PrefabData>(entities[prefab]);
                PrefabNameToIndex[prefab.name] = data.m_Index;
            }
            LoadTemplatesFromFolder();
            EntityManager.AddComponent<WETemplateForPrefabDirty>(m_prefabsToMarkDirty);
            isPrefabListDirty = false;
        }

        private void LoadTemplatesFromFolder()
        {
            var currentValues = PrefabTemplates.GetValueArray(Allocator.Temp);
            for (int i = 0; i < currentValues.Length; i++)
            {
                EntityManager.DestroyEntity(currentValues[i]);
            }
            currentValues.Dispose();

            PrefabTemplates.Clear();
            var files = Directory.GetFiles(SAVED_PREFABS_FOLDER, $"*.{PREFAB_LAYOUT_EXTENSION}");
            foreach (var f in files)
            {
                var prefabName = Path.GetFileName(f)[..^(PREFAB_LAYOUT_EXTENSION.Length + 1)];
                if (!PrefabNameToIndex.TryGetValue(prefabName, out var idx))
                {
                    LogUtils.DoWarnLog($"No prefab loaded with name: {prefabName}. Skipping...");
                    continue;
                }
                var tree = WETextDataTree.FromXML(File.ReadAllText(f));
                if (tree == null) continue;
                tree.self = new WETextDataXml
                {
                    textType = WESimulationTextType.Archetype,
                    offsetPosition = default,
                    offsetRotation = default,
                    text = null
                };
                var generatedEntity = WELayoutUtility.CreateEntityFromTree(Entity.Null, tree, EntityManager);
                PrefabTemplates[idx] = generatedEntity;
                if (BasicIMod.DebugMode) LogUtils.DoLog($"Loaded template for prefab: //{prefabName}// => {generatedEntity}");
            }
        }

        public bool CityTemplateExists(string name) => RegisteredTemplates.ContainsKey(name);
        public Dictionary<string, Entity> ListCityTemplates()
        {
            var arr = RegisteredTemplates.GetKeyArray(Allocator.Temp);
            var result = arr.ToArray().OrderBy(x => x).ToDictionary(x => x.ToString(), x => RegisteredTemplates[x]);
            arr.Dispose();
            return result;
        }
        public unsafe int GetCityTemplateUsageCount(string name)
        {
            if (m_templateBasedEntities.IsEmptyIgnoreFilter || !RegisteredTemplates.TryGetValue(name, out var templateEntity)) return 0;
            var counterResult = 0;
            var job = new WEPlaceholcerTemplateUsageCount
            {
                m_templateToCheck = templateEntity,
                m_updaterHdl = GetComponentTypeHandle<WETemplateUpdater>(),
                m_counter = &counterResult
            };
            job.Schedule(m_templateBasedEntities, Dependency).Complete();
            return counterResult;
        }


        [BurstCompile]
        private unsafe struct WEPlaceholcerTemplateUsageCount : IJobChunk
        {
            public Entity m_templateToCheck;
            public ComponentTypeHandle<WETemplateUpdater> m_updaterHdl;
            public int* m_counter;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var templates = chunk.GetNativeArray(ref m_updaterHdl);
                for (int i = 0; i < templates.Length; i++)
                {
                    if (templates[i].templateEntity == m_templateToCheck) *m_counter += 1;
                }
            }
        }
    }
}