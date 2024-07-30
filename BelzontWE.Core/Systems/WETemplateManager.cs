using Belzont.Serialization;
using Belzont.Utils;
using Colossal.Serialization.Entities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

namespace BelzontWE
{
    public partial class WETemplateManager : SystemBase, IBelzontSerializableSingleton<WETemplateManager>
    {
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
        private EntityQuery m_templateBasedEntities;

        public Entity this[FixedString32Bytes idx]
        {
            get
            {
                var value = RegisteredTemplates.TryGetValue(idx, out var entity) ? entity : Entity.Null;
                LogUtils.DoLog($"Loaded {value} @ {idx}");
                return value;
            }
            set
            {
                if (RegisteredTemplates.TryGetValue(idx, out var obsoleteTemplate))
                {
                    m_obsoleteTemplateList.Add(obsoleteTemplate);
                    RegisteredTemplates.Remove(idx);
                }
                LogUtils.DoLog($"Saved {value} @ {idx}");
                RegisteredTemplates.Add(idx, value);
            }
        }

        protected override void OnCreate()
        {
            RegisteredTemplates = new UnsafeParallelHashMap<FixedString32Bytes, Entity>(0, Allocator.Persistent);
            m_obsoleteTemplateList = new NativeList<Entity>(Allocator.Persistent);
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
        }

        protected override void OnDestroy()
        {
            RegisteredTemplates.Dispose();
        }

        protected override void OnUpdate()
        {
            if (m_obsoleteTemplateList.IsEmpty) return;
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

        public JobHandle SetDefaults(Context context)
        {
            return Dependency;
        }
    }
}