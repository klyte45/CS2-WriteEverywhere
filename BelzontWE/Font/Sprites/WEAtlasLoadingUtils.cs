using Belzont.Utils;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using WriteEverywhere.Layout;

namespace WriteEverywhere.Sprites
{
    public static class WEAtlasLoadingUtils
    {
        public const int MAX_SIZE_IMAGE_IMPORT = 2048;
        public const string CONTROL_MASK_MAP_EXTENSION = "_ControlMask.png";
        public const string NORMAL_MAP_EXTENSION = "_Normal.png";
        public const string EMISSIVE_MAP_EXTENSION = "_Emissive.png";
        public const string MASK_MAP_EXTENSION = "_MaskMap.png";


        public static void LoadAllImagesFromFolder(string folder, out List<WEImageInfo> spritesToAdd, out List<string> errors, bool addPrefix = true)
        {
            spritesToAdd = new List<WEImageInfo>();
            errors = new List<string>();
            LoadAllImagesFromFolderRef(folder, ref spritesToAdd, ref errors, addPrefix);
        }
        public static void LoadAllImagesFromFolderRef(string folder, ref List<WEImageInfo> spritesToAdd, ref List<string> errors, bool addPrefix)
        {
            var excludeFileSuffixes = new[]
            {
                CONTROL_MASK_MAP_EXTENSION,
                NORMAL_MAP_EXTENSION,
                EMISSIVE_MAP_EXTENSION,
                MASK_MAP_EXTENSION,
            };
            foreach (var imgFile in Directory.GetFiles(folder, "*.png"))
            {
                if (excludeFileSuffixes.Any(x => imgFile.EndsWith(x))) continue;
                var fileData = File.ReadAllBytes(imgFile);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                var xmlInfoFileName = imgFile.Replace(".png", "_info.xml");
                WEImageInfoXml xmlInfo = null;
                if (File.Exists(xmlInfoFileName))
                {
                    xmlInfo = XmlUtils.DefaultXmlDeserialize<WEImageInfoXml>(File.ReadAllText(xmlInfoFileName));
                }
                if (tex.LoadImage(fileData))
                {
                    if (tex.width <= MAX_SIZE_IMAGE_IMPORT && tex.width <= MAX_SIZE_IMAGE_IMPORT)
                    {
                        var imgName = addPrefix ? $"K45_WE_{Path.GetFileNameWithoutExtension(imgFile)}" : Path.GetFileNameWithoutExtension(imgFile);
                        spritesToAdd.Add(new WEImageInfo(xmlInfoFileName)
                        {
                            Borders = xmlInfo?.borders.ToWEBorder(tex.width, tex.height) ?? default,
                            Name = imgName,
                            Texture = tex,
                            ControlMask = TryLoadTexture(imgFile.Replace(".png", CONTROL_MASK_MAP_EXTENSION), tex.width, tex.height),
                            Emissive = TryLoadTexture(imgFile.Replace(".png", EMISSIVE_MAP_EXTENSION), tex.width, tex.height),
                            MaskMap = TryLoadTexture(imgFile.Replace(".png", MASK_MAP_EXTENSION), tex.width, tex.height),
                            Normal = TryLoadTexture(imgFile.Replace(".png", NORMAL_MAP_EXTENSION), tex.width, tex.height),
                            PixelsPerMeter = xmlInfo?.pixelsPerMeters ?? 100
                        });
                    }
                    else
                    {
                        errors.Add($"{Path.GetFileName(imgFile)}: IMAGE TOO LARGE (max: {MAX_SIZE_IMAGE_IMPORT}x{MAX_SIZE_IMAGE_IMPORT})");
                        Object.Destroy(tex);
                    }
                }
                else
                {
                    errors.Add($"{Path.GetFileName(imgFile)}: FAILED LOADING IMAGE");
                    Object.Destroy(tex);
                }
            }
        }

        private static Texture2D TryLoadTexture(string file, int width, int height)
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