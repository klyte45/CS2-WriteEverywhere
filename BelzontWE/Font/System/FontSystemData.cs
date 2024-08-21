//#define JOBS_DEBUG

using Belzont.Utils;
using Colossal.Serialization.Entities;
using System;
using Unity.Collections;
using Unity.Entities;

namespace BelzontWE.Font
{
    public class FontSystemData : IDisposable
    {
        private const uint CURRENT_VERSION = 0;

        public Font Font { get; set; }

        public FontSystem FontSystem { get; private set; }

        private FixedString32Bytes name;
        public string Name
        {
            get => name.ToString(); set
            {
                if (value.TrimToNull() != null)
                {
                    name = value;
                }
            }
        }
        public bool IsWeak { get; private set; }
        public Colossal.Hash128 Guid { get; } = System.Guid.NewGuid();

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(CURRENT_VERSION);
            writer.Write(name);
            var zippedFontFile = new NativeArray<byte>(ZipUtils.ZipBytes(Font._font.data.ArrayData), Allocator.Temp);
            writer.Write(zippedFontFile.Length);
            writer.Write(zippedFontFile);
            zippedFontFile.Dispose();
            FontSystem.ResetCache();
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out uint version);
            if (version > CURRENT_VERSION)
            {
                LogUtils.DoWarnLog($"Invalid version for {GetType()}: {version}");
                return;
            }
            reader.Read(out name);
            reader.Read(out int length);
            var zippedFontFile = new NativeArray<byte>(length, Allocator.Temp);
            try
            {
                reader.Read(zippedFontFile);
                Font = Font.FromMemory(ZipUtils.UnzipBytes(zippedFontFile.ToArray()));
                Font.RecalculateBasedOnHeight(FontServer.QualitySize);
            }
            finally
            {
                zippedFontFile.Dispose();
            }
            FontSystem = new FontSystem(this);
        }

        public static FontSystemData From(byte[] fontData, string name, bool isWeak = false)
        {
            var data = new FontSystemData();
            var font = Font.FromMemory(fontData);
            font.RecalculateBasedOnHeight(FontServer.QualitySize);
            data.Font = font;
            data.name = name;
            data.FontSystem = new FontSystem(data);
            data.IsWeak = isWeak;
            return data;
        }

        public void Dispose()
        {
            FontSystem.Dispose();
        }
    }
}