using Belzont.Interfaces;
using Belzont.Serialization;
using Belzont.Utils;
using BelzontWE.Sprites;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game;
using Game.Common;
using Game.Prefabs;
using Game.Rendering;
using Game.SceneFlow;
using Game.Tools;
using Game.UI;
using Game.UI.Localization;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;


#if BURST
#endif

namespace BelzontWE
{
    public partial class WETemplateManager : SystemBase, IBelzontSerializableSingleton<WETemplateManager>
    {
        public const string SIMPLE_LAYOUT_EXTENSION = "welayout.xml";
        public const string PREFAB_LAYOUT_EXTENSION = "wedefault.xml";
        public static readonly string SAVED_PREFABS_FOLDER = Path.Combine(BasicIMod.ModSettingsRootFolder, "layouts");
        public static readonly string SAVED_MODREPLACEMENTS_FOLDER = Path.Combine(BasicIMod.ModSettingsRootFolder, "modReplacementProfiles");
        public const string LAYOUT_REPLACEMENTS_EXTENSION = "weprefabreplace.xml";
        public static WETemplateManager Instance { get; private set; }

        public const int CURRENT_VERSION = 1;

        private Dictionary<FixedString128Bytes, WETextDataXmlTree> RegisteredTemplates;
        private PrefabSystem m_prefabSystem;
        private EndFrameBarrier m_endFrameBarrier;
        private EntityQuery m_templateBasedEntities;
        private EntityQuery m_uncheckedWePrefabLayoutQuery;
        private EntityQuery m_dirtyWePrefabLayoutQuery;
        private EntityQuery m_prefabsToMarkDirty;
        private EntityQuery m_prefabsDataToSerialize;
        private EntityQuery m_entitiesToBeUpdatedInMain;
        private EntityQuery m_prefabArchetypesToBeUpdatedInMain;
        private Dictionary<long, WETextDataXmlTree> PrefabTemplates;
        private readonly Queue<Action<EntityCommandBuffer>> m_executionQueue = new();
        private bool m_templatesDirty;

        public WETextDataXmlTree this[FixedString128Bytes idx]
        {
            get
            {
                var value = RegisteredTemplates.TryGetValue(idx, out var tree) ? tree : default;
                if (BasicIMod.DebugMode) LogUtils.DoLog($"Loaded {value} @ {idx}");
                return value;
            }
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out int version);
            if (version > CURRENT_VERSION)
            {
                LogUtils.DoWarnLog($"Invalid version for {GetType()}: {version}");
                return;
            }
            reader.Read(out int lengthTemplates);
            var valueArr = RegisteredTemplates.Values;
            RegisteredTemplates.Clear();
            for (var i = 0; i < lengthTemplates; i++)
            {
                reader.Read(out string key);
                reader.ReadNullCheck(out WETextDataXmlTree dataTree);
                if (dataTree is not null) RegisteredTemplates[key] = dataTree;
            }
            reader.Read(out int lengthInstances);
            for (var i = 0; i < lengthInstances; i++)
            {
                reader.Read(out Entity key);
                reader.ReadNullCheck(out WETextDataXmlTree dataTree);
                try
                {
                    if (dataTree?.children?.Length > 0)
                    {
                        var children = dataTree.children;
                        m_executionQueue.Enqueue((cmd) =>
                        {
                            ComponentLookup<WETextDataMain> tdLookup = GetComponentLookup<WETextDataMain>();
                            BufferLookup<WESubTextRef> subTextLookup = GetBufferLookup<WESubTextRef>();
                            WELayoutUtility.DoCreateLayoutItemArray(false, null, children, key, key, ref tdLookup, ref subTextLookup, cmd);
                        });
                    }
                }
                catch (Exception e)
                {
                    LogUtils.DoWarnLog($"IGNORING INSTANCE by exception: '{key}'\n{e}");
                }
            }
            if (version >= 1)
            {
                reader.Read(out string atlasesReplacementData);
                reader.Read(out string fontsReplacementData);
                m_atlasesReplacements.Clear();
                m_fontsReplacements.Clear();
                m_atlasesReplacements.AddRange(DeserializeReplacementData(atlasesReplacementData));
                m_fontsReplacements.AddRange(DeserializeReplacementData(fontsReplacementData));
            }
            else
            {
                m_atlasesReplacements.Clear();
                m_fontsReplacements.Clear();
            }
            ModReplacementDataVersion = 1;
            m_templatesDirty = true;
        }

        private const string L1_ITEM_SEPARATOR = "|";
        private const string L1_KV_SEPARATOR = "→";
        private const string L2_ITEM_SEPARATOR = "∫";
        private const string L2_KV_SEPARATOR = "↓";

        private Dictionary<string, Dictionary<string, string>> DeserializeReplacementData(string data)
        {
            return data.Split(L1_ITEM_SEPARATOR)
                .Where(x => x.Contains(L1_KV_SEPARATOR))
                .Select(x => x.Split(L1_KV_SEPARATOR))
                .ToDictionary(
                    x => x[0],
                    x => x[1]
                        .Split(L2_ITEM_SEPARATOR)
                        .Where(y => y.Contains(L2_KV_SEPARATOR))
                        .Select(y => y.Split(L2_KV_SEPARATOR))
                        .ToDictionary(y => y[0], y => y[1])
                );
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(CURRENT_VERSION);
            var length = RegisteredTemplates.Count();
            writer.Write(length);
            var keys = RegisteredTemplates.Keys.ToArray();
            for (var i = 0; i < length; i++)
            {
                writer.Write(keys[i].ToString());
                writer.WriteNullCheck(RegisteredTemplates[keys[i]]);
            }
            var prefabsWithLayout = m_prefabsDataToSerialize.ToEntityArray(Allocator.Temp);
            writer.Write(prefabsWithLayout.Length);
            if (prefabsWithLayout.Length > 0)
            {
                for (var j = 0; j < prefabsWithLayout.Length; j++)
                {
                    writer.Write(prefabsWithLayout[j]);
                    var data = WETextDataXmlTree.FromEntity(prefabsWithLayout[j], EntityManager);
                    writer.WriteNullCheck(data);
                }
            }
            prefabsWithLayout.Dispose();

            string atlasesReplacementData = string.Join(L1_ITEM_SEPARATOR, m_atlasesReplacements
                .Select(x => (x.Key, x.Value.Where(x => x.Key != x.Value).ToArray()))
                .Where(x => x.Item2.Length > 0)
                .Select(x => $"{x.Key}{L1_KV_SEPARATOR}{string.Join(L2_ITEM_SEPARATOR, x.Item2.Select(y => $"{y.Key}{L2_KV_SEPARATOR}{y.Value}"))}"));
            string fontsReplacementData = string.Join('|', m_fontsReplacements
                .Select(x => (x.Key, x.Value.Where(x => x.Key != x.Value).ToArray()))
                .Where(x => x.Item2.Length > 0)
                .Select(x => $"{x.Key}{L1_KV_SEPARATOR}{string.Join(L2_ITEM_SEPARATOR, x.Item2.Select(y => $"{y.Key}{L2_KV_SEPARATOR}{y.Value}"))}"));
            writer.Write(atlasesReplacementData);
            writer.Write(fontsReplacementData);
        }
        protected override void OnCreate()
        {
            Instance = this;
            KFileUtils.EnsureFolderCreation(SAVED_MODREPLACEMENTS_FOLDER);
            RegisteredTemplates = new();
            PrefabTemplates = new();
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
            m_entitiesToBeUpdatedInMain = GetEntityQuery(new EntityQueryDesc[]
            {
                    new ()
                    {
                        All = new ComponentType[]
                        {
                            ComponentType.ReadOnly<WETextDataMain>(),
                            ComponentType.ReadOnly<WETextDataMaterial>(),
                            ComponentType.ReadOnly<WETextDataMesh>(),
                            ComponentType.ReadOnly<WETextDataTransform>(),
                            ComponentType.ReadOnly<WEPlaceholderToBeProcessedInMain>(),
                        },
                        None = new ComponentType[]
                        {
                            ComponentType.ReadOnly<Temp>(),
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
                            ComponentType.ReadOnly<Temp>(),
                            ComponentType.ReadOnly<Deleted>(),
                            ComponentType.ReadOnly<WETemplateForPrefab>(),
                        }
                    }
            });

            WEAtlasesLibrary.GetWhiteTextureBRI();
        }

        protected override void OnDestroy()
        {
        }

        private Coroutine m_updatingEntitiesOnMain;

        private void UpdatePrefabArchetypes(NativeArray<ArchetypeChunk> chunks)
        {
            var m_TextDataLkp = GetComponentLookup<WETextDataMain>();
            var m_prefabDataLkp = GetComponentLookup<PrefabData>();
            var m_prefabRefHdl = GetComponentTypeHandle<PrefabRef>();
            var m_subRefLkp = GetBufferLookup<WESubTextRef>();
            var m_prefabEmptyLkp = GetComponentLookup<WETemplateForPrefabEmpty>();
            var globalCounter = 0;
            EntityCommandBuffer cmd = m_endFrameBarrier.CreateCommandBuffer();
            for (int h = 0; h < chunks.Length; h++)
            {
                var chunk = chunks[h];
                var entities = chunk.GetNativeArray(GetEntityTypeHandle());
                var prefabRefs = chunk.GetNativeArray(ref m_prefabRefHdl);
                for (int i = 0; i < entities.Length; i++, globalCounter++)
                {
                    if (globalCounter >= 1000)
                    {
                        m_updatingEntitiesOnMain = null;
                        return;
                    }
                    var e = entities[i];
                    var prefabRef = prefabRefs[i];

                    cmd.RemoveComponent<WETemplateForPrefabToRunOnMain>(e);
                    if (m_prefabDataLkp.TryGetComponent(prefabRef.m_Prefab, out var prefabData))
                    {
                        if (PrefabTemplates.TryGetValue(prefabData.m_Index, out var newTemplate))
                        {
                            var guid = newTemplate.Guid;
                            var childEntity = WELayoutUtility.DoCreateLayoutItem(true, newTemplate.ModSource, newTemplate, e, Entity.Null, ref m_TextDataLkp, ref m_subRefLkp, cmd, WELayoutUtility.ParentEntityMode.TARGET_IS_SELF_FOR_PARENT);
                            if (m_prefabEmptyLkp.HasComponent(e)) cmd.RemoveComponent<WETemplateForPrefabEmpty>(e);
                            cmd.AddComponent<WETemplateForPrefab>(entities[i], new()
                            {
                                templateRef = guid,
                                childEntity = childEntity
                            });
                            continue;
                        }
                    }

                    cmd.AddComponent<WETemplateForPrefab>(entities[i], new()
                    {
                        templateRef = default,
                        childEntity = Entity.Null
                    });
                    if (!m_prefabEmptyLkp.HasComponent(e)) cmd.AddComponent<WETemplateForPrefabEmpty>(e);
                }
            }
        }
        private void UpdateLayouts(NativeArray<ArchetypeChunk> chunks)
        {
            var m_templateUpdaterLkp = GetComponentLookup<WETemplateUpdater>();
            var m_TextDataLkp = GetComponentLookup<WETextDataMain>();
            var m_subRefLkp = GetBufferLookup<WESubTextRef>();
            var toBeProcessedDataHdl = GetComponentTypeHandle<WEPlaceholderToBeProcessedInMain>();
            var globalCounter = 0;
            EntityCommandBuffer cmd = m_endFrameBarrier.CreateCommandBuffer();
            for (int h = 0; h < chunks.Length; h++)
            {
                var chunk = chunks[h];
                var entities = chunk.GetNativeArray(GetEntityTypeHandle());
                var dataToBeProcessedArray = chunk.GetNativeArray(ref toBeProcessedDataHdl);
                for (int i = 0; i < entities.Length; i++, globalCounter++)
                {
                    if (globalCounter >= 100)
                    {
                        m_updatingEntitiesOnMain = null;
                        return;
                    }
                    var e = entities[i];
                    var dataToBeProcessed = dataToBeProcessedArray[i];
                    var newData = RegisteredTemplates.TryGetValue(dataToBeProcessed.layoutName, out var targetTemplate) ? new WETemplateUpdater()
                    {
                        templateEntity = targetTemplate.Guid,
                        childEntity = WELayoutUtility.DoCreateLayoutItem(true, targetTemplate.ModSource, targetTemplate, e, Entity.Null, ref m_TextDataLkp, ref m_subRefLkp, cmd, WELayoutUtility.ParentEntityMode.TARGET_IS_SELF_PARENT_HAS_TARGET)
                    } : default;
                    if (m_templateUpdaterLkp.TryGetComponent(e, out var oldCmp))
                    {
                        cmd.AddComponent<Deleted>(oldCmp.childEntity);
                        cmd.SetComponent(e, newData);
                    }
                    else
                    {
                        cmd.AddComponent(e, newData);
                    }
                    cmd.RemoveComponent<WEPlaceholderToBeProcessedInMain>(e);
                }
            }
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

            if (m_templatesDirty)
            {
                EntityManager.AddComponent<WEWaitingRendering>(m_templateBasedEntities);
                m_templatesDirty = false;
            }
            if (m_updatingEntitiesOnMain is null)
            {
                if (!m_entitiesToBeUpdatedInMain.IsEmpty)
                {
                    var entitiesToUpdate = m_entitiesToBeUpdatedInMain.ToArchetypeChunkArray(Allocator.Persistent);
                    UpdateLayouts(entitiesToUpdate);
                    entitiesToUpdate.Dispose();
                }
                else if (!m_prefabArchetypesToBeUpdatedInMain.IsEmpty)
                {
                    var entitiesToUpdate = m_prefabArchetypesToBeUpdatedInMain.ToArchetypeChunkArray(Allocator.Persistent);
                    UpdatePrefabArchetypes(entitiesToUpdate);
                    ModReplacementDataVersion++;
                    entitiesToUpdate.Dispose();
                }
                else if (!m_uncheckedWePrefabLayoutQuery.IsEmpty)
                {
                    var keysWithTemplate = new NativeHashMap<long, Colossal.Hash128>(0, Allocator.TempJob);
                    foreach (var i in PrefabTemplates)
                    {
                        keysWithTemplate[i.Key] = i.Value.Guid;
                    }
                    Dependency = new WEPrefabTemplateFilterJob
                    {
                        m_EntityType = GetEntityTypeHandle(),
                        m_prefabRefHdl = GetComponentTypeHandle<PrefabRef>(true),
                        m_prefabDataLkp = GetComponentLookup<PrefabData>(true),
                        m_prefabEmptyLkp = GetComponentLookup<WETemplateForPrefabEmpty>(true),
                        m_prefabDirtyLkp = GetComponentLookup<WETemplateForPrefabDirty>(true),
                        m_prefabLayoutLkp = GetComponentLookup<WETemplateForPrefab>(true),
                        m_subRefLkp = GetBufferLookup<WESubTextRef>(true),
                        m_TextDataLkp = GetComponentLookup<WETextDataMain>(true),
                        m_CommandBuffer = m_endFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                        m_indexesWithLayout = keysWithTemplate,
                        m_templateUpdaterLkp = GetComponentLookup<WETemplateUpdater>(true),
                    }.ScheduleParallel(m_uncheckedWePrefabLayoutQuery, Dependency);
                    keysWithTemplate.Dispose(Dependency);
                }
                else if (!m_dirtyWePrefabLayoutQuery.IsEmpty)
                {
                    var keysWithTemplate = new NativeHashMap<long, Colossal.Hash128>(0, Allocator.TempJob);
                    foreach (var i in PrefabTemplates)
                    {
                        keysWithTemplate[i.Key] = i.Value.Guid;
                    }
                    Dependency = new WEPrefabTemplateFilterJob
                    {
                        m_EntityType = GetEntityTypeHandle(),
                        m_prefabRefHdl = GetComponentTypeHandle<PrefabRef>(true),
                        m_prefabDataLkp = GetComponentLookup<PrefabData>(true),
                        m_prefabEmptyLkp = GetComponentLookup<WETemplateForPrefabEmpty>(true),
                        m_prefabDirtyLkp = GetComponentLookup<WETemplateForPrefabDirty>(true),
                        m_prefabLayoutLkp = GetComponentLookup<WETemplateForPrefab>(true),
                        m_subRefLkp = GetBufferLookup<WESubTextRef>(true),
                        m_TextDataLkp = GetComponentLookup<WETextDataMain>(true),
                        m_CommandBuffer = m_endFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                        m_indexesWithLayout = keysWithTemplate,
                        m_templateUpdaterLkp = GetComponentLookup<WETemplateUpdater>(true),
                    }.ScheduleParallel(m_dirtyWePrefabLayoutQuery, Dependency);
                    keysWithTemplate.Dispose(Dependency);
                }
            }
            Dependency.Complete();
        }

        public JobHandle SetDefaults(Context context)
        {
            RegisteredTemplates.Clear();
            m_atlasesReplacements.Clear();
            m_fontsReplacements.Clear();
            ModReplacementDataVersion = 2;
            return Dependency;
        }

        #region Prefab Layout
        public void MarkPrefabsDirty() => isPrefabListDirty = true;
        private readonly Dictionary<string, long> PrefabNameToIndex = new();
        private bool isPrefabListDirty = true;

        public int CanBePrefabLayout(WETextDataXmlTree data) => CanBePrefabLayout(data, true);
        public int CanBePrefabLayout(WETextDataXmlTree data, bool isRoot) => CanBePrefabLayout(data.self, data.children, isRoot);
        public int CanBePrefabLayout(WESelflessTextDataTree data) => CanBePrefabLayout(null, data.children, true);
        private int CanBePrefabLayout(WETextDataXml self, WETextDataXmlTree[] children, bool isRoot)
        {
            if (isRoot)
            {
                if (children?.Length > 0)
                {
                    for (int i = 0; i < children.Length; i++)
                    {
                        if (CanBePrefabLayout(children[i], false) != 0)
                        {
                            LogUtils.DoInfoLog($"Failed validation to transform to Prefab Default: A child node ({i}) failed validation");
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
                if (self.layoutMesh is null && self.imageMesh is null && self.textMesh is null && self.whiteMesh is null)
                {
                    LogUtils.DoInfoLog($"Failed validation to transform to Prefab Default: All children must have type 'Placeholder', 'WhiteTexture', 'Image' or 'Text'.");
                    return 4;
                };
                if (self.layoutMesh is not null && children?.Length > 0)
                {
                    LogUtils.DoInfoLog($"Failed validation to transform to Prefab Default: The node must not have children, as any Placeholder item don't.");
                    return 5;
                }
            }
            return 0;
        }
        public bool IsLoadingLayouts => LoadingPrefabLayoutsCoroutine != null;
        private Coroutine LoadingPrefabLayoutsCoroutine;
        private const string LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID = "loadingPrefabTemplates";
        private const string ERRORS_LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID = "errorLoadingPrefabTemplates";
        private unsafe void UpdatePrefabIndexDictionary()
        {
            if (!isPrefabListDirty) return;
            if (LoadingPrefabLayoutsCoroutine != null) GameManager.instance.StopCoroutine(LoadingPrefabLayoutsCoroutine);
            if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"UpdatePrefabIndexDictionary!!!");

            LoadingPrefabLayoutsCoroutine = GameManager.instance.StartCoroutine(UpdatePrefabIndexDictionary_Coroutine());
        }
        private IEnumerator UpdatePrefabIndexDictionary_Coroutine()
        {
            isPrefabListDirty = false;
            NotificationHelper.NotifyProgress(LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID, 0);
            yield return 0;
            PrefabNameToIndex.Clear();
            var prefabs = PrefabSystemOverrides.LoadedPrefabBaseList(m_prefabSystem);
            var entities = PrefabSystemOverrides.LoadedPrefabEntitiesList(m_prefabSystem);

            NotificationHelper.NotifyProgress(LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID, 1, textI18n: $"{LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID}.gettingPrefabIndexes");
            yield return 0;
            foreach (var prefab in prefabs)
            {
                var data = EntityManager.GetComponentData<PrefabData>(entities[prefab]);
                PrefabNameToIndex[prefab.name] = data.m_Index;
            }
            NotificationHelper.NotifyProgress(LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID, 20, textI18n: $"{LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID}.prefabIndexesLoaded");
            yield return LoadTemplatesFromFolder(20, 80f);
        }

        private IEnumerator LoadTemplatesFromFolder(int offsetPercentage, float totalStep, string modName = null)
        {
            KFileUtils.EnsureFolderCreation(SAVED_PREFABS_FOLDER);
            var currentValues = PrefabTemplates.Keys.ToArray();
            NotificationHelper.NotifyProgress(LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID, Mathf.RoundToInt(offsetPercentage + (.01f * totalStep)), textI18n: $"{LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID}.erasingCachedLayouts");
            yield return 0;
            if (modName is null)
            {
                for (int i = 0; i < currentValues.Length; i++)
                {
                    PrefabTemplates.Remove(currentValues[i]);
                    if (i % 3 == 0)
                    {
                        NotificationHelper.NotifyProgress(LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID, Mathf.RoundToInt(offsetPercentage + ((.01f + (.09f * ((i + 1f) / currentValues.Length))) * totalStep)),
                            textI18n: $"{LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID}.disposedOldLayout", argsText: new()
                            {
                                ["progress"] = LocalizedString.Value($"{i}/{currentValues.Length}")
                            });
                        yield return 0;
                    }
                }
                PrefabTemplates.Clear();
            }
            NotificationHelper.NotifyProgress(LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID, Mathf.RoundToInt(offsetPercentage + (.11f * totalStep)), textI18n: $"{LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID}.searchingForFiles");
            yield return 0;
            var files = modName != null
                ? Directory.GetFiles(m_modsTemplatesFolder[modName].rootFolder, $"*.{PREFAB_LAYOUT_EXTENSION}", SearchOption.AllDirectories).Select(y => (y, m_modsTemplatesFolder[modName].id, $"{m_modsTemplatesFolder[modName].name}: {y[m_modsTemplatesFolder[modName].rootFolder.Length..]}")).ToArray()
                : Directory.GetFiles(SAVED_PREFABS_FOLDER, $"*.{PREFAB_LAYOUT_EXTENSION}", SearchOption.AllDirectories).Select(x => (x, (string)null, x[SAVED_PREFABS_FOLDER.Length..]))
                    .Union(m_modsTemplatesFolder.Values.SelectMany(x => Directory.GetFiles(x.rootFolder, $"*.{PREFAB_LAYOUT_EXTENSION}", SearchOption.AllDirectories).Select(y => (y, x.id, $"{x.name}: {y[x.rootFolder.Length..]}"))))
                    .ToArray();
            var errorsList = new Dictionary<string, LocalizedString>();
            for (int i = 0; i < files.Length; i++)
            {
                var fileItemFull = files[i];
                var modId = fileItemFull.Item2;
                string fileItem = fileItemFull.Item1;
                string displayName = fileItemFull.Item3;
                NotificationHelper.NotifyProgress(LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID, Mathf.RoundToInt(offsetPercentage + ((.11f + (.89f * ((i + 1f) / files.Length))) * totalStep)),
                        textI18n: $"{LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID}.loadingPrefabLayoutFile", argsText: new()
                        {
                            ["fileName"] = LocalizedString.Value(displayName),
                            ["progress"] = LocalizedString.Value($"{i}/{files.Length}")
                        });
                yield return 0;
                var prefabName = Path.GetFileName(fileItem)[..^(PREFAB_LAYOUT_EXTENSION.Length + 1)];
                if (!PrefabNameToIndex.TryGetValue(prefabName, out var idx))
                {
                    if (modId is null)
                    {
                        LogUtils.DoInfoLog($"No prefab loaded with name: {prefabName}, from custom folder: {fileItem.Replace(SAVED_PREFABS_FOLDER, "")}. This is harmless. Skipping...");
                        errorsList.Add(displayName, new LocalizedString("K45::WE.TEMPLATE_MANAGER[invalidPrefabName]", null, new Dictionary<string, ILocElement>()
                        {
                            ["fileName"] = LocalizedString.Value(displayName.Replace("\\", "\\\\")),
                            ["prefabName"] = LocalizedString.Value(prefabName)
                        }));
                    }
                    else
                    {

                        LogUtils.DoLog($"No prefab loaded with name: {prefabName}, but it's from a mod. This is harmless. Skipping...");
                    }
                    continue;
                }
                var tree = WETextDataXmlTree.FromXML(File.ReadAllText(fileItem));
                if (tree is null) continue;
                tree.self = new WETextDataXml { };

                var validationResults = CanBePrefabLayout(tree);
                if (validationResults == 0)
                {
                    if (modId is string effectiveModId)
                    {
                        if (!m_atlasesMapped.TryGetValue(effectiveModId, out var dictAtlases))
                        {
                            dictAtlases = m_atlasesMapped[effectiveModId] = new();
                        }
                        if (!m_fontsMapped.TryGetValue(effectiveModId, out var dictFonts))
                        {
                            dictFonts = m_fontsMapped[effectiveModId] = new();
                        }
                        tree.MapFontAndAtlases(effectiveModId, dictAtlases, dictFonts);
                        tree.ModSource = effectiveModId.TrimToNull();
                    }

                    if (PrefabTemplates.ContainsKey(idx))
                    {
                        PrefabTemplates[idx].MergeChildren(tree);
                    }
                    else
                    {
                        PrefabTemplates[idx] = tree;
                    }
                    if (BasicIMod.DebugMode) LogUtils.DoLog($"Loaded template for prefab: //{prefabName}// => {tree} from {fileItem[SAVED_PREFABS_FOLDER.Length..]}");
                }
                else if (modId != null)
                {
                    if (BasicIMod.DebugMode) LogUtils.DoLog($"An integration mod have a broken template for prefab '{prefabName}' (File at: '{fileItem}'). Contact the developer of the corrupted file to get assistance ({validationResults})");
                }
                else
                {
                    if (fileItem.StartsWith(SAVED_PREFABS_FOLDER))
                    {
                        errorsList.Add(displayName, new LocalizedString("K45::WE.TEMPLATE_MANAGER[invalidFileContent]", null, new Dictionary<string, ILocElement>()
                        {
                            ["fileName"] = LocalizedString.Value(displayName),
                            ["prefabName"] = LocalizedString.Value(prefabName)
                        }));
                        LogUtils.DoWarnLog($"Failed loding default template for prefab '{prefabName}', from custom folder: {fileItem.Replace(SAVED_PREFABS_FOLDER, "")}. Check previous lines at mod log to more information ({validationResults})");
                    }
                    else
                    {
                        LogUtils.DoWarnLog($"Failed loding default template for prefab '{prefabName}', from a mod located at: {fileItem}. Check previous lines at mod log to more information and contact author from that mod for support ({validationResults})");
                    }
                }
            }
            if (errorsList.Count > 0)
            {
                NotificationHelper.NotifyWithCallback(ERRORS_LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID, Colossal.PSI.Common.ProgressState.Warning, () =>
                {
                    var dialog2 = new MessageDialog(
                        LocalizedString.Id(NotificationHelper.GetModDefaultNotificationTitle(ERRORS_LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID)),
                        LocalizedString.Id("K45::WE.TEMPLATE_MANAGER[errorDialogHeader]"),
                        LocalizedString.Value(string.Join("\n", errorsList.Select(x => $"{x.Key}: {x.Value.Translate()}"))),
                        true,
                        LocalizedString.Id("Common.OK"),
                        LocalizedString.Id(BasicIMod.ModData.FixLocaleId(BasicIMod.ModData.GetOptionLabelLocaleID(nameof(BasicModData.GoToLogFolder))))
                        );
                    GameManager.instance.userInterface.appBindings.ShowMessageDialog(dialog2, (x) =>
                    {
                        switch (x)
                        {
                            case 2:
                                BasicIMod.ModData.GoToLogFolder = true;
                                break;
                        }
                        NotificationHelper.RemoveNotification(ERRORS_LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID);
                    });
                });
            }
            NotificationHelper.NotifyProgress(LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID, Mathf.RoundToInt(offsetPercentage + totalStep), textI18n: $"{LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID}.loadingComplete");
            EntityManager.AddComponent<WETemplateForPrefabDirty>(m_prefabsToMarkDirty);
        }
        #endregion
        #region City Templates
        public int CanBeTransformedToTemplate(Entity e)
        {
            if (!EntityManager.TryGetComponent<WETextDataMesh>(e, out var weData))
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
        public int CanBeTransformedToTemplate(WETextDataXmlTree treeStruct)
        {
            var weData = treeStruct.self;
            if (weData.textMesh is null && weData.imageMesh is null && weData.whiteMesh is null)
            {
                LogUtils.DoInfoLog($"Failed validation to transform to City Template: Only white textures, text and image items are allowed in a city template");
                return 2;
            }
            for (int i = 0; i < treeStruct.children?.Length; i++)
            {
                if (CanBeTransformedToTemplate(treeStruct.children[i]) != 0)
                {
                    LogUtils.DoInfoLog($"Failed validation to transform to City Template: Item #{i} failed validation");
                    return 3;
                }
            }
            return 0;
        }
        public bool SaveCityTemplate(string name, Entity e)
        {
            if (CanBeTransformedToTemplate(e) != 0)
            {
                LogUtils.DoInfoLog($"Failed validating layout '{name}': it failed while verifying if it could be transformed to template, check previous lines for details.");
                return false;
            }
            var templateEntity = WETextDataXmlTree.FromEntity(e, EntityManager);
            CommonSaveAsTemplate(name, templateEntity);
            if (BasicIMod.DebugMode) LogUtils.DoLog($"Saved {e} as WETextDataTree {templateEntity.Guid} @ {name}");
            return true;
        }
        public bool SaveCityTemplate(string name, WETextDataXmlTree templateEntity)
        {
            if (CanBeTransformedToTemplate(templateEntity) != 0)
            {
                LogUtils.DoInfoLog($"Failed validating layout '{name}': it failed while verifying if it could be transformed to template, check previous lines for details.");
                return false;
            }
            CommonSaveAsTemplate(name, templateEntity);
            if (BasicIMod.DebugMode) LogUtils.DoLog($"Saved {templateEntity.Guid} as WETextDataTree @ {name}");
            return true;
        }

        private void CommonSaveAsTemplate(string name, WETextDataXmlTree templateEntity)
        {
            if (RegisteredTemplates.ContainsKey(name))
            {
                RegisteredTemplates.Remove(name);
            }
            RegisteredTemplates.Add(name, templateEntity);
            m_templatesDirty = true;
        }

        public bool CityTemplateExists(string name) => RegisteredTemplates.ContainsKey(name);
        public Dictionary<string, string> ListCityTemplates()
        {
            var arr = RegisteredTemplates.Keys;
            var result = arr.ToArray().OrderBy(x => x).ToDictionary(x => x.ToString(), x => RegisteredTemplates[x].Guid.ToString());
            return result;
        }
        public unsafe int GetCityTemplateUsageCount(string name)
        {
            if (m_templateBasedEntities.IsEmptyIgnoreFilter || !RegisteredTemplates.TryGetValue(name, out var templateEntity)) return 0;
            var counterResult = 0;
            var job = new WEPlaceholcerTemplateUsageCount
            {
                m_templateToCheck = templateEntity.Guid,
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
            m_templatesDirty = true;
        }

        public void DeleteCityTemplate(string name)
        {
            if (name != null && CityTemplateExists(name))
            {
                RegisteredTemplates.Remove(name);
                m_templatesDirty = true;
            }
        }
        public void DuplicateCityTemplate(string srcName, string newName)
        {
            if (srcName == newName || srcName.TrimToNull() == null || newName.TrimToNull() == null || !CityTemplateExists(srcName)) return;
            if (RegisteredTemplates.ContainsKey(newName))
            {
            }
            RegisteredTemplates[newName] = RegisteredTemplates[srcName].Clone();
            m_templatesDirty = true;
        }
        #endregion

        #region Modules integration

        private readonly Dictionary<Assembly, ModFolder> integrationLoadableTemplatesFromMod = new();
        private readonly Dictionary<string, HashSet<string>> m_atlasesMapped = new();
        private readonly Dictionary<string, HashSet<string>> m_fontsMapped = new();
        private readonly Dictionary<string, Dictionary<string, string>> m_atlasesReplacements = new();
        private readonly Dictionary<string, Dictionary<string, string>> m_fontsReplacements = new();
        public ushort ModReplacementDataVersion { get; private set; } = 0;

        private Dictionary<string, (string name, string id, string rootFolder)> m_modsTemplatesFolder = new();

        public void RegisterModTemplatesForLoading(Assembly mainAssembly, string folderTemplatesSource)
        {
            var modData = ModManagementUtils.GetModDataFromMainAssembly(mainAssembly).asset;
            var modId = mainAssembly.GetName().Name;
            var modName = modData.mod.displayName;

            if (m_modsTemplatesFolder.TryGetValue(modId, out var folder) && folder.rootFolder == folderTemplatesSource) return;
            m_modsTemplatesFolder[modId] = (modName, modId, folderTemplatesSource);
            GameManager.instance.StartCoroutine(LoadTemplatesFromFolder(0, 100, modId));
        }

        public FixedString128Bytes[] GetTemplateAvailableKeys() => RegisteredTemplates.Keys.ToArray();

        internal void RegisterLoadableTemplatesFolder(Assembly mainAssembly, ModFolder fontFolder) { integrationLoadableTemplatesFromMod[mainAssembly] = fontFolder; }
        internal List<ModFolder> ListModsExtraFolders() => integrationLoadableTemplatesFromMod.Values.ToList();

        internal FixedString32Bytes GetFontFor(FixedString64Bytes originalFontName, ref bool haveChanges)
        {
            var strOriginal = originalFontName.ToString();
            if (!strOriginal.Contains(":")) return strOriginal;
            var decomposedName = strOriginal.Split(":", 2);
            haveChanges |= true;
            return m_fontsReplacements.TryGetValue(decomposedName[0], out var fontList) && fontList.TryGetValue(decomposedName[1], out var fontName)
                        ? (FixedString32Bytes)fontName
                        : default;
        }

        internal FixedString64Bytes GetAtlasFor(FixedString64Bytes originalAtlasName, ref bool haveChanges)
        {
            var strOriginal = originalAtlasName.ToString();
            if (!strOriginal.Contains(":")) return strOriginal;
            var decomposedName = strOriginal.Split(":", 2);
            haveChanges |= true;
            return m_atlasesReplacements.TryGetValue(decomposedName[0], out var atlasList) && atlasList.TryGetValue(decomposedName[1], out var atlasName)
                        ? atlasName
                        : strOriginal;
        }

        [XmlRoot("WEModReplacementData")]
        public class ModReplacementDataXml
        {
            [XmlElement("Mod")]
            public ModReplacementData[] Mods;
        }

        public class ModReplacementData
        {
            [XmlAttribute] public string modId;
            [XmlIgnore] public string displayName;
            public StringableXmlDictionary atlases;
            public StringableXmlDictionary fonts;

            public ModReplacementData() { }
            internal ModReplacementData(string modId, string displayName, Dictionary<string, string> atlases, Dictionary<string, string> fonts)
            {
                this.modId = modId;
                this.displayName = displayName;
                this.atlases = new(); this.atlases.AddRange(atlases);
                this.fonts = new(); this.fonts.AddRange(fonts);
            }
        }

        internal ModReplacementData[] GetModsReplacementData()
            => m_modsTemplatesFolder.Select(x => new ModReplacementData(
                    x.Key,
                    x.Value.name,
                    (m_atlasesReplacements.TryGetValue(x.Key, out var atlases) ? atlases : new()).Union((m_atlasesMapped.TryGetValue(x.Key, out var mappedAtlases) ? mappedAtlases : new()).ToDictionary(y => y, y => (string)null)).GroupBy(y => y.Key).ToDictionary(y => y.Key, y => y.First().Value),
                    (m_fontsReplacements.TryGetValue(x.Key, out var fonts) ? fonts : new()).Union((m_fontsMapped.TryGetValue(x.Key, out var mappedFonts) ? mappedFonts : new()).ToDictionary(y => y, y => (string)null)).GroupBy(y => y.Key).ToDictionary(y => y.Key, y => y.First().Value)
                )).ToArray();

        internal string SetModAtlasReplacement(string modId, string original, string target)
        {
            if (m_atlasesMapped.ContainsKey(modId))
            {
                if (!m_atlasesReplacements.TryGetValue(modId, out var atlases))
                {
                    atlases = m_atlasesReplacements[modId] = new();
                }
                if (m_atlasesMapped[modId].Contains(original))
                {
                    unchecked
                    {
                        ModReplacementDataVersion++;
                    }
                    if (target.TrimToNull() is null)
                    {
                        atlases.Remove(original);
                        return null;
                    }
                    return atlases[original] = target.TrimToNull() ?? original;
                }
            }
            return null;

        }

        internal string SetModFontReplacement(string modId, string original, string target)
        {
            if (m_fontsMapped.ContainsKey(modId))
            {
                if (!m_fontsReplacements.TryGetValue(modId, out var fonts))
                {
                    fonts = m_fontsReplacements[modId] = new();
                }
                if (m_fontsMapped[modId].Contains(original))
                {
                    unchecked
                    {
                        ModReplacementDataVersion++;
                    }
                    if (target.TrimToNull() is null)
                    {
                        fonts.Remove(original);
                        return null;
                    }
                    return fonts[original] = target.TrimToNull() ?? original;
                }
            }
            return null;
        }

        internal string SaveReplacementSettings(string fileName)
        {
            KFileUtils.EnsureFolderCreation(SAVED_MODREPLACEMENTS_FOLDER);
            var targetFileName = Path.Combine(SAVED_MODREPLACEMENTS_FOLDER, $"{KFileUtils.RemoveInvalidFilenameChars(fileName)}.{LAYOUT_REPLACEMENTS_EXTENSION}");
            var content = new ModReplacementDataXml
            {
                Mods = GetModsReplacementData()
            };
            File.WriteAllText(targetFileName, XmlUtils.DefaultXmlSerialize(content));
            return targetFileName;
        }

        internal bool CheckReplacementSettingFileExists(string fileName) => File.Exists(Path.Combine(SAVED_MODREPLACEMENTS_FOLDER, $"{KFileUtils.RemoveInvalidFilenameChars(fileName)}.{LAYOUT_REPLACEMENTS_EXTENSION}"));

        internal bool LoadReplacementSettings(string fullPath)
        {
            if (!File.Exists(fullPath)) return false;
            try
            {
                var content = XmlUtils.DefaultXmlDeserialize<ModReplacementDataXml>(File.ReadAllText(fullPath));
                m_atlasesReplacements.Clear();
                m_fontsReplacements.Clear();
                m_atlasesReplacements.AddRange(m_atlasesMapped.Keys.Select(x => (content.Mods.Where(y => y.modId == x).FirstOrDefault()) ?? new() { modId = x }).ToDictionary(x => x.modId, x => x.atlases ?? new()));
                m_fontsReplacements.AddRange(m_fontsMapped.Keys.Select(x => (content.Mods.Where(y => y.modId == x).FirstOrDefault()) ?? new() { modId = x }).ToDictionary(x => x.modId, x => x.fonts ?? new())); ;
                unchecked
                {
                    ModReplacementDataVersion++;
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        #endregion

        [BurstCompile]
        private unsafe struct WEPlaceholcerTemplateUsageCount : IJobChunk
        {
            public Colossal.Hash128 m_templateToCheck;
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