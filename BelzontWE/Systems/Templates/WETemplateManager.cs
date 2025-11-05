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
