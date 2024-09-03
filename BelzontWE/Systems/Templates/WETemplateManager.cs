﻿using Belzont.Interfaces;
using Belzont.Utils;
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
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public partial class WETemplateManager : SystemBase
    {
        public const string SIMPLE_LAYOUT_EXTENSION = "welayout.xml";
        public const string PREFAB_LAYOUT_EXTENSION = "wedefault.xml";
        public static readonly string SAVED_PREFABS_FOLDER = Path.Combine(BasicIMod.ModSettingsRootFolder, "prefabs");
        public static WETemplateManager Instance { get; private set; }

        public const int CURRENT_VERSION = 0;

        private Dictionary<FixedString128Bytes, WETextDataXmlTree> RegisteredTemplates;
        private PrefabSystem m_prefabSystem;
        private EndFrameBarrier m_endFrameBarrier;
        private EntityQuery m_templateBasedEntities;
        private EntityQuery m_uncheckedWePrefabLayoutQuery;
        private EntityQuery m_dirtyWePrefabLayoutQuery;
        private EntityQuery m_prefabsToMarkDirty;
        private EntityQuery m_prefabsDataToSerialize;
        private Dictionary<long, WETextDataXmlTree> PrefabTemplates;
        private readonly Queue<Action<EntityCommandBuffer>> m_executionQueue = new();
        private bool m_templatesDirty;

        public FixedString128Bytes[] GetTemplateAvailableKeys() => RegisteredTemplates.Keys.ToArray();

        public WETextDataXmlTree this[FixedString128Bytes idx]
        {
            get
            {
                var value = RegisteredTemplates.TryGetValue(idx, out var tree) ? tree : default;
                if (BasicIMod.DebugMode) LogUtils.DoLog($"Loaded {value} @ {idx}");
                return value;
            }
        }

        protected override void OnCreate()
        {
            Instance = this;
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
        }

        protected override void OnDestroy()
        {
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
                EntityManager.AddComponent<WEWaitingRenderingPlaceholder>(m_templateBasedEntities);
                m_templatesDirty = false;
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
                if ((self.textType < 0 || self.textType > WESimulationTextType.Placeholder) && self.textType != WESimulationTextType.WhiteTexture)
                {
                    LogUtils.DoInfoLog($"Failed validation to transform to Prefab Default: All children must have type 'Placeholder', 'WhiteTexture', 'Image' or 'Text'.");
                    return 4;
                };
                if (self.textType == WESimulationTextType.Placeholder && children?.Length > 0)
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
            EntityManager.AddComponent<WETemplateForPrefabDirty>(m_prefabsToMarkDirty);
        }

        private IEnumerator LoadTemplatesFromFolder(int offsetPercentage, float totalStep)
        {
            var currentValues = PrefabTemplates.Keys.ToArray();
            NotificationHelper.NotifyProgress(LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID, Mathf.RoundToInt(offsetPercentage + (.01f * totalStep)), textI18n: $"{LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID}.erasingCachedLayouts");
            yield return 0;
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
            NotificationHelper.NotifyProgress(LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID, Mathf.RoundToInt(offsetPercentage + (.11f * totalStep)), textI18n: $"{LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID}.searchingForFiles");
            yield return 0;
            var files = Directory.GetFiles(SAVED_PREFABS_FOLDER, $"*.{PREFAB_LAYOUT_EXTENSION}", SearchOption.AllDirectories);
            var errorsList = new Dictionary<string, LocalizedString>();
            for (int i = 0; i < files.Length; i++)
            {
                string fileItem = files[i];
                string relativePath = fileItem[SAVED_PREFABS_FOLDER.Length..];
                NotificationHelper.NotifyProgress(LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID, Mathf.RoundToInt(offsetPercentage + ((.11f + (.89f * ((i + 1f) / files.Length))) * totalStep)),
                        textI18n: $"{LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID}.loadingPrefabLayoutFile", argsText: new()
                        {
                            ["fileName"] = LocalizedString.Value(relativePath),
                            ["progress"] = LocalizedString.Value($"{i}/{files.Length}")
                        });
                yield return 0;
                var prefabName = Path.GetFileName(fileItem)[..^(PREFAB_LAYOUT_EXTENSION.Length + 1)];
                if (!PrefabNameToIndex.TryGetValue(prefabName, out var idx))
                {
                    LogUtils.DoWarnLog($"No prefab loaded with name: {prefabName}. Skipping...");
                    errorsList.Add(relativePath, new LocalizedString("K45::WE.TEMPLATE_MANAGER[invalidPrefabName]", null, new Dictionary<string, ILocElement>()
                    {
                        ["fileName"] = LocalizedString.Value(relativePath),
                        ["prefabName"] = LocalizedString.Value(prefabName)
                    }));
                    continue;
                }
                var tree = WETextDataXmlTree.FromXML(File.ReadAllText(fileItem));
                if (tree is null) continue;
                tree.self = new WETextDataXml
                {
                    textType = WESimulationTextType.Archetype
                };
                var validationResults = CanBePrefabLayout(tree);
                if (validationResults == 0)
                {

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
                else
                {
                    errorsList.Add(relativePath, new LocalizedString("K45::WE.TEMPLATE_MANAGER[invalidFileContent]", null, new Dictionary<string, ILocElement>()
                    {
                        ["fileName"] = LocalizedString.Value(relativePath),
                        ["prefabName"] = LocalizedString.Value(prefabName)
                    }));
                    LogUtils.DoWarnLog($"Failed loding default template for prefab '{prefabName}'. Check previous lines at mod log to more information ({validationResults})");
                }
            }
            if (errorsList.Count > 0)
            {
                NotificationHelper.NotifyWithCallback(ERRORS_LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID, Colossal.PSI.Common.ProgressState.Warning, () =>
                {
                    var dialog2 = new MessageDialogWithDetails(
                        LocalizedString.Id(NotificationHelper.GetModDefaultNotificationTitle(ERRORS_LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID)),
                        LocalizedString.Id("K45::WE.TEMPLATE_MANAGER[errorDIalogHeader]"),
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
        }
        #endregion
        #region City Templates
        public int CanBeTransformedToTemplate(Entity e)
        {
            if (!EntityManager.TryGetComponent<WETextDataMain>(e, out var weData))
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
            if (weData.textType != WESimulationTextType.Text && weData.textType != WESimulationTextType.Image && weData.textType != WESimulationTextType.WhiteTexture)
            {
                LogUtils.DoInfoLog($"Failed validation to transform to City Template: Only white textures, text and image items are allowed in a city template");
                return 2;
            }
            for (int i = 0; i < treeStruct.children.Length; i++)
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
            if (RegisteredTemplates.TryGetValue(name, out var obsoleteTemplate))
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