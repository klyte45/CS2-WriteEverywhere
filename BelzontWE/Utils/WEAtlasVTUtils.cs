using Belzont.Interfaces;
using Belzont.Utils;
using BelzontWE.Sprites;
using Colossal;
using Colossal.Compression;
using Colossal.IO.AssetDatabase.VirtualTexturing;
using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEngine.Experimental.Rendering;

namespace BelzontWE
{
    /// <summary>
    /// Utility methods for converting raw BC7 atlas layer data into VT-compatible
    /// tiled format using the game's own <see cref="AtlassingUtils.PreProcessData"/>.
    /// </summary>
    public static class WEAtlasVTUtils
    {
        /// <summary>
        /// Default VT tile size for unit tests where <c>TextureStreamingSystem</c> is unavailable.
        /// Production code should use <c>tss.tileSize</c> instead.
        /// </summary>
        public const int VT_TILE_SIZE = 512;

        /// <summary>
        /// Padding border in pixels around each VT tile for texture filtering.
        /// </summary>
        public const int VT_PADDING = 8;

        /// <summary>
        /// Validates inputs for <see cref="PreprocessForVT"/> without referencing
        /// <c>Colossal.IO.AssetDatabase</c> types, so it can be unit-tested in isolation.
        /// </summary>
        internal static void ValidatePreprocessInputs(byte[] bc7Data, int width, int height, int tileSize = VT_TILE_SIZE)
        {
            if (bc7Data == null) throw new ArgumentNullException(nameof(bc7Data));
            if (tileSize <= 0 || !IsPowerOf2(tileSize))
                throw new ArgumentOutOfRangeException(nameof(tileSize), $"Tile size ({tileSize}) must be a positive power of 2.");
            if (width < tileSize)
                throw new ArgumentOutOfRangeException(nameof(width), $"Atlas width ({width}) must be ≥ tileSize ({tileSize}).");
            if (height < tileSize)
                throw new ArgumentOutOfRangeException(nameof(height), $"Atlas height ({height}) must be ≥ tileSize ({tileSize}).");
            if (!IsPowerOf2(width))
                throw new ArgumentException($"Atlas width ({width}) must be a power of 2.", nameof(width));
            if (!IsPowerOf2(height))
                throw new ArgumentException($"Atlas height ({height}) must be a power of 2.", nameof(height));

            int mip0Bytes = WEAtlasBC7Utils.GetBC7SizeBytes(width, height);
            if (bc7Data.Length < mip0Bytes)
                throw new ArgumentException($"Expected at least {mip0Bytes} bytes for {width}\u00d7{height} BC7 mip0 data, got {bc7Data.Length}.", nameof(bc7Data));
        }

        internal static bool IsPowerOf2(int x) => x > 0 && (x & (x - 1)) == 0;

        /// <summary>
        /// Preprocesses raw BC7 bytes into the game's VT tile layout
        /// using <see cref="AtlassingUtils.PreProcessData"/>.
        /// <para>
        /// <paramref name="bc7Data"/> should be a full BC7 mip chain (concatenated mip0+mip1+...)
        /// as produced by <see cref="WEAtlasBC7Utils.CompressToBC7WithMipChain"/>.
        /// When only mip0 data is supplied (legacy or 512×512 atlases), coarser VT levels will
        /// be zero-filled (decoding to solid black), which causes blurry/darkened rendering at
        /// medium/far distances. Providing the full chain fixes this.
        /// </para>
        /// <para>
        /// The returned <see cref="NativeArray{T}"/> is allocated with
        /// <see cref="Allocator.Persistent"/> — the caller <b>must</b> dispose it
        /// after uploading the tile data to the VT system.
        /// </para>
        /// </summary>
        /// <param name="bc7Data">
        ///   Concatenated BC7 block data for a single atlas layer: [mip0 bytes][mip1 bytes]...
        ///   At minimum must contain mip0 (<c>GetBC7SizeBytes(width, height)</c> bytes).
        /// </param>
        /// <param name="width">Atlas width in pixels (must be power-of-two, ≥ tileSize).</param>
        /// <param name="height">Atlas height in pixels (must be power-of-two, ≥ tileSize).</param>
        /// <param name="format">
        ///   <see cref="GraphicsFormat.RGBA_BC7_SRGB"/> for sRGB layers (main, mask, control, emissive) or
        ///   <see cref="GraphicsFormat.RGBA_BC7_UNorm"/> for linear layers (normal).
        /// </param>
        /// <param name="tileSize">VT tile size from <c>TextureStreamingSystem.tileSize</c>.</param>
        /// <returns>VT-tiled byte data ready for registration into the streaming system.</returns>
        public static NativeArray<byte> PreprocessForVT(byte[] bc7Data, int width, int height, GraphicsFormat format, int tileSize = VT_TILE_SIZE)
        {
            ValidatePreprocessInputs(bc7Data, width, height, tileSize);

            var layerInfo = new AtlassingUtils.LayerInfo(tileSize, format);

            // maxLevel = number of mip subdivisions within the tile structure.
            // With mip0-only data, maxLevel = log2(min(w,h) / tileSize).
            int maxLevel = (int)Math.Log(Math.Min(width, height) / (double)tileSize, 2.0);

            // PreProcessData reads mip levels 0 through maxLevel from the source data,
            // and ALSO reads mip (level+1) at each level for trilinear filtering
            // (CopyFromTextureData with requiresCachedMip=true).
            // Total mip levels accessed = maxLevel + 2 (levels 0..maxLevel, plus one more).
            // bc7Data contains the full mip chain (mip0+mip1+...) from CompressToBC7WithMipChain;
            // for atlases with maxLevel==0 or legacy mip0-only data, higher levels are zero-filled.
            int mipLevelsNeeded = maxLevel + 2;
            int totalSrcBytes = 0;
            int mipW = width, mipH = height;
            for (int m = 0; m < mipLevelsNeeded; m++)
            {
                totalSrcBytes += WEAtlasBC7Utils.GetBC7SizeBytes(mipW, mipH);
                mipW = Math.Max(4, mipW / 2);
                mipH = Math.Max(4, mipH / 2);
            }

            VTCrashLog($"[PreprocessForVT] {width}x{height} maxLevel={maxLevel} mipLevelsNeeded={mipLevelsNeeded} bc7DataLen={bc7Data.Length} totalSrc={totalSrcBytes}");

            // Allocate zeroed buffer for all mip levels; copy as many bytes as available
            // (up to totalSrcBytes) from bc7Data. When bc7Data contains the full mip chain,
            // all needed levels are populated. When only mip0 is available, higher levels
            // remain zero-filled (black when decoded — only affects coarser VT LOD levels).
            // IMPORTANT: must not copy more than totalSrcBytes to avoid buffer overflow when
            // bc7Data holds extra tiny mip levels beyond what PreProcessData needs.
            var input = new NativeArray<byte>(totalSrcBytes, Allocator.TempJob, NativeArrayOptions.ClearMemory);
            NativeArray<byte>.Copy(bc7Data, 0, input, 0, Math.Min(bc7Data.Length, totalSrcBytes));

            // Unity's BC7 output has bottom-up row order (inherited from RGBA32 GetRawTextureData),
            // but the game's VT pipeline (PreProcessData / CopyData) expects top-down row order
            // (matching NativeTextures.FileLoad which loads PNGs top-down).
            // Flip BC7 block-rows in-place for each mip level.
            FlipBC7BlockRowsInPlace(input, width, height, mipLevelsNeeded);

            try
            {
                var inputSlice = new NativeSlice<byte>(input);
                AtlassingUtils.PreProcessData(inputSlice, out var processedData, width, height, tileSize, maxLevel, VT_PADDING, layerInfo);
                return processedData;
            }
            finally
            {
                input.Dispose();
            }
        }

        /// <summary>
        /// Flips BC7 block-rows vertically (bottom-up → top-down) in-place for each mip level
        /// in a concatenated mip chain buffer. BC7 blocks are 4×4 pixels, 16 bytes each.
        /// </summary>
        internal static void FlipBC7BlockRowsInPlace(NativeArray<byte> data, int mip0Width, int mip0Height, int mipLevels)
        {
            const int BLOCK_SIZE = 16; // BC7: 16 bytes per 4×4 block
            int offset = 0;
            int mipW = mip0Width, mipH = mip0Height;

            for (int mip = 0; mip < mipLevels; mip++)
            {
                int blocksPerRow = Math.Max(1, mipW / 4);
                int blockRows = Math.Max(1, mipH / 4);
                int rowBytes = blocksPerRow * BLOCK_SIZE;
                int mipBytes = WEAtlasBC7Utils.GetBC7SizeBytes(mipW, mipH);

                if (offset + mipBytes > data.Length) break;

                var tempRow = new byte[rowBytes];
                var tempRow2 = new byte[rowBytes];

                // Swap block-rows: row[r] ↔ row[blockRows-1-r]
                for (int r = 0; r < blockRows / 2; r++)
                {
                    int topOff = offset + r * rowBytes;
                    int botOff = offset + (blockRows - 1 - r) * rowBytes;
                    // top → tempRow
                    NativeArray<byte>.Copy(data, topOff, tempRow, 0, rowBytes);
                    // bot → tempRow2
                    NativeArray<byte>.Copy(data, botOff, tempRow2, 0, rowBytes);
                    // tempRow2(bot) → top
                    NativeArray<byte>.Copy(tempRow2, 0, data, topOff, rowBytes);
                    // tempRow(top) → bot
                    NativeArray<byte>.Copy(tempRow, 0, data, botOff, rowBytes);
                }

                offset += mipBytes;
                mipW = Math.Max(4, mipW / 2);
                mipH = Math.Max(4, mipH / 2);
            }
        }

        /// <summary>
        /// Returns the <see cref="GraphicsFormat"/> for a given WE atlas layer.
        /// </summary>
        /// <param name="linear">
        ///   <c>true</c> for linear layers (normal);
        ///   <c>false</c> for sRGB layers (main, mask, control, emissive).
        /// </param>
        public static GraphicsFormat GetBC7Format(bool linear)
            => linear ? GraphicsFormat.RGBA_BC7_UNorm : GraphicsFormat.RGBA_BC7_SRGB;

        /// <summary>
        /// Computes the total number of VT tiles for a given texture at the specified
        /// max level. Useful for validating preprocessed output sizes.
        /// </summary>
        public static int GetTileCount(int width, int height, int maxLevel, int tileSize = VT_TILE_SIZE)
            => AtlassingUtils.TextureRelativeTileIndex(width, height, maxLevel + 1, 0, 0, tileSize);

        /// <summary>
        /// Computes the expected preprocessed byte count for a given atlas layer.
        /// </summary>
        public static int GetPreprocessedByteCount(int width, int height, GraphicsFormat format, int tileSize = VT_TILE_SIZE)
        {
            var layerInfo = new AtlassingUtils.LayerInfo(tileSize, format);
            int maxLevel = (int)Math.Log(Math.Min(width, height) / (double)tileSize, 2.0);
            int tileCount = GetTileCount(width, height, maxLevel, tileSize);
            return tileCount * (layerInfo.tileBlockSize + layerInfo.trilinearTileBlockSize) * layerInfo.blockSizeInBytes;
        }

        /// <summary>
        /// Uploads a single preprocessed VT layer into the game's streaming system.
        /// Does NOT call <c>InvalidateRegion</c> — the caller should invalidate once
        /// per stack after all layers for that texture have been uploaded (matching
        /// the game's <c>TexturesAsyncLoader.CompleteIfReady</c> pattern).
        /// <para>
        /// Flow: preprocess BC7 → register buffer → copy tile data → DoneLoading → AddTextureToCache.
        /// The <see cref="TextureStreamingSystem"/> assumes ownership of the registered buffer
        /// and disposes it inside <c>AddTextureToCache</c>.
        /// </para>
        /// </summary>
        /// <param name="tss">Game's texture streaming system.</param>
        /// <param name="atlasInfo">VT atlas info returned from <see cref="TextureStreamingSystem.ReserveTextureRect"/>.</param>
        /// <param name="layerIndex">Layer index within the VT stack (0-based).</param>
        /// <param name="bc7Data">Raw BC7 bytes for this layer (mip0).</param>
        /// <param name="width">Atlas width in pixels.</param>
        /// <param name="height">Atlas height in pixels.</param>
        /// <param name="format"><see cref="GraphicsFormat"/> matching the VT stack layer format.</param>
        /// <param name="guid">Unique <see cref="Hash128"/> identifying this layer's tile data in the VT system.</param>
        /// <param name="tileSize">VT tile size from <c>TextureStreamingSystem.tileSize</c>.</param>
        public static void UploadLayerToVT(
            TextureStreamingSystem tss, VTAtlassingInfo atlasInfo,
            int layerIndex, byte[] bc7Data, int width, int height,
            GraphicsFormat format, Hash128 guid, string vtTileFolderName,
            int tileSize = VT_TILE_SIZE, bool isCityAtlas = false)
        {
            string stepTag = $"stack={atlasInfo.stackGlobalIndex} layer={layerIndex} idx={atlasInfo.indexInStack} {width}x{height}";
            VTCrashLog($"[UploadLayerToVT] START {stepTag} fmt={format} tile={tileSize} bc7len={bc7Data.Length} guid={guid}");

            if (BasicIMod.VerboseMode) LogUtils.DoVerboseLog(
                "[WEAtlasVTUtils] UploadLayerToVT: stack={0} layer={1} idx={2} {3}x{4} fmt={5} tile={6} bc7len={7}",
                atlasInfo.stackGlobalIndex, layerIndex, atlasInfo.indexInStack,
                width, height, format, tileSize, bc7Data.Length);

            // 1. Preprocess BC7 into VT tile layout
            VTCrashLog($"[UploadLayerToVT] Step1-PreProcess {stepTag}");
            NativeArray<byte> tileData = PreprocessForVT(bc7Data, width, height, format, tileSize);
            VTCrashLog($"[UploadLayerToVT] Step1-Done {stepTag} produced={tileData.Length}");
            if (BasicIMod.VerboseMode) LogUtils.DoVerboseLog(
                "[WEAtlasVTUtils] PreprocessForVT produced {0} bytes", tileData.Length);

            // 2. Save preprocessed data to disk as zstd-compressed tiles for VT re-streaming.
            //    When the VT CPU cache evicts tiles, ReadVTTextureDataAsync re-reads
            //    individual tiles from this file using the tileOffsets mapping.
            //    Each tile is individually compressed with CompressZstdWithMarker.
            VTCrashLog($"[UploadLayerToVT] Step2-SaveToFile {stepTag}");
            string filePath;
            List<int> tileOffsets = null;
            bool existedOnDisk = TryGetExistingTileFile(guid, vtTileFolderName, isCityAtlas, out filePath);
            if (existedOnDisk)
            {
                tileOffsets = ComputeOffsetsFromFile(filePath, width, height, format, tileSize);
                if (tileOffsets == null) existedOnDisk = false; // File is corrupt — regenerate
            }
            if (!existedOnDisk)
            {
                (filePath, tileOffsets) = SaveTileDataToFile(guid, tileData, width, height, format, vtTileFolderName, tileSize, isCityAtlas);
            }
            VTCrashLog($"[UploadLayerToVT] Step2-Saved {stepTag} path={filePath} tileOffsets.Count={tileOffsets.Count} reused={existedOnDisk}");

            try
            {
                // 3. Register buffer allocation in VT system with correct path and offsets
                VTCrashLog($"[UploadLayerToVT] Step3-RegisterVTTextureData {stepTag} guid={guid} dataSize={tileData.Length}");
                bool registered = tss.RegisterVTTextureData(guid, filePath, 0, 0, tileData.Length,
                    tileOffsets, width, height);
                VTCrashLog($"[UploadLayerToVT] Step3-Done {stepTag} registered={registered}");

                if (!registered)
                {
                    LogUtils.DoWarnLog($"[WEAtlasVTUtils] RegisterVTTextureData returned false for guid {guid} — skipping layer {layerIndex}");
                    return;
                }

                // 4. Copy preprocessed tiles into the registered buffer
                VTCrashLog($"[UploadLayerToVT] Step4-GetTextureData {stepTag}");
                var buffer = tss.GetTextureData(guid);
                VTCrashLog($"[UploadLayerToVT] Step4-GotBuffer {stepTag} bufLen={buffer.Length} tileLen={tileData.Length}");
                if (buffer.Length != tileData.Length)
                {
                    LogUtils.DoWarnLog($"[WEAtlasVTUtils] Buffer length mismatch: expected {tileData.Length}, got {buffer.Length}");
                }
                VTCrashLog($"[UploadLayerToVT] Step4-Copy {stepTag}");
                NativeArray<byte>.Copy(tileData, buffer);
                VTCrashLog($"[UploadLayerToVT] Step4-Done {stepTag}");
            }
            finally
            {
                // Dispose preprocessing output regardless of success
                tileData.Dispose();
            }

            // 5. Mark loading complete (enables AddTextureToCache)
            VTCrashLog($"[UploadLayerToVT] Step5-DoneLoading {stepTag}");
            tss.DoneLoading(guid);
            VTCrashLog($"[UploadLayerToVT] Step5-Done {stepTag}");

            // 6. Transfer tile data to GPU cache (disposes registered buffer internally)
            VTCrashLog($"[UploadLayerToVT] Step6-AddTextureToCache {stepTag}");
            tss.AddTextureToCache(atlasInfo.stackGlobalIndex, layerIndex,
                atlasInfo.indexInStack, width, height, guid, 0);
            VTCrashLog($"[UploadLayerToVT] Step6-Done {stepTag}");

            if (BasicIMod.VerboseMode) LogUtils.DoVerboseLog(
                "[WEAtlasVTUtils] Layer {0} committed to VT cache (stack={1} idx={2})",
                layerIndex, atlasInfo.stackGlobalIndex, atlasInfo.indexInStack);
        }

        /// <summary>
        /// Crash-safe log: appends to a dedicated file and flushes immediately.
        /// Survives native crashes that kill the process before normal logs are flushed.
        /// </summary>
        internal static void VTCrashLog(string message)
        {
            if (!BasicIMod.VerboseMode) return;
            try
            {
                string path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "Low",
                    "Colossal Order", "Cities Skylines II", "Logs", "WE_VT_CrashLog.txt");
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (var sw = new StreamWriter(path, true, System.Text.Encoding.UTF8))
                {
                    sw.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
                    sw.Flush();
                }
            }
            catch { /* never let diagnostic logging crash the mod */ }
        }

        /// <summary>
        /// Generates a deterministic <see cref="Hash128"/> for a WE atlas layer,
        /// unique per atlas instance + stack + layer combination.
        /// </summary>
        internal static Hash128 GenerateLayerGuid(int atlasInstanceId, int stackGlobalIndex, int layerIndex)
            => Hash128.CreateGuid($"WE_VT_{atlasInstanceId}_{stackGlobalIndex}_{layerIndex}");

        #region VT tile file persistence

        /// <summary>
        /// Saves preprocessed VT tile data to disk with zstd compression per tile,
        /// matching the format expected by the game's <c>ReadVTTextureDataAsync</c>
        /// and <c>DecompressionJob</c>.
        /// </summary>
        /// <returns>Tuple of (absolute file path, cumulative compressed tile offsets).</returns>
        internal static (string filePath, List<int> tileOffsets) SaveTileDataToFile(
            Hash128 guid, NativeArray<byte> tileData,
            int width, int height, GraphicsFormat format,
            string vtTileFolderName, int tileSize = VT_TILE_SIZE,
            bool isCityAtlas = false)
        {
            var layerInfo = new AtlassingUtils.LayerInfo(tileSize, format);
            int maxLevel = (int)Math.Log(Math.Min(width, height) / (double)tileSize, 2.0);
            int numTiles = GetTileCount(width, height, maxLevel, tileSize);
            int bytesPerTile = layerInfo.totalTileSizeInBytes;

            string dir = GetVTTileFileDirectory(vtTileFolderName, isCityAtlas);
            string filePath = Path.Combine(dir, $"{guid}.vtd");
            var tileOffsets = new List<int>(numTiles);
            int cumulativeOffset = 0;

            using (var fs = File.Open(filePath, FileMode.Create, FileAccess.Write))
            {
                for (int i = 0; i < numTiles; i++)
                {
                    int srcOffset = i * bytesPerTile;
                    int tileLen = Math.Min(bytesPerTile, tileData.Length - srcOffset);
                    var tileBytes = new byte[tileLen];
                    NativeArray<byte>.Copy(tileData, srcOffset, tileBytes, 0, tileLen);
                    byte[] compressed = CompressionUtils.CompressZstdWithMarker(
                        tileBytes, tileLen, VirtualTexturingConfig.zStdCompressionLevel);
                    fs.Write(compressed, 0, compressed.Length);
                    cumulativeOffset += compressed.Length;
                    tileOffsets.Add(cumulativeOffset);
                }
            }

            return (filePath, tileOffsets);
        }

        /// <summary>
        /// Checks if a .vtd tile file already exists on disk for the given GUID and folder.
        /// </summary>
        internal static bool TryGetExistingTileFile(Hash128 guid, string vtTileFolderName, bool isCityAtlas, out string filePath)
        {
            string dir = GetVTTileFileDirectory(vtTileFolderName, isCityAtlas);
            filePath = Path.Combine(dir, $"{guid}.vtd");
            return File.Exists(filePath);
        }

        /// <summary>
        /// Reads an existing .vtd file and computes cumulative tile offsets by scanning
        /// each tile's zstd header (12-byte prefix: magic, uncompressed size, compressed size).
        /// Returns null if the file is corrupt or unreadable. 
        /// </summary>
        internal static List<int> ComputeOffsetsFromFile(string filePath, int width, int height, GraphicsFormat format, int tileSize)
        {
            try
            {
                int maxLevel = (int)Math.Log(Math.Min(width, height) / (double)tileSize, 2.0);
                int numTiles = GetTileCount(width, height, maxLevel, tileSize);
                var offsets = new List<int>(numTiles);
                int cumulativeOffset = 0;
                var header = new byte[12];

                using (var fs = File.Open(filePath, FileMode.Open, FileAccess.Read))
                {
                    for (int i = 0; i < numTiles; i++)
                    {
                        if (fs.Read(header, 0, 12) != 12) return null;
                        int magic = BitConverter.ToInt32(header, 0);
                        if (magic != int.MaxValue) return null;
                        int compressedSize = BitConverter.ToInt32(header, 8);
                        if (compressedSize <= 0) return null;
                        int tileFileSize = 12 + compressedSize;
                        // Seek past the compressed data (we already read the 12-byte header)
                        fs.Seek(compressedSize, SeekOrigin.Current);
                        cumulativeOffset += tileFileSize;
                        offsets.Add(cumulativeOffset);
                    }
                }
                return offsets;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Deletes a cached VT tile file for a specific layer GUID.
        /// Best-effort; ignores errors.
        /// </summary>
        internal static void DeleteCachedTileFile(Hash128 guid, string vtTileFolderName, bool isCityAtlas = false)
        {
            try
            {
                string filePath = Path.Combine(GetVTTileFileDirectory(vtTileFolderName, isCityAtlas), $"{guid}.vtd");
                if (File.Exists(filePath)) File.Delete(filePath);
            }
            catch { /* best effort */ }
        }

        /// <summary>
        /// Cleans all .vtd files from the city-bound VT tile cache directory.
        /// Called on system init because city atlas data changes every session.
        /// Local and mod atlas tiles are persistent and NOT cleaned here.
        /// </summary>
        internal static void CleanCityVTTileFileDirectory()
        {
            try
            {
                string dir = WEAtlasesLibrary.CACHED_VT_TILES_CITY_FOLDER;
                if (Directory.Exists(dir))
                {
                    foreach (var file in Directory.GetFiles(dir, "*.vtd", SearchOption.AllDirectories))
                    {
                        File.Delete(file);
                    }
                }
            }
            catch { /* best effort */ }
        }

        /// <summary>
        /// Deletes an entire atlas subfolder from the persistent VT tile cache.
        /// Used when an atlas's checksum changes and tiles need regeneration.
        /// </summary>
        internal static void CleanAtlasVTTileFolder(string vtTileFolderName, bool isCityAtlas = false)
        {
            try
            {
                string dir = GetVTTileFileDirectory(vtTileFolderName, isCityAtlas);
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, true);
                }
            }
            catch { /* best effort */ }
        }

        private static string GetVTTileFileDirectory(string vtTileFolderName, bool isCityAtlas = false)
        {
            string baseDir = isCityAtlas
                ? WEAtlasesLibrary.CACHED_VT_TILES_CITY_FOLDER
                : WEAtlasesLibrary.CACHED_VT_TILES_FOLDER;
            string dir = Path.Combine(baseDir, SanitizeFolderName(vtTileFolderName));
            Directory.CreateDirectory(dir);
            return dir;
        }

        private static string SanitizeFolderName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var chars = name.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (Array.IndexOf(invalid, chars[i]) >= 0) chars[i] = '_';
            }
            return new string(chars);
        }

        #endregion
    }
}
