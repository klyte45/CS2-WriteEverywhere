using BelzontWE.Layout;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BelzontWE.Sprites
{
    public static class WEAtlasLoadingUtils
    {
        internal static void LoadAllImagesFromFolderRef(string folder, List<WEImageInfo> spritesToAdd, List<string> errors)
        {
            foreach (var imgFile in Directory.GetFiles(folder, "*.png"))
            {
                var info = WEImageInfo.CreateFromBaseImageFile(errors, imgFile);
                if (info != null) spritesToAdd.Add(info);
            }
        }

        internal static void LoadAllImagesFromList(string[] files, List<WEImageInfo> spritesToAdd, List<string> errors)
        {
            foreach (var imgFile in files)
            {
                var info = WEImageInfo.CreateFromBaseImageFile(errors, imgFile);
                if (info != null) spritesToAdd.Add(info);
            }
        }
        internal static void LoadAllImagesFromList(
            (string Name, byte[] Main, byte[] ControlMask, byte[] MaskMap, byte[] Normal, byte[] Emissive, string XmlInfo)[] files,
            List<WEImageInfo> spritesToAdd, List<string> errors)
        {
            foreach (var imgFile in files)
            {
                var info = WEImageInfo.CreateFromTuple(errors, imgFile);
                if (info != null) spritesToAdd.Add(info);
            }
        }

        internal static Texture2D TryLoadTexture(string file, int width, int height)
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
        internal static Texture2D TryLoadTexture(byte[] contents, int width, int height)
        {
            if (contents == null || contents.Length == 0) return null;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (tex.LoadImage(contents) && tex.width == width && tex.height == height)
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