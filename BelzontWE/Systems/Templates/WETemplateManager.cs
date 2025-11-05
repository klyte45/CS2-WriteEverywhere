using Belzont.Interfaces;
using Belzont.Serialization;
using Belzont.Utils;
using BelzontWE.Sprites;
using Colossal.Serialization.Entities;
using Game;
using Game.Common;
using Game.Prefabs;
using Game.SceneFlow;
using Game.Tools;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private EntityQuery m_prefabsToMarkDirty;
        private EntityQuery m_prefabsDataToSerialize;
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
    }
}
