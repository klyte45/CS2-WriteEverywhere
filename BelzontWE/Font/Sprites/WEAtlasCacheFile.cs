using BelzontWE.Sprites;
using Colossal;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using HeuristicMethod = MaxRectsBinPack.FreeRectChoiceHeuristic;

namespace BelzontWE.Font.Sprites
{
    /// <summary>
    /// Binary file format for persisting pre-compressed BC7 atlas data.
    /// Uses a mod-private BinaryWriter/BinaryReader layout (not the game IWriter/IReader).
    /// </summary>
    public class WEAtlasCacheFile
    {
        /// <summary>Magic number for the file header: ASCII "WBC7".</summary>
        public const uint MAGIC = 0x37434257; // little-endian "WBC7"

        /// <summary>Current binary format version.</summary>
        /// <remarks>Bumped to 2 when BC7 layer data changed to full mip chains (mip0+mip1+...).
        /// Old format-1 files (mip0-only BC7) are rejected so caches are rebuilt with correct mip data.</remarks>
        public const uint FORMAT_VERSION = 2;

        public uint Checksum { get; }
        public int Width { get; }
        public int Height { get; }
        public int Size { get; }
        public HeuristicMethod Method { get; }

        // MaxRectsBinPack state
        public int BinWidth { get; }
        public int BinHeight { get; }
        public bool AllowRotations { get; }
        public IReadOnlyList<Rect> UsedRectangles { get; }
        public IReadOnlyList<Rect> FreeRectangles { get; }

        /// <summary>Ordered sprite entries matching the atlas <c>Sprites</c> dictionary.</summary>
        public IReadOnlyList<CachedSprite> Sprites { get; }

        /// <summary>
        /// Raw BC7 bytes for each of the 5 atlas layers in order:
        /// [0]=main [1]=emissive [2]=control [3]=mask [4]=normal.
        /// A null element means the layer has no data.
        /// </summary>
        public byte[]?[] LayerBC7 { get; }

        /// <summary>
        /// Saved VT layer GUIDs (5 elements) for persistent tile file reuse.
        /// Null when no VT data was cached (e.g., older cache files).
        /// </summary>
        public Colossal.Hash128[]? VTLayerGuids { get; }

        public readonly struct CachedSprite
        {
            public readonly string Name;
            public readonly Rect Region;
            public readonly WESpriteInfo.ExtraTexturesFlag Flags;

            public CachedSprite(string name, Rect region, WESpriteInfo.ExtraTexturesFlag flags)
            {
                Name = name;
                Region = region;
                Flags = flags;
            }
        }

        public WEAtlasCacheFile(
            uint checksum, int width, int height, int size,
            HeuristicMethod method, MaxRectsBinPack rectsPack,
            IReadOnlyList<CachedSprite> sprites, byte[]?[] layerBC7,
            Colossal.Hash128[]? vtLayerGuids = null)
        {
            if (layerBC7 is null || layerBC7.Length != 5)
                throw new ArgumentException("layerBC7 must have exactly 5 elements.", nameof(layerBC7));

            Checksum = checksum;
            Width = width;
            Height = height;
            Size = size;
            Method = method;

            BinWidth = rectsPack.binWidth;
            BinHeight = rectsPack.binHeight;
            AllowRotations = rectsPack.allowRotations;
            UsedRectangles = rectsPack.usedRectangles.AsReadOnly();
            FreeRectangles = rectsPack.freeRectangles.AsReadOnly();

            Sprites = sprites;
            LayerBC7 = layerBC7;
            VTLayerGuids = vtLayerGuids;
        }

        // Internal constructor for deserialization
        private WEAtlasCacheFile(
            uint checksum, int width, int height, int size,
            HeuristicMethod method,
            int binWidth, int binHeight, bool allowRotations,
            List<Rect> usedRects, List<Rect> freeRects,
            List<CachedSprite> sprites, byte[]?[] layerBC7,
            Colossal.Hash128[]? vtLayerGuids)
        {
            Checksum = checksum;
            Width = width;
            Height = height;
            Size = size;
            Method = method;
            BinWidth = binWidth;
            BinHeight = binHeight;
            AllowRotations = allowRotations;
            UsedRectangles = usedRects.AsReadOnly();
            FreeRectangles = freeRects.AsReadOnly();
            Sprites = sprites.AsReadOnly();
            LayerBC7 = layerBC7;
            VTLayerGuids = vtLayerGuids;
        }

        /// <summary>
        /// Reconstructs a <see cref="MaxRectsBinPack"/> from the cached state.
        /// </summary>
        public MaxRectsBinPack RebuildRectsPack()
        {
            var pack = new MaxRectsBinPack();
            pack.binWidth = BinWidth;
            pack.binHeight = BinHeight;
            pack.allowRotations = AllowRotations;
            pack.usedRectangles.Clear();
            foreach (var r in UsedRectangles) pack.usedRectangles.Add(r);
            pack.freeRectangles.Clear();
            foreach (var r in FreeRectangles) pack.freeRectangles.Add(r);
            return pack;
        }

        /// <summary>
        /// Writes the cache file to disk. Creates the directory if it does not exist.
        /// </summary>
        public void WriteTo(string filePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            using var stream = File.Open(filePath, FileMode.Create, FileAccess.Write);
            using var w = new BinaryWriter(stream);

            w.Write(MAGIC);
            w.Write(FORMAT_VERSION);
            w.Write(Checksum);
            w.Write(Width);
            w.Write(Height);
            w.Write(Size);
            w.Write((byte)Method);

            // MaxRectsBinPack state
            w.Write(BinWidth);
            w.Write(BinHeight);
            w.Write(AllowRotations);
            WriteRects(w, UsedRectangles);
            WriteRects(w, FreeRectangles);

            // Sprites
            w.Write(Sprites.Count);
            foreach (var s in Sprites)
            {
                w.Write(s.Name);
                w.Write(s.Region.x);
                w.Write(s.Region.y);
                w.Write(s.Region.width);
                w.Write(s.Region.height);
                w.Write((byte)s.Flags);
            }

            // 5 layers
            foreach (var layer in LayerBC7)
            {
                if (layer is null)
                {
                    w.Write(-1);
                }
                else
                {
                    w.Write(layer.Length);
                    w.Write(layer);
                }
            }

            // VT layer GUIDs (optional, appended after layers)
            if (VTLayerGuids != null && VTLayerGuids.Length == 5)
            {
                w.Write((byte)1); // marker: GUIDs present
                foreach (var guid in VTLayerGuids)
                {
                    w.Write(guid.value.x);
                    w.Write(guid.value.y);
                    w.Write(guid.value.z);
                    w.Write(guid.value.w);
                }
            }
            else
            {
                w.Write((byte)0); // marker: no GUIDs
            }
        }

        /// <summary>
        /// Reads a cache file from disk.
        /// Returns <c>null</c> on invalid magic, unsupported version, or I/O error.
        /// </summary>
        public static WEAtlasCacheFile? ReadFrom(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            try
            {
                using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
                using var r = new BinaryReader(stream);

                var magic = r.ReadUInt32();
                if (magic != MAGIC) return null;

                var version = r.ReadUInt32();
                if (version != FORMAT_VERSION) return null;

                var checksum = r.ReadUInt32();
                var width = r.ReadInt32();
                var height = r.ReadInt32();
                var size = r.ReadInt32();
                var method = (HeuristicMethod)r.ReadByte();

                // MaxRectsBinPack state
                var binWidth = r.ReadInt32();
                var binHeight = r.ReadInt32();
                var allowRotations = r.ReadBoolean();
                var usedRects = ReadRects(r);
                var freeRects = ReadRects(r);

                // Sprites
                var spriteCount = r.ReadInt32();
                var sprites = new List<CachedSprite>(spriteCount);
                for (int i = 0; i < spriteCount; i++)
                {
                    var name = r.ReadString();
                    var rx = r.ReadSingle();
                    var ry = r.ReadSingle();
                    var rw = r.ReadSingle();
                    var rh = r.ReadSingle();
                    var flags = (WESpriteInfo.ExtraTexturesFlag)r.ReadByte();
                    sprites.Add(new CachedSprite(name, new Rect(rx, ry, rw, rh), flags));
                }

                // 5 layers
                var layers = new byte[]?[5];
                for (int i = 0; i < 5; i++)
                {
                    var len = r.ReadInt32();
                    layers[i] = len < 0 ? null : r.ReadBytes(len);
                }

                // VT layer GUIDs (optional, may not be present in older files)
                Colossal.Hash128[]? vtGuids = null;
                try
                {
                    if (stream.Position < stream.Length)
                    {
                        byte marker = r.ReadByte();
                        if (marker == 1)
                        {
                            vtGuids = new Colossal.Hash128[5];
                            for (int i = 0; i < 5; i++)
                            {
                                vtGuids[i] = new Colossal.Hash128(
                                    r.ReadUInt32(), r.ReadUInt32(),
                                    r.ReadUInt32(), r.ReadUInt32());
                            }
                        }
                    }
                }
                catch { /* older file without GUIDs — ignore */ }

                return new WEAtlasCacheFile(
                    checksum, width, height, size, method,
                    binWidth, binHeight, allowRotations,
                    usedRects, freeRects, sprites, layers, vtGuids);
            }
            catch
            {
                return null;
            }
        }

        private static void WriteRects(BinaryWriter w, IReadOnlyList<Rect> rects)
        {
            w.Write(rects.Count);
            foreach (var r in rects)
            {
                w.Write(r.x);
                w.Write(r.y);
                w.Write(r.width);
                w.Write(r.height);
            }
        }

        private static List<Rect> ReadRects(BinaryReader r)
        {
            var count = r.ReadInt32();
            var list = new List<Rect>(count);
            for (int i = 0; i < count; i++)
            {
                var x = r.ReadSingle();
                var y = r.ReadSingle();
                var w = r.ReadSingle();
                var h = r.ReadSingle();
                list.Add(new Rect(x, y, w, h));
            }
            return list;
        }
    }
}
