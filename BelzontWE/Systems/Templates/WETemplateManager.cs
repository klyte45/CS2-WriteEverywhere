using Belzont.Interfaces;
using Belzont.Serialization;
using Belzont.Utils;
using Colossal;
using Colossal.Entities;
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

        public const int CURRENT_VERSION = 1;

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out int version);
            if (version == 0)
            {
                reader.Read(out int length);
                for (var i = 0; i < length; i++)
                {
                    reader.Read(out string _);
                    reader.Read(out Entity _);
                }
                SetDefaults(default);
                return;
            }
            if (version > CURRENT_VERSION)
            {
                LogUtils.DoWarnLog($"Invalid version for {GetType()}: {version}");
                return;
            }
            reader.Read(out int lengthTemplates);
            RegisteredTemplates.Clear();
            var errors = new List<string>();
            for (var i = 0; i < lengthTemplates; i++)
            {
                reader.Read(out string key);
                reader.Read(out int lengthData);
                if (lengthData == 0) continue;
                var tempArray = new NativeArray<byte>(lengthData, Allocator.Temp);
                reader.Read(tempArray);
                var dataUnzipped = ZipUtils.Unzip(tempArray.ToArray());
                tempArray.Dispose();
                var dataTree = XmlUtils.DefaultXmlDeserialize<WETextDataTree>(dataUnzipped, (x, e) =>
                   {
                       LogUtils.DoErrorLog($"Invalid layout data for Registered template '{key}': {e.Message}. Skipping");
                       if (BasicIMod.DebugMode) LogUtils.DoLog($"data: {x}\n Exception: {e}");
                   });
                if (dataTree != null)
                {
                    var newEntity = WELayoutUtility.CreateEntityFromTree(dataTree, Entity.Null, EntityManager);
                    if (!SaveCityTemplate(key, newEntity, false)) errors.Add(key);
                }
            }
            reader.Read(out int lengthInstances);

            for (var i = 0; i < lengthInstances; i++)
            {
                reader.Read(out Entity key);
                reader.Read(out int lengthData);
                var tempArray = new NativeArray<byte>(lengthData, Allocator.Temp);
                reader.Read(tempArray);
                var dataUnzipped = ZipUtils.Unzip(tempArray.ToArray());
                tempArray.Dispose();
                var dataTree = XmlUtils.DefaultXmlDeserialize<WESelflessTextDataTree>(dataUnzipped, (x, e) =>
                {
                    LogUtils.DoErrorLog($"Invalid layout data for Registered template '{key}': {e.Message}. Skipping");
                    if (BasicIMod.DebugMode) LogUtils.DoLog($"data: {x}\n Exception: {e}");
                });
                if (dataTree != null && dataTree.children.Length > 0)
                {
                    var children = dataTree.children;
                    foreach (var item in children)
                    {
                        WELayoutUtility.CreateEntityFromTree(item, key, EntityManager);
                    }
                }
            }
            if (errors.Count > 0) LogUtils.DoErrorLog($"WE: The following city layouts failed being loaded. Load again with at least the Debug log level enabled to get details.\n['{string.Join("', '", errors)}']");
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
                var treeData = WETextDataTree.FromEntity(RegisteredTemplates[keys[i]], EntityManager);
                if (treeData == null)
                {
                    LogUtils.DoErrorLog($"WE: OBJECT WAS NULL: Template {keys[i]} - {RegisteredTemplates[keys[i]]} - {EntityManager.TryGetComponent<WETextData>(RegisteredTemplates[keys[i]], out var cmp)} {cmp.EffectiveText}");
                    writer.Write(0);
                    continue;
                }
                var data = ZipUtils.Zip(treeData.ToXML(false));
                var tempArray = new NativeArray<byte>(data, Allocator.Temp);
                writer.Write(data.Length);
                writer.Write(tempArray);
                tempArray.Dispose();
            }
            keys.Dispose();

            var prefabsWithLayout = m_prefabsDataToSerialize.ToEntityArray(Allocator.Temp);
            writer.Write(prefabsWithLayout.Length);
            if (prefabsWithLayout.Length > 0)
            {
                for (var j = 0; j < prefabsWithLayout.Length; j++)
                {
                    writer.Write(prefabsWithLayout[j]);
                    var data = ZipUtils.Zip(WESelflessTextDataTree.FromEntity(prefabsWithLayout[j], EntityManager).ToXML(false));
                    var tempArray = new NativeArray<byte>(data, Allocator.Temp);
                    writer.Write(data.Length);
                    writer.Write(tempArray);
                    tempArray.Dispose();

                }
            }

        }

        private UnsafeParallelHashMap<FixedString128Bytes, Entity> RegisteredTemplates;
        private NativeHashSet<Entity> m_obsoleteTemplateList;
        private PrefabSystem m_prefabSystem;
        private EndFrameBarrier m_endFrameBarrier;
        private EntityQuery m_templateBasedEntities;
        private EntityQuery m_uncheckedWePrefabLayoutQuery;
        private EntityQuery m_dirtyWePrefabLayoutQuery;
        private EntityQuery m_prefabsToMarkDirty;
        private EntityQuery m_prefabsDataToSerialize;
        private UnsafeParallelHashMap<long, Entity> PrefabTemplates;
        private readonly Queue<System.Action<EntityCommandBuffer>> m_executionQueue = new();


        public ref UnsafeParallelHashMap<FixedString128Bytes, Entity> RegisteredTemplatesRef => ref RegisteredTemplates;

        public Entity this[FixedString128Bytes idx]
        {
            get
            {
                var value = RegisteredTemplates.TryGetValue(idx, out var entity) ? entity : Entity.Null;
                if (BasicIMod.DebugMode) LogUtils.DoLog($"Loaded {value} @ {idx}");
                return value;
            }
        }

        protected override void OnCreate()
        {
            RegisteredTemplates = new UnsafeParallelHashMap<FixedString128Bytes, Entity>(0, Allocator.Persistent);
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
            m_prefabsDataToSerialize = GetEntityQuery(new EntityQueryDesc[]
            {
                    new ()
                    {
                        All = new ComponentType[]
                        {
                            ComponentType.ReadOnly<WESubTextRef>(),
                            ComponentType.ReadOnly<PrefabRef>(),
                        },
                        None = new ComponentType[]
                        {
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
            if (GameManager.instance.isLoading || GameManager.instance.isGameLoading) return;

            if (m_executionQueue.Count > 0)
            {
                var cmdBuffer = m_endFrameBarrier.CreateCommandBuffer();
                while (m_executionQueue.TryDequeue(out var nextAction))
                {
                    nextAction(cmdBuffer);
                }
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
                else
                {
                    m_obsoleteTemplateList.Clear();
                }                
            }
            else if (!m_uncheckedWePrefabLayoutQuery.IsEmpty)
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
            else if (!m_dirtyWePrefabLayoutQuery.IsEmpty)
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
            Dependency.Complete();
        }

        public JobHandle SetDefaults(Context context)
        {
            RegisteredTemplates.Clear();
            return Dependency;
        }

        #region Prefab Layout
        public void MarkPrefabsDirty() => isPrefabListDirty = true;
        private readonly Dictionary<string, long> PrefabNameToIndex = new();
        private bool isPrefabListDirty = true;

        public int CanBePrefabLayout(Entity e) => CanBePrefabLayout(e, true);
        public int CanBePrefabLayout(Entity e, bool isRoot)
        {
            if (isRoot)
            {
                if (EntityManager.TryGetBuffer<WESubTextRef>(e, true, out var subRef))
                {
                    for (int i = 0; i < subRef.Length; i++)
                    {
                        if (CanBePrefabLayout(subRef[i].m_weTextData, false) != 0)
                        {
                            LogUtils.DoInfoLog($"Failed validation to transform to Prefab Default: A child node (${i}) failed validation");
                            return 2;
                        }
                    }
                }
                else
                {
                    LogUtils.DoInfoLog($"Failed validation to transform to Prefab Default: The root node had no children. When exporting for prefabs, the selected node must have children typed as Placeholder items. The root setting itself is ignored.");
                    return 1;
                }
            }
            else
            {
                if (!EntityManager.TryGetComponent<WETextData>(e, out var weData))
                {
                    LogUtils.DoInfoLog($"Failed validation to transform to Prefab Default: The node don't have a text layout data.");
                    return 3;
                }

                if ((weData.TextType < 0 || weData.TextType > WESimulationTextType.Placeholder) && weData.TextType != WESimulationTextType.WhiteTexture)
                {
                    LogUtils.DoInfoLog($"Failed validation to transform to Prefab Default: All children must have type 'Placeholder', 'WhiteTexture', 'Image' or 'Text'.");
                    return 4;
                };
                if (weData.TextType == WESimulationTextType.Placeholder && EntityManager.TryGetBuffer<WESubTextRef>(e, true, out var subRef) && !subRef.IsEmpty)
                {
                    LogUtils.DoInfoLog($"Failed validation to transform to Prefab Default: The node must not have children, as any Placeholder item don't.");
                    return 5;
                }
            }
            return 0;
        }
        private void UpdatePrefabIndexDictionary()
        {
            if (!isPrefabListDirty) return;
            if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"UpdatePrefabIndexDictionary!!!");
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
            var files = Directory.GetFiles(SAVED_PREFABS_FOLDER, $"*.{PREFAB_LAYOUT_EXTENSION}", SearchOption.AllDirectories);
            foreach (var f in files)
            {
                var prefabName = Path.GetFileName(f)[..^(PREFAB_LAYOUT_EXTENSION.Length + 1)];
                if (!PrefabNameToIndex.TryGetValue(prefabName, out var idx))
                {
                    LogUtils.DoWarnLog($"No prefab loaded with name: {prefabName}. Skipping...");
                    continue;
                }
                if (PrefabTemplates.ContainsKey(idx))
                {
                    LogUtils.DoWarnLog($"Prefab defaults already loaded for '{prefabName}'. Skipping data from file at '{f}'");
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
                var generatedEntity = WELayoutUtility.CreateEntityFromTree(tree, Entity.Null, EntityManager);
                var validationResults = CanBePrefabLayout(generatedEntity);
                if (validationResults == 0)
                {
                    PrefabTemplates[idx] = generatedEntity;
                    if (BasicIMod.DebugMode) LogUtils.DoLog($"Loaded template for prefab: //{prefabName}// => {generatedEntity}");
                }
                else
                {
                    EntityManager.DestroyEntity(generatedEntity);
                    LogUtils.DoWarnLog($"Failed loding default template for prefab '{prefabName}'. Check previous lines at mod log to more information ({validationResults})");
                }
            }
        }
        #endregion
        #region City Templates
        public int CanBeTransformedToTemplate(Entity e)
        {
            if (!EntityManager.TryGetComponent<WETextData>(e, out var weData))
            {
                LogUtils.DoInfoLog($"Failed validation to transform to City Template: No text data found");
                return 1;
            }

            if (weData.TextType != WESimulationTextType.Text && weData.TextType != WESimulationTextType.Image && weData.TextType != WESimulationTextType.WhiteTexture)
            {
                LogUtils.DoInfoLog($"Failed validation to transform to City Template: Only white textures, text and image items are allowed in a city template");
                return 2;
            }
            if (EntityManager.TryGetBuffer<WESubTextRef>(e, true, out var subRef))
            {
                for (int i = 0; i < subRef.Length; i++)
                {
                    if (CanBeTransformedToTemplate(subRef[i].m_weTextData) != 0)
                    {
                        LogUtils.DoInfoLog($"Failed validation to transform to City Template: Item #{i} failed validation");
                        return 3;
                    }
                }
            }
            return 0;
        }
        public bool SaveCityTemplate(string name, Entity e) => SaveCityTemplate(name, e, true);

        private bool SaveCityTemplate(string name, Entity e, bool clone)
        {
            if (CanBeTransformedToTemplate(e) != 0)
            {
                LogUtils.DoInfoLog($"Failed {(clone ? "storing" : "loading")} layout '{name}': it failed while verifying if it could be transformed to template, check previous lines for details.");
                return false;
            }

            Entity templateEntity = clone ? WELayoutUtility.DoCloneTextItemReferenceSelf(e, default, EntityManager) : e;
            if (!EntityManager.HasComponent<WETemplateData>(templateEntity)) EntityManager.AddComponent<WETemplateData>(templateEntity);
            if (RegisteredTemplates.TryGetValue(name, out var obsoleteTemplate))
            {
                m_obsoleteTemplateList.Add(obsoleteTemplate);
                RegisteredTemplates.Remove(name);
            }
            else if (e != Entity.Null)
            {
                m_obsoleteTemplateList.Add(Entity.Null);
            }
            if (BasicIMod.DebugMode) LogUtils.DoLog($"Saved {e} @ {name}");
            if (e != Entity.Null)
            {
                RegisteredTemplates.Add(name, e);
            }
            return true;
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

        public void RenameCityTemplate(string oldName, string newName)
        {
            if (oldName == newName || oldName.TrimToNull() == null || newName.TrimToNull() == null || !CityTemplateExists(oldName)) return;
            RegisteredTemplates[newName] = RegisteredTemplates[oldName];
            RegisteredTemplates.Remove(oldName);
        }

        public void DeleteCityTemplate(string name)
        {
            if (name != null && CityTemplateExists(name))
            {
                EntityManager.DestroyEntity(RegisteredTemplates[name]);
                RegisteredTemplates.Remove(name);
            }
        }
        public void DuplicateCityTemplate(string srcName, string newName)
        {
            if (srcName == newName || srcName.TrimToNull() == null || newName.TrimToNull() == null || !CityTemplateExists(srcName)) return;
            SaveCityTemplate(newName, RegisteredTemplates[srcName], true);
        }
        #endregion

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