using Belzont.Utils;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using System;
using System.Linq;
using Unity.Collections;
using Unity.Entities;

namespace BelzontWE
{
    public struct WETextDataTreeStruct : IDisposable, ISerializable
    {
        private const int CURRENT_VERSION = 0;
        public bool IsInitialized { get; private set; }
        public WETextDataStruct self = default;
        public NativeArray<WETextDataTreeStruct> children = default;
        private Colossal.Hash128 guid = System.Guid.NewGuid();

        public readonly Colossal.Hash128 Guid => guid;
        public WETextDataTreeStruct()
        {
            IsInitialized = true;
        }

        public readonly void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(CURRENT_VERSION);
            writer.Write(self);
            writer.Write(children.Length);
            for (int i = 0; i < children.Length; i++)
            {
                writer.Write(children[i]);
            }
            writer.Write(guid);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out int version);
            if (version > CURRENT_VERSION)
            {
                LogUtils.DoWarnLog($"Invalid version for {GetType()}: {version}");
                return;
            }
            reader.Read(out self);
            reader.Read(out int length);
            children.Dispose();
            children = new NativeArray<WETextDataTreeStruct>(length, Allocator.Persistent);
            for (int i = 0; i < length; i++)
            {
                reader.Read(out WETextDataTreeStruct tds);
                children[i] = tds;
            }
            reader.Read(out guid);
            IsInitialized = true;
        }

        public static WETextDataTreeStruct FromEntity(Entity e, EntityManager em)
        {
            if (!em.TryGetComponent<WETextData>(e, out var weTextData)) return default;
            var result = new WETextDataTreeStruct
            {
                self = weTextData.ToDataStruct(em)
            };
            if (em.TryGetBuffer<WESubTextRef>(e, true, out var subTextData))
            {
                result.children = new NativeArray<WETextDataTreeStruct>(subTextData.Length, Allocator.Persistent);
                for (int i = 0; i < subTextData.Length; i++)
                {
                    result.children[i] = FromEntity(subTextData[i].m_weTextData, em);
                }
            }
            return result;
        }

        public static WETextDataTreeStruct FromXml(WETextDataTree tree)
            => new()
            {
                self = WETextDataStruct.FromXml(tree.self),
                children = new(tree.children?.Select(x => FromXml(x)).ToArray() ?? new WETextDataTreeStruct[0], Allocator.Persistent)
            };

        public void Dispose()
        {
            for (int i = 0; i < children.Length; i++)
            {
                children[i].Dispose();
            }
            children.Dispose();
        }

        public readonly WETextDataTreeStruct WithNewGuid()
        {
            var binaryWriter = new BinaryWriter();
            var byteList = new NativeList<byte>(0, Allocator.Temp);
            var entityArr = new NativeArray<Entity>(0, Allocator.Temp);
            binaryWriter.Initialize(default, byteList, entityArr);
            Serialize(binaryWriter);
            var binaryReader = new BinaryReader();
            var pointer = new NativeReference<int>(Allocator.Temp);
            binaryReader.Initialize(default, byteList.AsArray(), pointer, entityArr);
            var newInstance = new WETextDataTreeStruct();
            newInstance.Deserialize(binaryReader);
            newInstance.guid = System.Guid.NewGuid();

            byteList.Dispose();
            entityArr.Dispose();
            pointer.Dispose();

            return newInstance;
        }

        internal WETextDataTree ToXml() => new()
        {
            self = self.ToXml(),
            children = children.ToArray().Select(x => x.ToXml()).ToArray()
        };
    }
}