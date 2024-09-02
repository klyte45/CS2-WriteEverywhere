using System.Collections.Generic;
using System.IO;
using UnityEngine;
using BelzontWE.Layout;

namespace BelzontWE.Sprites
{
    public static class WEAtlasLoadingUtils
    {
        public static void LoadAllImagesFromFolderRef(string folder, List<WEImageInfo> spritesToAdd, ref List<string> errors)
        {
            foreach (var imgFile in Directory.GetFiles(folder, "*.png"))
            {
                var info = WEImageInfo.CreateFromBaseImageFile(errors, imgFile);
                if (info != null) spritesToAdd.Add(info);
            }
        }

        public static Texture2D TryLoadTexture(string file, int width, int height)
        {
            if (!File.Exists(file)) return null;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (tex.LoadImage(File.ReadAllBytes(file)) && tex.width == width && tex.height == height)
            {
                return tex;
            }
            else
            {
                Object.Destroy(tex);
                return null;
            }
        }
    }
}