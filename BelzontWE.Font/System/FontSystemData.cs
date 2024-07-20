//#define JOBS_DEBUG

using Belzont.Interfaces;
using Belzont.Utils;
using Colossal.Serialization.Entities;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;

namespace BelzontWE.Font
{
    public struct FontSystemData : IDisposable, IComponentData, ISerializable
    {
        private const uint CURRENT_VERSION = 0;

        private GCHandle _fontAddr;
        private GCHandle _systemAddr;
        public Font Font
        {
            get => _fontAddr.IsAllocated ? _fontAddr.Target as Font : null;
            set
            {
                if (_fontAddr.IsAllocated) _fontAddr.Free();
                _fontAddr = GCHandle.Alloc(value);
            }
        }

        public FontSystem FontSystem
        {
            get => _systemAddr.IsAllocated ? _systemAddr.Target as FontSystem : null;
            private set
            {
                if (BasicIMod.DebugMode) LogUtils.DoLog($"Setting font system! {name}");
                if (_systemAddr.IsAllocated) _systemAddr.Free();
                _systemAddr = GCHandle.Alloc(value);
            }
        }

        private FixedString32Bytes name;
        public string Name => name.ToString();

        public void Dispose()
        {
            if (_fontAddr.IsAllocated)
            {
                _fontAddr.Free();
            }
            if (_systemAddr.IsAllocated)
            {
                FontSystem.Dispose();
                _systemAddr.Free();
            }
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(CURRENT_VERSION);
            writer.Write(name);
            var zippedFontFile = new NativeArray<byte>(ZipUtils.ZipBytes(Font._font.data.ArrayData), Allocator.Temp);
            writer.Write(zippedFontFile.Length);
            writer.Write(zippedFontFile);
            zippedFontFile.Dispose();
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

        public static FontSystemData From(byte[] fontData, string name)
        {
            var data = new FontSystemData();
            var font = Font.FromMemory(fontData);
            font.RecalculateBasedOnHeight(FontServer.QualitySize);
            data.Font = font;
            data.name = name;
            data.FontSystem = new FontSystem(data);

            return data;
        }
    }
}