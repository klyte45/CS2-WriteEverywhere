using BelzontWE.Commons.Utils.AssetPipeline;
using BelzontWE.Layout;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BelzontWE.Sprites
{
    public static class WEAtlasLoadingUtils
    {
        internal static ulong CalculateCheckshumForDirectory(string folder)
        {
            ulong checksum = 0;
            foreach (var imgFile in Directory.GetFiles(folder, "*.png"))
            {
                checksum ^= WEImageInfo.CalculateCheckshumFor(imgFile);
            }
            return checksum;
        }

        internal static void LoadAllImagesFromFolderRef(string folder, List<WEImageInfo> spritesToAdd, Action<string, string> onError)
        {
            foreach (var imgFile in Directory.GetFiles(folder, "*.png"))
            {
                var info = WEImageInfo.CreateFromBaseImageFile(onError, imgFile);
                if (info != null) spritesToAdd.Add(info);
            }
        }

        internal static void LoadAllImagesFromList(string[] files, List<WEImageInfo> spritesToAdd, Action<string, string> onError)
        {
            foreach (var imgFile in files)
            {
                var info = WEImageInfo.CreateFromBaseImageFile(onError, imgFile);
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

        // Extracted to KTextureLoadingUtils — delegating for backward compatibility.
        internal static Texture2D TryLoadTexture(string file, int width, int height)
            => KTextureLoadingUtils.TryLoadTexture(file, width, height);

        internal static Texture2D TryLoadTexture(byte[] contents, int width, int height)
            => KTextureLoadingUtils.TryLoadTexture(contents, width, height);
    }
}