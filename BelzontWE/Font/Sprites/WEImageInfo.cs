using Belzont.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using WriteEverywhere.Sprites;

namespace WriteEverywhere.Layout
{
    public class WEImageInfo : IDisposable
    {
        public const int MAX_SIZE_IMAGE_IMPORT = 2048;
        public const string CONTROL_MASK_MAP_EXTENSION = "_ControlMask.png";
        public const string NORMAL_MAP_EXTENSION = "_Normal.png";
        public const string EMISSIVE_MAP_EXTENSION = "_Emissive.png";
        public const string MASK_MAP_EXTENSION = "_MaskMap.png";
        private static readonly string[] excludeFileSuffixes = new[]
        {
            CONTROL_MASK_MAP_EXTENSION,
            MASK_MAP_EXTENSION,
            NORMAL_MAP_EXTENSION,
            EMISSIVE_MAP_EXTENSION,
        };


        public WEImageInfo() { }
        public string Name { get; set; }
        public Texture2D Main { get; set; }
        public Texture2D ControlMask { get; set; }
        public Texture2D MaskMap { get; set; }
        public Texture2D Normal { get; set; }
        public Texture2D Emissive { get; set; }
        public void Dispose()
        {
            if (Main) GameObject.Destroy(Main);
            if (ControlMask) GameObject.Destroy(ControlMask);
            if (MaskMap) GameObject.Destroy(MaskMap);
            if (Normal) GameObject.Destroy(Normal);
            if (Emissive) GameObject.Destroy(Emissive);
        }

        internal void ExportAt(string targetDir)
        {
            KFileUtils.EnsureFolderCreation(targetDir);
            var baseName = Path.Combine(targetDir, string.Join("_", Name.Split(Path.GetInvalidFileNameChars())));
            File.WriteAllBytes($"{baseName}.png", Main.EncodeToPNG());
            if (ControlMask) File.WriteAllBytes($"{baseName}{CONTROL_MASK_MAP_EXTENSION}", ControlMask.EncodeToPNG());
            if (MaskMap) File.WriteAllBytes($"{baseName}{MASK_MAP_EXTENSION}", MaskMap.EncodeToPNG());
            if (Normal) File.WriteAllBytes($"{baseName}{NORMAL_MAP_EXTENSION}", Normal.EncodeToPNG());
            if (Emissive) File.WriteAllBytes($"{baseName}{EMISSIVE_MAP_EXTENSION}", Emissive.EncodeToPNG());
        }

        public static WEImageInfo CreateFromBaseImageFile(List<string> errors, string imgFile)
        {
            if (excludeFileSuffixes.Any(x => imgFile.EndsWith(x))) return null;
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
                    var imgName = Path.GetFileNameWithoutExtension(imgFile);
                    return new WEImageInfo()
                    {
                        Name = imgName,
                        Main = tex,
                        ControlMask = WEAtlasLoadingUtils.TryLoadTexture(imgFile.Replace(".png", CONTROL_MASK_MAP_EXTENSION), tex.width, tex.height),
                        Emissive = WEAtlasLoadingUtils.TryLoadTexture(imgFile.Replace(".png", EMISSIVE_MAP_EXTENSION), tex.width, tex.height),
                        MaskMap = WEAtlasLoadingUtils.TryLoadTexture(imgFile.Replace(".png", MASK_MAP_EXTENSION), tex.width, tex.height),
                        Normal = WEAtlasLoadingUtils.TryLoadTexture(imgFile.Replace(".png", NORMAL_MAP_EXTENSION), tex.width, tex.height),
                    };
                }
                else
                {
                    errors.Add($"{Path.GetFileName(imgFile)}: IMAGE TOO LARGE (max: {MAX_SIZE_IMAGE_IMPORT}x{MAX_SIZE_IMAGE_IMPORT})");
                    GameObject.Destroy(tex);
                }
            }
            else
            {
                errors.Add($"{Path.GetFileName(imgFile)}: FAILED LOADING IMAGE");
                GameObject.Destroy(tex);
            }
            return null;
        }
    }
}
