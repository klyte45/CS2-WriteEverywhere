﻿using Belzont.Utils;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace BelzontWE
{
    public struct WETemplateUpdater : IComponentData, ICleanupComponentData, ISerializable
    {
        public static int CURRENT_VERSION = 0;

        public Entity templateEntity;
        public Entity childEntity;

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out int version);
            if (version > CURRENT_VERSION)
            {
                LogUtils.DoWarnLog($"Invalid version for {GetType()}: {version}");
                return;
            }
            reader.Read(out templateEntity);
            reader.Read(out childEntity);
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(CURRENT_VERSION);
            writer.Write(templateEntity);
            writer.Write(childEntity);
        }
    }
}