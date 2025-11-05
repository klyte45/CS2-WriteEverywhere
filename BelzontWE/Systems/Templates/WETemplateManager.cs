using Belzont.Interfaces;
using Belzont.Serialization;
using Belzont.Utils;
using BelzontWE.Sprites;
using BelzontWE.Utils;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game;
using Game.Common;
using Game.Objects;
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
using Unity.Mathematics;
using UnityEngine;


#if BURST
#endif

namespace BelzontWE
{
    public partial class WETemplateManager : SystemBase, IBelzontSerializableSingleton<WETemplateManager>
    {
        public const string SIMPLE_LAYOUT_EXTENSION = "welayout.xml";
        public const string PREFAB_LAYOUT_EXTENSION = "wedefault.xml";
        public static string SAVED_PREFABS_FOLDER => Path.Combine(BasicIMod.ModSettingsRootFolder, "layouts");
        public static string SAVED_MODREPLACEMENTS_FOLDER => Path.Combine(BasicIMod.ModSettingsRootFolder, "modReplacementProfiles");
        public const string LAYOUT_REPLACEMENTS_EXTENSION = "weprefabreplace.xml";
        public static WETemplateManager Instance { get; private set; }

        public const int CURRENT_VERSION = 3;

        private Dictionary<FixedString128Bytes, WETextDataXmlTree> RegisteredTemplates;
        private PrefabSystem m_prefabSystem;
        private EndFrameBarrier m_endFrameBarrier;
        private EntityQuery m_templateBasedEntities;
        private EntityQuery m_uncheckedWePrefabLayoutQuery;
        private EntityQuery m_dirtyWePrefabLayoutQuery;
        private EntityQuery m_dirtyInstancingWeQuery;
        private EntityQuery m_prefabsToMarkDirty;
        private EntityQuery m_prefabsDataToSerialize;
        private EntityQuery m_entitiesToBeUpdatedInMain;
        private EntityQuery m_prefabArchetypesToBeUpdatedInMain;
        private EntityQuery m_textDataDirtyQuery;
        private Dictionary<long, WETextDataXmlTree> PrefabTemplates;
        private readonly Queue<Action<EntityCommandBuffer>> m_executionQueue = new();
        private bool m_templatesDirty;
        public Entity PrefabUpdateSource { get; private set; } = Entity.Null;

        private Dictionary<string, Dictionary<string, WETextDataXmlTree>> ModsSubTemplates { get; } = new();

        public WETextDataXmlTree this[FixedString128Bytes idx]
        {
            get
            {
                if (!RegisteredTemplates.TryGetValue(idx, out var tree)
                    && idx.ToString().Split(":", 2) is string[] nameKv
                    && nameKv.Length == 2
                    && ModsSubTemplates.TryGetValue(nameKv[0], out var modTemplates)
                    && modTemplates.TryGetValue(nameKv[1], out var modtemplate)
                )
                {
                    tree = modtemplate;
                }
                if (BasicIMod.DebugMode) LogUtils.DoLog($"Loaded {tree} @ {idx}");
                return tree;
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
            m_atlasesReplacements.Clear();
            m_fontsReplacements.Clear();
            m_subtemplatesReplacements.Clear();
            m_meshesReplacements.Clear();
            if (version >= 1)
            {
                reader.Read(out string atlasesReplacementData);
                reader.Read(out string fontsReplacementData);
                m_atlasesReplacements.AddRange(DeserializeReplacementData(atlasesReplacementData));
                m_fontsReplacements.AddRange(DeserializeReplacementData(fontsReplacementData));
            }
            if (version >= 2)
            {
                reader.Read(out string subtemplatesReplacements);
                m_subtemplatesReplacements.AddRange(DeserializeReplacementData(subtemplatesReplacements));
            }
            if (version >= 3)
            {
                reader.Read(out string meshesReplacementData);
                m_meshesReplacements.AddRange(DeserializeReplacementData(meshesReplacementData));
            }
            SpritesAndLayoutsDataVersion = 3;
            m_templatesDirty = true;
        }

        private const string L1_ITEM_SEPARATOR = "|";
        private const string L1_KV_SEPARATOR = "→";
        private const string L2_ITEM_SEPARATOR = "∫";
        private const string L2_KV_SEPARATOR = "↓";

        private Dictionary<string, Dictionary<string, string>> DeserializeReplacementData(string data)
        {
            if (BasicIMod.DebugMode)
            {
                LogUtils.DoLog($"Deserializing replacement data: {data}");
            }
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
                .Select(x => (x.Key, x.Value.Where(x => x.Value.TrimToNull() != null).ToArray()))
                .Where(x => x.Item2.Length > 0)
                .Select(x => $"{x.Key}{L1_KV_SEPARATOR}{string.Join(L2_ITEM_SEPARATOR, x.Item2.Select(y => $"{y.Key}{L2_KV_SEPARATOR}{y.Value}"))}"));
            string fontsReplacementData = string.Join('|', m_fontsReplacements
                .Select(x => (x.Key, x.Value.Where(x => x.Value.TrimToNull() != null).ToArray()))
                .Where(x => x.Item2.Length > 0)
                .Select(x => $"{x.Key}{L1_KV_SEPARATOR}{string.Join(L2_ITEM_SEPARATOR, x.Item2.Select(y => $"{y.Key}{L2_KV_SEPARATOR}{y.Value}"))}"));
            string subtemplatesReplacements = string.Join('|', m_subtemplatesReplacements
                .Select(x => (x.Key, x.Value.Where(x => x.Value.TrimToNull() != null).ToArray()))
                .Where(x => x.Item2.Length > 0)
                .Select(x => $"{x.Key}{L1_KV_SEPARATOR}{string.Join(L2_ITEM_SEPARATOR, x.Item2.Select(y => $"{y.Key}{L2_KV_SEPARATOR}{y.Value}"))}"));
            writer.Write(atlasesReplacementData);
            writer.Write(fontsReplacementData);
            writer.Write(subtemplatesReplacements);
            string meshesReplacements = string.Join('|', m_meshesReplacements
                .Select(x => (x.Key, x.Value.Where(x => x.Value.TrimToNull() != null).ToArray()))
                .Where(x => x.Item2.Length > 0)
                .Select(x => $"{x.Key}{L1_KV_SEPARATOR}{string.Join(L2_ITEM_SEPARATOR, x.Item2.Select(y => $"{y.Key}{L2_KV_SEPARATOR}{y.Value}"))}"));
            writer.Write(meshesReplacements);
            if (BasicIMod.DebugMode)
            {
                LogUtils.DoLog($"ATLASES:\n{atlasesReplacementData.Replace(L1_ITEM_SEPARATOR, "\n").Replace(L1_KV_SEPARATOR, "\n\t").Replace(L2_ITEM_SEPARATOR, "\n\t").Replace(L2_KV_SEPARATOR, "\t=>\t")}");
                LogUtils.DoLog($"FONTS:\n{fontsReplacementData.Replace(L1_ITEM_SEPARATOR, "\n").Replace(L1_KV_SEPARATOR, "\n\t").Replace(L2_ITEM_SEPARATOR, "\n\t").Replace(L2_KV_SEPARATOR, "\t=>\t")}");
                LogUtils.DoLog($"SUBTEMPLATES:\n{subtemplatesReplacements.Replace(L1_ITEM_SEPARATOR, "\n").Replace(L1_KV_SEPARATOR, "\n\t").Replace(L2_ITEM_SEPARATOR, "\n\t").Replace(L2_KV_SEPARATOR, "\t=>\t")}");
                LogUtils.DoLog($"MESHES:\n{atlasesReplacementData.Replace(L1_ITEM_SEPARATOR, "\n").Replace(L1_KV_SEPARATOR, "\n\t").Replace(L2_ITEM_SEPARATOR, "\n\t").Replace(L2_KV_SEPARATOR, "\t=>\t")}");
            }
        }
        protected override void OnCreate()
        {
            Instance = this;
            KFileUtils.EnsureFolderCreation(SAVED_MODREPLACEMENTS_FOLDER);
            RegisteredTemplates = new();
            PrefabTemplates = new();
            m_prefabSystem = World.GetExistingSystemManaged<PrefabSystem>();
            m_endFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
            PrefabUpdateSource = EntityManager.CreateEntity();
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
                            ComponentType.ReadOnly<WETemplateDirtyInstancing>(),
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
            WEAtlasesLibrary.GetWhiteTextureBRI();
        }

        protected override void OnDestroy()
        {
        }

        private Coroutine m_updatingEntitiesOnMain;

        internal void UpdatePrefabArchetypes(NativeArray<ArchetypeChunk> chunks)
        {
            var m_TextDataLkp = GetComponentLookup<WETextDataMain>();
            var m_prefabDataLkp = GetComponentLookup<PrefabData>();
            var m_prefabRefHdl = GetComponentTypeHandle<PrefabRef>();
            var m_subRefLkp = GetBufferLookup<WESubTextRef>();
            var m_prefabEmptyLkp = GetComponentLookup<WETemplateForPrefabEmpty>();
            var globalCounter = 0;
            for (int h = 0; h < chunks.Length; h++)
            {
                var chunk = chunks[h];
                using var entities = chunk.GetNativeArray(GetEntityTypeHandle());
                using var prefabRefs = chunk.GetNativeArray(ref m_prefabRefHdl);
                for (int i = 0; i < entities.Length; i++, globalCounter++)
                {
                    if (globalCounter >= 10_000)
                    {
                        m_updatingEntitiesOnMain = null;
                        return;
                    }
                    var e = entities[i];
                    var prefabRef = prefabRefs[i];

                    EntityCommandBuffer cmd = m_endFrameBarrier.CreateCommandBuffer();
                    cmd.SetComponentEnabled<WETemplateForPrefabToRunOnMain>(e, false);
                    if (m_prefabDataLkp.TryGetComponent(prefabRef.m_Prefab, out var prefabData))
                    {
                        if (PrefabTemplates.TryGetValue(prefabData.m_Index, out var newTemplate))
                        {
                            var guid = newTemplate.Guid;
                            var childEntity = WELayoutUtility.DoCreateLayoutItemCmdBuffer(true, newTemplate.ModSource, newTemplate, e, Entity.Null, ref m_TextDataLkp, ref m_subRefLkp, cmd, WELayoutUtility.ParentEntityMode.TARGET_IS_SELF_FOR_PARENT);
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
        internal void UpdateLayouts(NativeArray<ArchetypeChunk> chunks)
        {
            var m_MainDataLkp = GetComponentLookup<WETextDataMain>();
            var m_DataTransformLkp = GetComponentLookup<WETextDataTransform>();
            var m_subRefLkp = GetBufferLookup<WESubTextRef>();
            var toBeProcessedDataHdl = GetComponentTypeHandle<WEPlaceholderToBeProcessedInMain>();
            var globalCounter = 0;
            for (int h = 0; h < chunks.Length; h++)
            {
                var chunk = chunks[h];
                using var entities = chunk.GetNativeArray(GetEntityTypeHandle());
                using var dataToBeProcessedArray = chunk.GetNativeArray(ref toBeProcessedDataHdl);
                for (int i = 0; i < entities.Length; i++, globalCounter++)
                {
                    if (globalCounter >= 10_000)
                    {
                        m_updatingEntitiesOnMain = null;
                        return;
                    }
                    var e = entities[i];
                    EntityCommandBuffer cmd = m_endFrameBarrier.CreateCommandBuffer();
                    var buff = cmd.SetBuffer<WETemplateUpdater>(e);

                    var dataToBeProcessed = dataToBeProcessedArray[i];
                    m_DataTransformLkp.TryGetComponent(e, out var transformData);
                    bool hasTemplate = TryGetTargetTemplate(dataToBeProcessed.layoutName, out WETextDataXmlTree targetTemplate);

                    for (int j = 0; j < buff.Length; j++)
                    {
                        cmd.DestroyEntity(buff[j].childEntity);
                    }
                    buff.Clear();
                    if (hasTemplate)
                    {
                        targetTemplate = targetTemplate.Clone();

                        var targetSize = transformData.InstanceCountFn.EffectiveValue < 0 ? math.clamp(transformData.ArrayInstancing.x * transformData.ArrayInstancing.y * transformData.ArrayInstancing.z, 1, 256) : math.min(256, (uint)transformData.InstanceCountFn.EffectiveValue);
                        if (targetSize == 0) goto end;
                        var instancingCount = (uint3)math.min(transformData.InstanceCountByAxisOrder, math.ceil(targetSize / new float3(1, transformData.InstanceCountByAxisOrder[0], transformData.InstanceCountByAxisOrder[0] * transformData.InstanceCountByAxisOrder[1])));
                        var spacingOffsets = transformData.SpacingByAxisOrder;
                        var totalArea = (transformData.ArrayInstancing - 1) * transformData.arrayInstancingGapMeters;

                        var effectivePivot = transformData.PivotAsFloat3 - (math.sign(totalArea.xyz) / 2) - .5f;

                        var pivotOffset = effectivePivot * math.abs(totalArea);
                        var alignmentByAxisOrder = transformData.AlignmentByAxisOrder;

                        var spacingO = spacingOffsets[2];
                        GetSpacingAndOffset(targetSize, instancingCount.z, instancingCount.y * instancingCount.x, alignmentByAxisOrder.o, ref spacingO, out float3 offsetO);
                        for (int o = 0; o < instancingCount.z; o++)
                        {
                            var spacingN = spacingOffsets[1];
                            GetSpacingAndOffset(targetSize - (uint)buff.Length, instancingCount.y, instancingCount.x, alignmentByAxisOrder.n, ref spacingN, out float3 offsetN);
                            for (int n = 0; n < instancingCount.y; n++)
                            {
                                var spacingM = spacingOffsets[0];
                                GetSpacingAndOffset(targetSize - (uint)buff.Length, instancingCount.x, 1, alignmentByAxisOrder.m, ref spacingM, out float3 offsetM);
                                var totalOffset = pivotOffset + offsetM + offsetN + offsetO;
                                for (int m = 0; m < instancingCount.x; m++)
                                {
                                    targetTemplate.self.transform.offsetPosition = (Vector3Xml)(Vector3)(totalOffset + (m * spacingM) + (n * spacingN) + (o * spacingO));
                                    targetTemplate.self.transform.pivot = transformData.pivot;

                                    var updater = new WETemplateUpdater()
                                    {
                                        templateEntity = targetTemplate.Guid,
                                        childEntity = WELayoutUtility.DoCreateLayoutItemCmdBuffer(true, targetTemplate.ModSource, targetTemplate, e, Entity.Null, ref m_MainDataLkp, ref m_subRefLkp, cmd, WELayoutUtility.ParentEntityMode.TARGET_IS_SELF_PARENT_HAS_TARGET)
                                    };

                                    buff.Add(updater);
                                    globalCounter++;
                                    if (buff.Length >= targetSize) goto end;
                                }
                            }
                        }
                    }
                end:
                    cmd.RemoveComponent<WEPlaceholderToBeProcessedInMain>(e);
                }
            }
        }

        private bool TryGetTargetTemplate(FixedString128Bytes layoutName, out WETextDataXmlTree targetTemplate)
        {
            targetTemplate = null;
            return layoutName.ToString().Split(":", 2) is string[] modEntryName && modEntryName.Length == 2
                                      ? ModsSubTemplates.TryGetValue(modEntryName[0], out var modTemplates) && modTemplates.TryGetValue(modEntryName[1], out targetTemplate)
                                      : RegisteredTemplates.TryGetValue(layoutName, out targetTemplate);
        }

        private static void GetSpacingAndOffset(uint remaining, uint rowCount, uint rowCapacity, WEPlacementAlignment axisAlignment, ref float3 spacing, out float3 offset)
        {
            offset = float3.zero;
            uint capacity = rowCapacity * (rowCount - 1);
            if (remaining <= capacity && axisAlignment != WEPlacementAlignment.Left)
            {
                var totalWidth = (rowCount - 1) * spacing;
                var effectiveRowsCount = math.ceil(remaining / rowCapacity);
                var effectiveWidth = (effectiveRowsCount - 1) * spacing;
                switch (axisAlignment)
                {
                    case WEPlacementAlignment.Center:
                        offset = (totalWidth - effectiveWidth) / 2;
                        break;
                    case WEPlacementAlignment.Right:
                        offset = totalWidth - effectiveWidth;
                        break;
                    case WEPlacementAlignment.Justified:
                        spacing = effectiveRowsCount == 1 ? 0 : totalWidth / (effectiveRowsCount - 1);
                        break;
                }
            }
        }

        public void EnqueueToBeDestructed(Material m)
        {
            m_executionQueue.Enqueue((x) => GameObject.Destroy(m));
        }

        protected override void OnUpdate()
        {
            if (GameManager.instance.isGameLoading || IsLoadingLayouts || !WriteEverywhereCS2Mod.IsInitializationComplete) return;

            // Process execution queue for UI actions
            if (m_executionQueue.Count > 0)
            {
                var cmdBuffer = m_endFrameBarrier.CreateCommandBuffer();
                while (m_executionQueue.TryDequeue(out var nextAction))
                {
                    nextAction?.Invoke(cmdBuffer);
                }
            }

            // Update prefab index dictionary (file I/O)
            UpdatePrefabIndexDictionary();

            // Note: Entity processing has been moved to WETemplateUpdateSystem
            Dependency.Complete();
        }

        public JobHandle SetDefaults(Context context)
        {
            RegisteredTemplates.Clear();
            m_atlasesReplacements.Clear();
            m_fontsReplacements.Clear();
            SpritesAndLayoutsDataVersion = 2;
            return Dependency;
        }

        #region Prefab Layout
        public void MarkPrefabsDirty() => isPrefabListDirty = true;
        public void MarkTemplatesDirty() => m_templatesDirty = true;
        private readonly Dictionary<string, HashSet<long>> PrefabNameToIndex = new();
        private Dictionary<(string prefabName, int meshIdx), ObjectState> MeshesHidden = new();
        private bool isPrefabListDirty = true;

        public int CanBePrefabLayout(WETextDataXmlTree data) => World.GetExistingSystemManaged<WETemplateQuerySystem>().CanBePrefabLayout(data);
        public int CanBePrefabLayout(WETextDataXmlTree data, bool isRoot) => World.GetExistingSystemManaged<WETemplateQuerySystem>().CanBePrefabLayout(data, isRoot);
        public int CanBePrefabLayout(WESelflessTextDataTree data) => World.GetExistingSystemManaged<WETemplateQuerySystem>().CanBePrefabLayout(data);
        public bool IsLoadingLayouts => LoadingPrefabLayoutsCoroutine != null;
        private Coroutine LoadingPrefabLayoutsCoroutine;
        private const string LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID = "loadingPrefabTemplates";
        private const string LOADING_SUBTEMPLATES_NOTIFICATION_ID = "loadingModSubtemplates";
        private const string ERRORS_LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID = "errorLoadingPrefabTemplates";
        private const string ERRORS_LOADING_SUBTEMPLATES_NOTIFICATION_ID = "errorLoadingModSubtemplates";
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
                if (!PrefabNameToIndex.ContainsKey(prefab.name)) PrefabNameToIndex[prefab.name] = new();
                PrefabNameToIndex[prefab.name].Add(data.m_Index);
                if (EntityManager.TryGetBuffer<PlaceholderObjectElement>(entities[prefab], true, out var buff))
                {
                    for (int j = 0; j < buff.Length; j++)
                    {
                        var otherData = EntityManager.GetComponentData<PrefabData>(buff[j].m_Object);
                        PrefabNameToIndex[prefab.name].Add(otherData.m_Index);
                    }
                }
            }
            NotificationHelper.NotifyProgress(LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID, 20, textI18n: $"{LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID}.prefabIndexesLoaded");
            yield return LoadTemplatesFromFolder(20, 80f);
            LoadingPrefabLayoutsCoroutine = null;
        }

        private IEnumerator LoadTemplatesFromFolder(int offsetPercentage, float totalStepFull, string modName = null)
        {
            var prefabs = PrefabSystemOverrides.LoadedPrefabBaseList(m_prefabSystem);
            var prefabsToUpdate = new List<PrefabBase>();
            var totalStepPrefabTemplates = modName is null ? totalStepFull : totalStepFull * .7f;
            KFileUtils.EnsureFolderCreation(SAVED_PREFABS_FOLDER);
            var currentValues = PrefabTemplates.Keys.ToArray();
            NotificationHelper.NotifyProgress(LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID, Mathf.RoundToInt(offsetPercentage + (.01f * totalStepPrefabTemplates)), textI18n: $"{LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID}.erasingCachedLayouts");
            yield return 0;
            if (modName is null)
            {
                for (int i = 0; i < currentValues.Length; i++)
                {
                    PrefabTemplates.Remove(currentValues[i]);
                    if (i % 3 == 0)
                    {
                        NotificationHelper.NotifyProgress(LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID, Mathf.RoundToInt(offsetPercentage + ((.01f + (.09f * ((i + 1f) / currentValues.Length))) * totalStepPrefabTemplates)),
                            textI18n: $"{LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID}.disposedOldLayout", argsText: new()
                            {
                                ["progress"] = LocalizedString.Value($"{i}/{currentValues.Length}")
                            });
                        yield return 0;
                    }
                }
                PrefabTemplates.Clear();
                foreach (var x in MeshesHidden)
                {
                    foreach (var y in PrefabNameToIndex[x.Key.prefabName])
                    {
                        switch (prefabs[(int)y])
                        {
                            case ObjectGeometryPrefab ogp:
                                if (x.Key.meshIdx < ogp.m_Meshes.Length)
                                {
                                    ogp.m_Meshes[x.Key.meshIdx].m_RequireState = x.Value;
                                }
                                prefabsToUpdate.Add(ogp);
                                break;
                        }
                    }
                }
                MeshesHidden.Clear();
            }
            NotificationHelper.NotifyProgress(LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID, Mathf.RoundToInt(offsetPercentage + (.11f * totalStepPrefabTemplates)), textI18n: $"{LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID}.searchingForFiles");
            yield return 0;
            var files = modName != null
                ? Directory.GetFiles(m_modsTemplatesFolder[modName].rootFolder, $"*.{PREFAB_LAYOUT_EXTENSION}", SearchOption.AllDirectories).Select(y => (y, m_modsTemplatesFolder[modName].id, $"{m_modsTemplatesFolder[modName].name}: {y[m_modsTemplatesFolder[modName].rootFolder.Length..]}")).ToArray()
                : Directory.GetFiles(SAVED_PREFABS_FOLDER, $"*.{PREFAB_LAYOUT_EXTENSION}", SearchOption.AllDirectories).Select(x => (x, (string)null, x[SAVED_PREFABS_FOLDER.Length..]))
                    .Union(m_modsTemplatesFolder.Values.SelectMany(x => Directory.GetFiles(x.rootFolder, $"*.{PREFAB_LAYOUT_EXTENSION}", SearchOption.AllDirectories).Select(y => (y, x.id, $"{x.name}: {y[x.rootFolder.Length..]}"))))
                    .ToArray();
            var errorsList = new Dictionary<string, LocalizedString>();
            for (int i = 0; i < files.Length; i++)
            {
                yield return LoadPrefabFileTemplate(offsetPercentage, totalStepPrefabTemplates, files, errorsList, i);
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
            var meshesToHide = MeshesHidden.Keys.ToArray();
            foreach (var key in meshesToHide)
            {
                if (MeshesHidden[key] != 0) continue;
                foreach (var prefabIdx in PrefabNameToIndex[key.prefabName])
                {
                    switch (prefabs[(int)prefabIdx])
                    {
                        case ObjectGeometryPrefab ogp:
                            if (key.meshIdx < ogp.m_Meshes.Length)
                            {
                                MeshesHidden[key] = ogp.m_Meshes[key.meshIdx].m_RequireState;
                                ogp.m_Meshes[key.meshIdx].m_RequireState = ObjectState.Outline;
                            }

                            prefabsToUpdate.Add(ogp);
                            break;
                    }
                }
            }

            foreach (var prefab in prefabsToUpdate)
            {
                m_prefabSystem.UpdatePrefab(prefab, PrefabUpdateSource);
            }

            NotificationHelper.NotifyProgress(LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID, Mathf.RoundToInt(offsetPercentage + totalStepPrefabTemplates), textI18n: $"{LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID}.loadingComplete");
            new WEAddAndEnableComponentJob<WETemplateForPrefabDirty>
            {
                m_EntityType = GetEntityTypeHandle(),
                m_CommandBuffer = m_endFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
            }.ScheduleParallel(m_prefabsToMarkDirty, Dependency).Complete();
        }


        private IEnumerator LoadPrefabFileTemplate(int offsetPercentage, float totalStep, (string, string, string)[] files, Dictionary<string, LocalizedString> errorsList, int i)
        {
            var fileItemFull = files[i];
            var fileItem = fileItemFull.Item1;
            var modId = fileItemFull.Item2;
            var displayName = fileItemFull.Item3;
            NotificationHelper.NotifyProgress(LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID, Mathf.RoundToInt(offsetPercentage + ((.11f + (.89f * ((i + 1f) / files.Length))) * totalStep)),
                    textI18n: $"{LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID}.loadingPrefabLayoutFile", argsText: new()
                    {
                        ["fileName"] = LocalizedString.Value(displayName),
                        ["progress"] = LocalizedString.Value($"{i}/{files.Length}")
                    });
            yield return 0;
            var prefabName = Path.GetFileName(fileItem)[..^(PREFAB_LAYOUT_EXTENSION.Length + 1)];
            if (!PrefabNameToIndex.TryGetValue(prefabName, out var idxArray))
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

                    if (BasicIMod.DebugMode) LogUtils.DoLog($"No prefab loaded with name: {prefabName}, but it's from a mod. This is harmless. Skipping...");
                }
                yield break;
            }
            var tree = WETextDataXmlTree.FromXML(File.ReadAllText(fileItem));
            if (tree is null) yield break;
            tree.self = new WETextDataXml { };

            var validationResults = CanBePrefabLayout(tree);
            if (validationResults == 0)
            {
                if (modId is string effectiveModId)
                {
                    ExtractReplaceableContent(tree, effectiveModId);
                }
                foreach (var idx in idxArray)
                {
                    if (PrefabTemplates.ContainsKey(idx))
                    {
                        PrefabTemplates[idx].MergeChildren(tree);
                    }
                    else
                    {
                        PrefabTemplates[idx] = tree;
                    }
                }
                if (tree.MeshesToHide?.Length > 0)
                {
                    foreach (var x in tree.MeshesToHide)
                    {
                        if (!MeshesHidden.ContainsKey((prefabName, x)))
                        {
                            MeshesHidden[(prefabName, x)] = default;
                        }
                    }
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

        private void ExtractReplaceableContent(WETextDataXmlTree tree, string effectiveModId)
        {
            if (!m_atlasesMapped.TryGetValue(effectiveModId, out var dictAtlases))
            {
                dictAtlases = m_atlasesMapped[effectiveModId] = new();
            }
            if (!m_fontsMapped.TryGetValue(effectiveModId, out var dictFonts))
            {
                dictFonts = m_fontsMapped[effectiveModId] = new();
            }
            if (!m_subtemplatesMapped.TryGetValue(effectiveModId, out var dictSubTemplates))
            {
                dictSubTemplates = m_subtemplatesMapped[effectiveModId] = new();
            }
            if (!m_meshesMapped.TryGetValue(effectiveModId, out var dictMeshes))
            {
                dictMeshes = m_meshesMapped[effectiveModId] = new();
            }
            tree.MapFontAtlasesTemplates(effectiveModId, dictAtlases, dictFonts, dictSubTemplates, dictMeshes);
            tree.ModSource = effectiveModId.TrimToNull();
        }
        #endregion
        #region Mod SubTemplates

        private Coroutine reloadingSubtemplatesCoroutine;
        public void ReloadSubtemplates()
        {
            if (reloadingSubtemplatesCoroutine != null) return;
            reloadingSubtemplatesCoroutine = GameManager.instance.StartCoroutine(LoadModSubtemplates_Coroutine());
        }


        private IEnumerator LoadModSubtemplates_Coroutine()
        {
            yield return 0;
            var mods = ModsSubTemplates.Keys.ToArray();
            if (mods.Length == 0) yield break;
            var eachItemPart = 1f / mods.Length;
            for (int i = 0; i < mods.Length; i++)
            {
                string modId = mods[i];
                GameManager.instance.StartCoroutine(LoadModSubtemplates_Item(0, 100, modId, true));
            }
            m_templatesDirty = true;
            reloadingSubtemplatesCoroutine = null;
        }


        private IEnumerator LoadModSubtemplates_Item(float offsetPercentage, float totalStep, string modId, bool isStandalone = false)
        {
            var groupId = isStandalone ? $"{LOADING_SUBTEMPLATES_NOTIFICATION_ID}:{modId}" : LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID;

            NotificationHelper.NotifyProgress(groupId, Mathf.RoundToInt(offsetPercentage + (.11f * totalStep)), textI18n: $"{LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID}.searchingForFiles");
            yield return 0;
            var files = Directory.GetFiles(m_modsTemplatesFolder[modId].rootFolder, $"*.{SIMPLE_LAYOUT_EXTENSION}", SearchOption.TopDirectoryOnly)
                .Select(y => (y, $"{m_modsTemplatesFolder[modId].name}: {y[m_modsTemplatesFolder[modId].rootFolder.Length..]}"))
                .ToArray();

            if (!ModsSubTemplates.TryGetValue(modId, out var list))
            {
                ModsSubTemplates[modId] = list = new Dictionary<string, WETextDataXmlTree>();
            }
            else
            {
                list.Clear();
            }

            if (files.Length > 0)
            {
                var errorsList = new Dictionary<string, LocalizedString>();
                for (int i = 0; i < files.Length; i++)
                {
                    var fileItemFull = files[i];
                    var fileItem = fileItemFull.Item1;
                    var displayName = fileItemFull.Item2;
                    NotificationHelper.NotifyProgress(groupId, Mathf.RoundToInt(offsetPercentage + ((.11f + (.89f * ((i + 1f) / files.Length))) * totalStep)),
                            textI18n: $"{LOADING_PREFAB_LAYOUTS_NOTIFICATION_ID}.loadingLayoutFile", argsText: new()
                            {
                                ["fileName"] = LocalizedString.Value(displayName),
                                ["progress"] = LocalizedString.Value($"{i}/{files.Length}")
                            });
                    yield return 0;
                    try
                    {
                        var tree = WETextDataXmlTree.FromXML(File.ReadAllText(fileItem));
                        if (tree is null) yield break;
                        ExtractReplaceableContent(tree, modId);


                        var templateName = Path.GetFileName(fileItem)[..^(SIMPLE_LAYOUT_EXTENSION.Length + 1)];
                        list[templateName] = tree;

                        if (BasicIMod.DebugMode) LogUtils.DoLog($"Loaded subtemplate \"{displayName}\"");
                    }
                    catch (Exception e)
                    {
                        LogUtils.DoWarnLog($"Failed loading subtemplate \"{displayName}\": {e}");
                        yield break;
                    }

                }
                if (errorsList.Count > 0)
                {
                    NotificationHelper.NotifyWithCallback(ERRORS_LOADING_SUBTEMPLATES_NOTIFICATION_ID, Colossal.PSI.Common.ProgressState.Warning, () =>
                    {
                        var dialog2 = new MessageDialog(
                            LocalizedString.Id(NotificationHelper.GetModDefaultNotificationTitle(ERRORS_LOADING_SUBTEMPLATES_NOTIFICATION_ID)),
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
                            NotificationHelper.RemoveNotification(ERRORS_LOADING_SUBTEMPLATES_NOTIFICATION_ID);
                        });
                    });
                }
                m_templatesDirty = true;
            }
            NotificationHelper.NotifyProgress(groupId, Mathf.RoundToInt(offsetPercentage + totalStep), textI18n: $"{LOADING_SUBTEMPLATES_NOTIFICATION_ID}.loadingComplete");

        }
        internal record struct ModSubtemplateRegistry(string ModId, string ModName, string[] Subtemplates) { }

        internal ModSubtemplateRegistry[] ListModSubtemplates() => m_modsTemplatesFolder
            .Where(x => ModsSubTemplates.ContainsKey(x.Key))
            .Select(x => new ModSubtemplateRegistry(x.Key, x.Value.name, ModsSubTemplates[x.Key].Select(y => $"{x.Key}:{y.Key}").ToArray()))
            .ToArray();

        #endregion
        #region City Templates
        public bool SaveCityTemplate(string name, Entity e)
        {
            var templateEntity = WETextDataXmlTree.FromEntity(e, EntityManager);
            CommonSaveAsTemplate(name, templateEntity);
            if (BasicIMod.DebugMode) LogUtils.DoLog($"Saved {e} as WETextDataTree {templateEntity.Guid} @ {name}");
            return true;
        }
        public bool SaveCityTemplate(string name, WETextDataXmlTree templateEntity)
        {
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
        public int GetCityTemplateUsageCount(string name)
        {
            return World.GetExistingSystemManaged<WETemplateQuerySystem>().GetCityTemplateUsageCount(name);
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
        private readonly Dictionary<string, HashSet<string>> m_subtemplatesMapped = new();
        private readonly Dictionary<string, HashSet<string>> m_meshesMapped = new();
        private readonly Dictionary<string, Dictionary<string, string>> m_atlasesReplacements = new();
        private readonly Dictionary<string, Dictionary<string, string>> m_fontsReplacements = new();
        private readonly Dictionary<string, Dictionary<string, string>> m_subtemplatesReplacements = new();
        private readonly Dictionary<string, Dictionary<string, string>> m_meshesReplacements = new();
        public ushort SpritesAndLayoutsDataVersion { get; private set; } = 0;

        private Dictionary<string, (string name, string id, string rootFolder)> m_modsTemplatesFolder = new();

        public void RegisterModTemplatesForLoading(Assembly mainAssembly, string folderTemplatesSource)
        {
            var modData = ModManagementUtils.GetModDataFromMainAssembly(mainAssembly).asset;
            var modId = WEModIntegrationUtility.GetModIdentifier(mainAssembly);
            var modName = modData.mod.displayName;

            if (m_modsTemplatesFolder.TryGetValue(modId, out var folder) && folder.rootFolder == folderTemplatesSource) return;
            m_modsTemplatesFolder[modId] = (modName, modId, folderTemplatesSource);
            GameManager.instance.StartCoroutine(LoadModSubtemplates_Item(0, 100, modId));
            MarkPrefabsDirty();
        }

        public FixedString128Bytes[] GetTemplateAvailableKeys() => RegisteredTemplates.Keys.Union(ModsSubTemplates.SelectMany(x => x.Value.Keys.Select(y => new FixedString128Bytes($"{x.Key}:{y}")))).ToArray();

        internal void RegisterLoadableTemplatesFolder(Assembly mainAssembly, ModFolder fontFolder) { integrationLoadableTemplatesFromMod[mainAssembly] = fontFolder; }
        internal List<ModFolder> ListModsExtraFolders() => integrationLoadableTemplatesFromMod.Values.ToList();

        internal FixedString64Bytes GetFontFor(string strOriginal, FixedString64Bytes currentFont, ref bool haveChanges)
        {
            if (!strOriginal.Contains(":")) return strOriginal;
            var decomposedName = strOriginal.Split(":", 2);
            var result = m_fontsReplacements.TryGetValue(decomposedName[0], out var fontList) && fontList.TryGetValue(decomposedName[1], out var fontName)
                        ? (FixedString64Bytes)fontName
                        : default;
            if (result != currentFont)
            {
                haveChanges |= true;
            }
            return result;
        }

        internal FixedString64Bytes GetAtlasFor(string strOriginal, FixedString64Bytes currentAtlas, ref bool haveChanges)
        {
            if (!strOriginal.Contains(":")) return strOriginal;
            var decomposedName = strOriginal.Split(":", 2);
            FixedString64Bytes result = (m_atlasesReplacements.TryGetValue(decomposedName[0], out var atlasList) && atlasList.TryGetValue(decomposedName[1], out var atlasName)
                        ? atlasName
                        : strOriginal) ?? "";
            if (result != currentAtlas)
            {
                haveChanges |= true;
            }
            return result;
        }

        internal FixedString64Bytes GetTemplateFor(string strOriginal, string currentTemplate, ref bool haveChanges)
        {
            if (!strOriginal.Contains(":")) return strOriginal;
            var decomposedName = strOriginal.Split(":", 2);
            FixedString64Bytes result = (m_subtemplatesReplacements.TryGetValue(decomposedName[0], out var templateList) && templateList.TryGetValue(decomposedName[1], out var templateName)
                        ? templateName
                        : strOriginal) ?? "";
            if (result != currentTemplate)
            {
                haveChanges |= true;
            }
            return result;
        }

        internal FixedString64Bytes GetMeshFor(string strOriginal, string currentMesh, ref bool haveChanges)
        {
            if (!strOriginal.Contains(":")) return strOriginal;
            var decomposedName = strOriginal.Split(":", 2);
            FixedString64Bytes result = (m_meshesReplacements.TryGetValue(decomposedName[0], out var templateList) && templateList.TryGetValue(decomposedName[1], out var templateName)
                        ? templateName
                        : strOriginal) ?? "";
            if (result != currentMesh)
            {
                haveChanges |= true;
            }
            return result;
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
            public StringableXmlDictionary subtemplates;
            public StringableXmlDictionary meshes;

            public ModReplacementData() { }
            internal ModReplacementData(string modId, string displayName, Dictionary<string, string> atlases, Dictionary<string, string> fonts, Dictionary<string, string> subtemplates, Dictionary<string, string> meshes)
            {
                this.modId = modId;
                this.displayName = displayName;
                this.atlases = new(); this.atlases.AddRange(atlases);
                this.fonts = new(); this.fonts.AddRange(fonts);
                this.subtemplates = new(); this.subtemplates.AddRange(subtemplates);
                this.meshes = new(); this.meshes.AddRange(meshes);
            }
        }

        internal ModReplacementData[] GetModsReplacementData()
            => m_modsTemplatesFolder.Select(x =>
            {
                var modId = x.Key;
                var modName = x.Value.name;
                var atlasesReplacements = MergeDictionaries(modId, m_atlasesMapped, m_atlasesReplacements);
                var fontsReplacements = MergeDictionaries(modId, m_fontsMapped, m_fontsReplacements);
                var subtemplateReplacements = MergeDictionaries(modId, m_subtemplatesMapped, m_subtemplatesReplacements);
                var meshesReplacements = MergeDictionaries(modId, m_meshesMapped, m_meshesReplacements);
                return new ModReplacementData(modId, modName, atlasesReplacements, fontsReplacements, subtemplateReplacements, meshesReplacements);
            }).ToArray();

        private static Dictionary<string, string> MergeDictionaries(string modId, Dictionary<string, HashSet<string>> mapped, Dictionary<string, Dictionary<string, string>> replacements)
        {
            if (mapped.TryGetValue(modId, out var mappedSet))
            {
                mappedSet.RemoveWhere(x => x.StartsWith("__"));
                return replacements.TryGetValue(modId, out var replacementDict)
                    ? mappedSet.ToDictionary(x => x, x => replacementDict.TryGetValue(x, out var data) ? data : null)
                    : mappedSet.ToDictionary(x => x, x => (string)null);
            }
            else
            {
                return new();
            }
        }

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
                    IncreaseSpritesAndLayoutsDataVersion();
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
        internal string SetModMeshReplacement(string modId, string original, string target)
        {
            if (m_meshesMapped.ContainsKey(modId))
            {
                if (!m_meshesReplacements.TryGetValue(modId, out var mesh))
                {
                    mesh = m_meshesReplacements[modId] = new();
                }
                if (m_meshesMapped[modId].Contains(original))
                {
                    IncreaseSpritesAndLayoutsDataVersion();
                    if (target.TrimToNull() is null)
                    {
                        mesh.Remove(original);
                        return null;
                    }
                    return mesh[original] = target.TrimToNull() ?? original;
                }
            }
            return null;
        }

        internal void IncreaseSpritesAndLayoutsDataVersion()
        {
            unchecked
            {
                SpritesAndLayoutsDataVersion++;
            }
        }

        internal string SetModFontReplacement(string modId, string original, string target)
        {
            if (m_fontsMapped.TryGetValue(modId, out var mapping) && mapping.Contains(original))
            {
                if (!m_fontsReplacements.TryGetValue(modId, out var fonts))
                {
                    fonts = m_fontsReplacements[modId] = new();
                }
                IncreaseSpritesAndLayoutsDataVersion();
                if (target.TrimToNull() is null)
                {
                    fonts.Remove(original);
                    return null;
                }
                return fonts[original] = target.TrimToNull() ?? original;
            }

            return null;
        }
        internal string SetModSubtemplateReplacement(string modId, string original, string target)
        {
            if (m_subtemplatesMapped.TryGetValue(modId, out var mapping) && mapping.Contains(original))
            {
                if (!m_subtemplatesReplacements.TryGetValue(modId, out var subtemplates))
                {
                    subtemplates = m_subtemplatesReplacements[modId] = new();
                }
                IncreaseSpritesAndLayoutsDataVersion();
                if (target.TrimToNull() is null)
                {
                    subtemplates.Remove(original);
                    return null;
                }
                return subtemplates[original] = target.TrimToNull() ?? original;
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
                m_subtemplatesReplacements.Clear();
                m_atlasesReplacements.AddRange(m_atlasesMapped.Keys.Select(x => (content.Mods.Where(y => y.modId == x).FirstOrDefault()) ?? new() { modId = x }).ToDictionary(x => x.modId, x => x.atlases ?? new()));
                m_fontsReplacements.AddRange(m_fontsMapped.Keys.Select(x => (content.Mods.Where(y => y.modId == x).FirstOrDefault()) ?? new() { modId = x }).ToDictionary(x => x.modId, x => x.fonts ?? new()));
                m_subtemplatesReplacements.AddRange(m_subtemplatesMapped.Keys.Select(x => (content.Mods.Where(y => y.modId == x).FirstOrDefault()) ?? new() { modId = x }).ToDictionary(x => x.modId, x => x.subtemplates ?? new()));
                IncreaseSpritesAndLayoutsDataVersion();
            }
            catch
            {
                return false;
            }

            return true;
        }

        internal Dictionary<string, string> GetMetadatasFromReplacement(Assembly mainAssembly, string originalLayoutName)
        {
            var modId = WEModIntegrationUtility.GetModIdentifier(mainAssembly);
            if (m_subtemplatesMapped.TryGetValue(modId, out var mappings) && mappings.Contains(originalLayoutName))
            {
                bool haveChanges = false;
                var targetTemplateName = GetTemplateFor(WEModIntegrationUtility.GetModAccessName(mainAssembly, originalLayoutName), "", ref haveChanges);
                if (TryGetTargetTemplate(targetTemplateName, out WETextDataXmlTree targetTemplate))
                {
                    return targetTemplate.metadatas.Where(x => x.dll == modId).GroupBy(x => x.refName).ToDictionary(x => x.Key, x => x.First().content);
                }
            }
            return null;
        }

        #endregion

        #region System Communication API
        // Internal API for communication between WETemplateManager and the new systems

        /// <summary>
        /// Checks if the game is in a loading or initializing state
        /// </summary>
        internal bool IsGameLoadingOrInitializing => 
            GameManager.instance.isGameLoading || !WriteEverywhereCS2Mod.IsInitializationComplete;

        /// <summary>
        /// Flag indicating templates have changed and entities need updating
        /// </summary>
        internal bool TemplatesDirty 
        { 
            get => m_templatesDirty;
            set => m_templatesDirty = value;
        }

        /// <summary>
        /// Tries to get a city template by name
        /// </summary>
        internal bool TryGetCityTemplate(FixedString128Bytes name, out WETextDataXmlTree template)
        {
            return RegisteredTemplates.TryGetValue(name, out template);
        }

        /// <summary>
        /// Tries to get a prefab template by index
        /// </summary>
        internal bool TryGetPrefabTemplate(long prefabIndex, out WETextDataXmlTree template)
        {
            return PrefabTemplates.TryGetValue(prefabIndex, out template);
        }

        /// <summary>
        /// Tries to get a mod subtemplate
        /// </summary>
        internal bool TryGetModSubtemplate(string modId, string templateName, out WETextDataXmlTree template)
        {
            template = null;
            return ModsSubTemplates.TryGetValue(modId, out var modTemplates) 
                && modTemplates.TryGetValue(templateName, out template);
        }

        /// <summary>
        /// Gets the entity query for template-based entities
        /// </summary>
        internal EntityQuery GetTemplateBasedEntitiesQuery() => m_templateBasedEntities;

        /// <summary>
        /// Gets read-only access to prefab templates dictionary
        /// </summary>
        internal IReadOnlyDictionary<long, WETextDataXmlTree> GetPrefabTemplatesReadOnly() => PrefabTemplates;

        /// <summary>
        /// Clears the templates dirty flag
        /// </summary>
        internal void ClearTemplatesDirty() => m_templatesDirty = false;

        /// <summary>
        /// Gets whether entities are currently being updated on main thread
        /// </summary>
        internal Coroutine UpdatingEntitiesOnMain => m_updatingEntitiesOnMain;

        #endregion

    }
}
