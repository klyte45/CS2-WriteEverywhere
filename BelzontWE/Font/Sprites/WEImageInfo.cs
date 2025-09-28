using Belzont.Utils;
using BelzontWE.Sprites;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace BelzontWE.Layout
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

        public static WEImageInfo CreateFromTuple(List<string> errors, (
            string Name,
            byte[] Main,
            byte[] ControlMask,
            byte[] MaskMap,
            byte[] Normal,
            byte[] Emissive,
            string XmlInfo) data)
        {
            if (data.Main == null) return null;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            //WEImageInfoXml xmlInfo = null;
            //if (!data.XmlInfo.IsNullOrWhitespace())
            //{
            //    xmlInfo = XmlUtils.DefaultXmlDeserialize<WEImageInfoXml>(data.XmlInfo);
            //}
            if (tex.LoadImage(data.Main))
            {
                if (tex.height <= MAX_SIZE_IMAGE_IMPORT && tex.width <= MAX_SIZE_IMAGE_IMPORT)
                {
                    return new WEImageInfo()
                    {
                        Name = data.Name,
                        Main = tex,
                        ControlMask = WEAtlasLoadingUtils.TryLoadTexture(data.ControlMask, tex.width, tex.height),
                        Emissive = WEAtlasLoadingUtils.TryLoadTexture(data.Emissive, tex.width, tex.height),
                        MaskMap = WEAtlasLoadingUtils.TryLoadTexture(data.MaskMap, tex.width, tex.height),
                        Normal = WEAtlasLoadingUtils.TryLoadTexture(data.Normal, tex.width, tex.height),
                    };
                }
                else
                {
                    errors.Add($"{data.Name}: IMAGE TOO LARGE (max: {MAX_SIZE_IMAGE_IMPORT}x{MAX_SIZE_IMAGE_IMPORT}, have: {tex.width}x{tex.height})");
                    GameObject.Destroy(tex);
                }
            }
            else
            {
                errors.Add($"{Path.GetFileName(data.Name)}: FAILED LOADING IMAGE");
                GameObject.Destroy(tex);
            }
            return null;
        }

        public static WEImageInfo CreateFromBaseImageFile(Action<string, string> onError, string imgFile)
        {
            if (!imgFile.EndsWith(".png")) return null;
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
                if (tex.height <= MAX_SIZE_IMAGE_IMPORT && tex.width <= MAX_SIZE_IMAGE_IMPORT)
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
                    onError(string.Join("/", imgFile.Split(Path.DirectorySeparatorChar)[^2..]), $"IMAGE TOO LARGE (max: {MAX_SIZE_IMAGE_IMPORT}x{MAX_SIZE_IMAGE_IMPORT}, have: {tex.width}x{tex.height})");
                    GameObject.Destroy(tex);
                }
            }
            else
            {
                onError(string.Join("/", imgFile.Split(Path.DirectorySeparatorChar)[^2..]), "FAILED LOADING IMAGE");
                GameObject.Destroy(tex);
            }
            return null;
        }



        public static ulong CalculateCheckshumFor(string imgFile)
        {
            if (!imgFile.EndsWith(".png")) return 0;
            if (excludeFileSuffixes.Any(x => imgFile.EndsWith(x))) return 0;

            var checksum = GetChecksumInfoForImageFile(imgFile);

            checksum ^= GetChecksumInfoForImageFile(imgFile.Replace(".png", CONTROL_MASK_MAP_EXTENSION));
            checksum ^= GetChecksumInfoForImageFile(imgFile.Replace(".png", EMISSIVE_MAP_EXTENSION));
            checksum ^= GetChecksumInfoForImageFile(imgFile.Replace(".png", MASK_MAP_EXTENSION));
            checksum ^= GetChecksumInfoForImageFile(imgFile.Replace(".png", NORMAL_MAP_EXTENSION));
            return checksum;
        }

        public static ulong GetChecksumInfoForImageFile(string imgFile)
        {
            if (File.Exists(imgFile))
            {
                var fileInfo = new FileInfo(imgFile);
                ulong size = (ulong)fileInfo.Length << 25;
                ulong lastWrite = (ulong)fileInfo.LastWriteTimeUtc.ToFileTimeUtc();
                ulong nameHash = (ulong)imgFile.GetHashCode();
                return size ^ lastWrite ^ nameHash;
            }
            return 0;
        }
    }
}
