using Belzont.Interfaces;
using Belzont.Serialization;
using Belzont.Utils;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Prefabs;
using Game.Rendering;
using Game.SceneFlow;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

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
        private NativeList<Entity> m_obsoleteTemplateList;
        private PrefabSystem m_prefabSystem;
        private EntityQuery m_templateBasedEntities;
        private EntityQuery m_missingWePrefabLayoutQuery;
        private readonly Dictionary<string, Entity> PrefabTemplates = new();
        private bool m_prefabTemplatesInitialized;

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
                if (BasicIMod.DebugMode) LogUtils.DoLog($"Saved {value} @ {idx}");
                RegisteredTemplates.Add(idx, value);
            }
        }

        protected override void OnCreate()
        {
            RegisteredTemplates = new UnsafeParallelHashMap<FixedString32Bytes, Entity>(0, Allocator.Persistent);
            m_obsoleteTemplateList = new NativeList<Entity>(Allocator.Persistent);
            m_prefabSystem = World.GetExistingSystemManaged<PrefabSystem>();
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
                        }
                    }
              });
            m_missingWePrefabLayoutQuery = GetEntityQuery(new EntityQueryDesc[]
              {
                    new ()
                    {
                        Any = new ComponentType[]
                        {
                            ComponentType.ReadOnly<Game.Objects.Transform>(),
                            ComponentType.ReadOnly<InterpolatedTransform>(),
                            ComponentType.ReadOnly<PrefabRef>(),
                        },
                        None = new ComponentType[]
                        {
                            ComponentType.ReadOnly<WETemplateForPrefab>(),
                        }
                    }
              });
        }

        private bool LoadTemplatesFromFolder()
        {
            if (GameManager.instance.isLoading || (GameManager.instance.gameMode & Game.GameMode.GameOrEditor) == 0) return m_prefabTemplatesInitialized = false;
            PrefabTemplates.Clear();
            var files = Directory.GetFiles(SAVED_PREFABS_FOLDER, $"*.{PREFAB_LAYOUT_EXTENSION}");
            foreach (var f in files)
            {
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
                var prefabName = Path.GetFileName(f)[..^(PREFAB_LAYOUT_EXTENSION.Length + 1)];

                PrefabTemplates[prefabName] = generatedEntity;
                if (BasicIMod.DebugMode) LogUtils.DoLog($"Loaded template for prefab: //{prefabName}// => {generatedEntity}");
            }
            return m_prefabTemplatesInitialized = true;
        }

        protected override void OnDestroy()
        {
            RegisteredTemplates.Dispose();
        }

        protected override void OnUpdate()
        {
            if (GameManager.instance.isLoading || GameManager.instance.isGameLoading) return;
            if (!m_obsoleteTemplateList.IsEmpty)
            {
                if (!m_templateBasedEntities.IsEmpty)
                {
                    var entities = m_templateBasedEntities.ToEntityArray(Allocator.Temp);
                    var updaters = m_templateBasedEntities.ToComponentDataArray<WETemplateUpdater>(Allocator.Temp);
                    for (int i = 0; i < updaters.Length; i++)
                    {
                        if (m_obsoleteTemplateList.Contains(updaters[i].templateEntity))
                        {
                            EntityManager.AddComponent<WEWaitingRendering>(entities[i]);
                        }
                    }
                    m_obsoleteTemplateList.Clear();
                    entities.Dispose();
                    updaters.Dispose();
                }
            }
            if (!m_missingWePrefabLayoutQuery.IsEmpty)
            {
                if (!m_prefabTemplatesInitialized && !LoadTemplatesFromFolder())
                {
                    return;
                }
                var entities = m_missingWePrefabLayoutQuery.ToEntityArray(Allocator.Temp);
                try
                {
                    for (int i = 0; i < entities.Length; i++)
                    {
                        EntityManager.TryGetComponent<PrefabRef>(entities[i], out var prefabRef);
                        var name = m_prefabSystem.GetPrefabName(prefabRef.m_Prefab);
                        if (PrefabTemplates.TryGetValue(name, out var defaultLayout))
                        {
                            var childEntity = WELayoutUtility.DoCloneTextItemReferenceSelf(defaultLayout, entities[i], EntityManager, true);
                            EntityManager.AddComponentData<WETemplateForPrefab>(entities[i], new()
                            {
                                childEntity = childEntity
                            });
                            if (BasicIMod.DebugMode) LogUtils.DoLog($"Loaded layout for prefab: {name} ({entities[i]}) => {childEntity}");
                            continue;

                        }
                        EntityManager.AddComponent<WETemplateForPrefab>(entities[i]);
                        if (i > 10000) break;
                    }
                }
                finally
                {
                    entities.Dispose();
                }
            }
        }

        public JobHandle SetDefaults(Context context)
        {
            RegisteredTemplates.Clear();
            return Dependency;
        }
    }
}