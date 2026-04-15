using Colossal.IO.AssetDatabase.VirtualTexturing;
using System;
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
        /// Default VT tile size used by the game's VT system (pixels per tile edge).
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
        internal static void ValidatePreprocessInputs(byte[] bc7Data, int width, int height)
        {
            if (bc7Data == null) throw new ArgumentNullException(nameof(bc7Data));
            if (width < VT_TILE_SIZE)
                throw new ArgumentOutOfRangeException(nameof(width), $"Atlas width ({width}) must be ≥ {VT_TILE_SIZE}.");
            if (height < VT_TILE_SIZE)
                throw new ArgumentOutOfRangeException(nameof(height), $"Atlas height ({height}) must be ≥ {VT_TILE_SIZE}.");
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
        /// <param name="width">Atlas width in pixels (must be power-of-two, ≥ 512).</param>
        /// <param name="height">Atlas height in pixels (must be power-of-two, ≥ 512).</param>
        /// <param name="format">
        ///   <see cref="GraphicsFormat.RGBA_BC7_SRGB"/> for sRGB layers (main, emissive) or
        ///   <see cref="GraphicsFormat.RGBA_BC7_UNorm"/> for linear layers (normal, mask, control).
        /// </param>
        /// <returns>VT-tiled byte data ready for registration into the streaming system.</returns>
        public static NativeArray<byte> PreprocessForVT(byte[] bc7Data, int width, int height, GraphicsFormat format)
        {
            ValidatePreprocessInputs(bc7Data, width, height);

            var layerInfo = new AtlassingUtils.LayerInfo(VT_TILE_SIZE, format);

            // maxLevel = number of mip subdivisions within the tile structure.
            // With mip0-only data, maxLevel = log2(min(w,h) / tileSize).
            int maxLevel = (int)Math.Log(Math.Min(width, height) / (double)VT_TILE_SIZE, 2.0);

            // Wrap raw bytes in a NativeArray for the NativeSlice API.
            var input = new NativeArray<byte>(bc7Data, Allocator.TempJob);
            try
            {
                var inputSlice = new NativeSlice<byte>(input);
                AtlassingUtils.PreProcessData(inputSlice, out var processedData, width, height, VT_TILE_SIZE, maxLevel, VT_PADDING, layerInfo);
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
        ///   <c>true</c> for linear layers (normal, mask, control);
        ///   <c>false</c> for sRGB layers (main, emissive).
        /// </param>
        public static GraphicsFormat GetBC7Format(bool linear)
            => linear ? GraphicsFormat.RGBA_BC7_UNorm : GraphicsFormat.RGBA_BC7_SRGB;

        /// <summary>
        /// Computes the total number of VT tiles for a given texture at the specified
        /// max level. Useful for validating preprocessed output sizes.
        /// </summary>
        public static int GetTileCount(int width, int height, int maxLevel)
            => AtlassingUtils.TextureRelativeTileIndex(width, height, maxLevel + 1, 0, 0, VT_TILE_SIZE);

        /// <summary>
        /// Computes the expected preprocessed byte count for a given atlas layer.
        /// </summary>
        public static int GetPreprocessedByteCount(int width, int height, GraphicsFormat format)
        {
            var layerInfo = new AtlassingUtils.LayerInfo(VT_TILE_SIZE, format);
            int maxLevel = (int)Math.Log(Math.Min(width, height) / (double)VT_TILE_SIZE, 2.0);
            int tileCount = GetTileCount(width, height, maxLevel);
            return tileCount * (layerInfo.tileBlockSize + layerInfo.trilinearTileBlockSize) * layerInfo.blockSizeInBytes;
        }
    }
}
