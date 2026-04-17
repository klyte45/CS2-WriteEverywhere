using BelzontWE.Commons.Utils.AssetPipeline;
using UnityEngine;

namespace BelzontWE
{
    // Compatibility shim: all logic moved to Belzont.Utils.KAtlasBC7Utils (Commons).
    public static class WEAtlasBC7Utils
    {
        public static int GetBC7SizeBytes(int width, int height) => KAtlasBC7Utils.GetBC7SizeBytes(width, height);
        public static unsafe byte[] CompressToBC7(Texture2D source, bool linear) => KAtlasBC7Utils.CompressToBC7(source, linear);
        public static byte[] CompressToBC7WithMipChain(Texture2D source, bool linear) => KAtlasBC7Utils.CompressToBC7WithMipChain(source, linear);
        public static Texture2D CreateFromBC7(int width, int height, byte[] bc7Data, bool linear) => KAtlasBC7Utils.CreateFromBC7(width, height, bc7Data, linear);
    }
}