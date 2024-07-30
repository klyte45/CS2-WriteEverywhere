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
                if (RegisteredTemplates.ContainsKey(idx)) { RegisteredTemplates.Remove(idx); }
                LogUtils.DoLog($"Saved {value} @ {idx}");
                RegisteredTemplates.Add(idx, value);
            }
        }

        protected override void OnCreate()
        {
            RegisteredTemplates = new UnsafeParallelHashMap<FixedString32Bytes, Entity>(0, Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            RegisteredTemplates.Dispose();
        }

        protected override void OnUpdate()
        {
        }

        public JobHandle SetDefaults(Context context)
        {
            return Dependency;
        }
    }
}