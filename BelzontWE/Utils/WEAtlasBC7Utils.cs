using Belzont.Utils;
using Colossal.AssetPipeline.Native;
using System;
using UnityEngine;
using BlockCompressionFlags = Colossal.AssetPipeline.Native.NativeTextures.BlockCompressionFlags;
using BlockCompressionFormat = Colossal.AssetPipeline.Native.NativeTextures.BlockCompressionFormat;

namespace BelzontWE
{
    /// <summary>
    /// Utility methods for BC7 texture compression and decompression using the
    /// game's own native encoder (NativeTextures.BlockCompress via PipelinePlugin.dll).
    /// Requires the game runtime to be active (PipelinePlugin.dll loaded).
    /// </summary>
    public static class WEAtlasBC7Utils
    {
        /// <summary>
        /// Returns the byte count required to store a BC7-compressed texture of
        /// the given dimensions (4×4 blocks × 16 bytes each).
        /// </summary>
        public static int GetBC7SizeBytes(int width, int height)
            => ((width + 3) / 4) * ((height + 3) / 4) * 16;

        /// <summary>
        /// Compresses a readable RGBA32 <see cref="Texture2D"/> to raw BC7 bytes
        /// using the game's own CPU encoder (<c>NativeTextures.BlockCompress</c>,
        /// effort level 3).
        /// </summary>
        /// <param name="source">Must be RGBA32 format and CPU-readable.</param>
        /// <param name="linear">
        ///   <c>true</c> for linear/UNorm textures (normal maps, masks, control);
        ///   <c>false</c> for sRGB textures (basecolor, emissive).
        /// </param>
        /// <returns>Raw BC7 block data compatible with <c>AtlassingUtils.PreProcessData</c>.</returns>
        public static unsafe byte[] CompressToBC7(Texture2D source, bool linear)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            Texture2D toCompress = source;
            bool isTemp = false;
            if (source.format != TextureFormat.RGBA32)
            {
                var readable = source.MakeReadable(out _);
                toCompress = new Texture2D(readable.width, readable.height, TextureFormat.RGBA32, false, linear);
                toCompress.SetPixels(readable.GetPixels());
                toCompress.Apply(false, false);
                isTemp = true;
            }

            var raw = toCompress.GetRawTextureData();
            int width = toCompress.width;
            int height = toCompress.height;
            var dst = new byte[GetBC7SizeBytes(width, height)];

            // sRGB textures use perceptual quality; linear textures use no bias.
            var flags = linear ? BlockCompressionFlags.None : BlockCompressionFlags.Perceptual;

            fixed (byte* srcPtr = raw)
            fixed (byte* dstPtr = dst)
            {
                int result = NativeTextures.BlockCompress(
                    (IntPtr)srcPtr, width, height,
                    (IntPtr)dstPtr,
                    BlockCompressionFormat.BC7,
                    flags,
                    effort: 3);

                if (result != 0)
                    throw new InvalidOperationException($"NativeTextures.BlockCompress failed (code {result}).");
            }

            if (isTemp) UnityEngine.Object.Destroy(toCompress);

            return dst;
        }

        /// <summary>
        /// Creates a GPU-only <see cref="Texture2D"/> from raw BC7 block data.
        /// The returned texture has <c>makeNoLongerReadable = true</c> (no CPU copy).
        /// </summary>
        /// <param name="width">Texture width in pixels.</param>
        /// <param name="height">Texture height in pixels.</param>
        /// <param name="bc7Data">Raw BC7 bytes (output of <see cref="CompressToBC7"/>).</param>
        /// <param name="linear">
        ///   <c>true</c> for linear/UNorm; <c>false</c> for sRGB.
        /// </param>
        public static Texture2D CreateFromBC7(int width, int height, byte[] bc7Data, bool linear)
        {
            if (bc7Data == null) throw new ArgumentNullException(nameof(bc7Data));
            if (bc7Data.Length != GetBC7SizeBytes(width, height))
                throw new ArgumentException($"Expected {GetBC7SizeBytes(width, height)} bytes for {width}×{height} BC7 texture.", nameof(bc7Data));

            var tex = new Texture2D(width, height, TextureFormat.BC7, false, linear);
            tex.LoadRawTextureData(bc7Data);
            tex.Apply(false, makeNoLongerReadable: true);
            return tex;
        }
    }
}
