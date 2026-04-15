using Belzont.Interfaces;
using Belzont.Utils;
using Colossal;
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

            int expectedBytes = WEAtlasBC7Utils.GetBC7SizeBytes(width, height);
            if (bc7Data.Length != expectedBytes)
                throw new ArgumentException($"Expected {expectedBytes} bytes for {width}×{height} BC7 data, got {bc7Data.Length}.", nameof(bc7Data));
        }

        internal static bool IsPowerOf2(int x) => x > 0 && (x & (x - 1)) == 0;

        /// <summary>
        /// Preprocesses raw BC7 bytes (mip0 only) into the game's VT tile layout
        /// using <see cref="AtlassingUtils.PreProcessData"/>.
        /// <para>
        /// The returned <see cref="NativeArray{T}"/> is allocated with
        /// <see cref="Allocator.Persistent"/> — the caller <b>must</b> dispose it
        /// after uploading the tile data to the VT system.
        /// </para>
        /// </summary>
        /// <param name="bc7Data">Raw BC7 block data for a single atlas layer (mip0).</param>
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
            // We only have mip0. Allocate a buffer large enough for all expected mip levels
            // and zero-fill the missing ones to prevent out-of-bounds native reads.
            int mipLevelsNeeded = maxLevel + 2;
            int totalSrcBytes = 0;
            int mipW = width, mipH = height;
            for (int m = 0; m < mipLevelsNeeded; m++)
            {
                totalSrcBytes += WEAtlasBC7Utils.GetBC7SizeBytes(mipW, mipH);
                mipW = Math.Max(4, mipW / 2);
                mipH = Math.Max(4, mipH / 2);
            }

            VTCrashLog($"[PreprocessForVT] {width}x{height} maxLevel={maxLevel} mipLevelsNeeded={mipLevelsNeeded} mip0={bc7Data.Length} totalSrc={totalSrcBytes}");

            // Allocate zeroed buffer for all mip levels; copy mip0 at offset 0.
            var input = new NativeArray<byte>(totalSrcBytes, Allocator.TempJob, NativeArrayOptions.ClearMemory);
            NativeArray<byte>.Copy(bc7Data, 0, input, 0, bc7Data.Length);
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
            GraphicsFormat format, Hash128 guid, int tileSize = VT_TILE_SIZE)
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

            try
            {
                // 2. Register buffer allocation in VT system
                VTCrashLog($"[UploadLayerToVT] Step2-RegisterVTTextureData {stepTag} guid={guid} dataSize={tileData.Length}");
                bool registered = tss.RegisterVTTextureData(guid, string.Empty, 0, 0, tileData.Length,
                    new List<int> { 0 }, width, height);
                VTCrashLog($"[UploadLayerToVT] Step2-Done {stepTag} registered={registered}");

                if (!registered)
                {
                    LogUtils.DoWarnLog($"[WEAtlasVTUtils] RegisterVTTextureData returned false for guid {guid} — skipping layer {layerIndex}");
                    return;
                }

                // 3. Copy preprocessed tiles into the registered buffer
                VTCrashLog($"[UploadLayerToVT] Step3-GetTextureData {stepTag}");
                var buffer = tss.GetTextureData(guid);
                VTCrashLog($"[UploadLayerToVT] Step3-GotBuffer {stepTag} bufLen={buffer.Length} tileLen={tileData.Length}");
                if (buffer.Length != tileData.Length)
                {
                    LogUtils.DoWarnLog($"[WEAtlasVTUtils] Buffer length mismatch: expected {tileData.Length}, got {buffer.Length}");
                }
                VTCrashLog($"[UploadLayerToVT] Step3-Copy {stepTag}");
                NativeArray<byte>.Copy(tileData, buffer);
                VTCrashLog($"[UploadLayerToVT] Step3-Done {stepTag}");
            }
            finally
            {
                // Dispose preprocessing output regardless of success
                tileData.Dispose();
            }

            // 4. Mark loading complete (enables AddTextureToCache)
            VTCrashLog($"[UploadLayerToVT] Step4-DoneLoading {stepTag}");
            tss.DoneLoading(guid);
            VTCrashLog($"[UploadLayerToVT] Step4-Done {stepTag}");

            // 5. Transfer tile data to GPU cache (disposes registered buffer internally)
            VTCrashLog($"[UploadLayerToVT] Step5-AddTextureToCache {stepTag}");
            tss.AddTextureToCache(atlasInfo.stackGlobalIndex, layerIndex,
                atlasInfo.indexInStack, width, height, guid, 0);
            VTCrashLog($"[UploadLayerToVT] Step5-Done {stepTag}");

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
    }
}
