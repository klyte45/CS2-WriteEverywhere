﻿using Belzont.Interfaces;
using Belzont.Utils;
using BelzontWE.Font.Utility;
using Colossal.Serialization.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using WriteEverywhere.Layout;
using WriteEverywhere.Sprites;
using HeuristicMethod = MaxRectsBinPack.FreeRectChoiceHeuristic;

namespace BelzontWE.Font
{
    public class WETextureAtlas : IDisposable, ISerializable
    {
        public const uint CURRENT_VERSION = 0;
        public int Width => Main.width;
        public int Height => Main.height;
        public Dictionary<FixedString32Bytes, WESpriteInfo> Sprites { get; } = new();
        public Texture2D Main { get; set; }
        public Texture2D Emissive { get; set; }
        public Texture2D Control { get; set; }
        public Texture2D Mask { get; set; }
        public Texture2D Normal { get; set; }
        public uint Version { get; private set; }
        public bool IsApplied { get; private set; }
        public HeuristicMethod Method { get; private set; }
        public float Occupancy => rectsPack.Occupancy();
        public int Count => Sprites.Count;

        public IEnumerable<FixedString32Bytes> Keys => Sprites.Keys;

        private MaxRectsBinPack rectsPack;
        private WETextureAtlas() : this(width: 2, height: 2) { }
        public WETextureAtlas(int size) : this(width: size, height: size) { }

        public WETextureAtlas(int width, int height, HeuristicMethod method = HeuristicMethod.RectBestShortSideFit)
        {
            Main = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Emissive = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Control = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Mask = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Normal = new Texture2D(width, height, TextureFormat.RGBA32, false);
            this.Method = method;
            rectsPack = new MaxRectsBinPack(width, height, false);
        }

        #region Write

        internal int Insert(WEImageInfo entry)
        {
            return Insert(entry.Name, entry.Texture, entry.Emissive, entry.ControlMask, entry.MaskMap, entry.Normal);
        }
        public int Insert(string spriteName, Texture2D main, Texture2D emissive = null, Texture2D control = null, Texture2D mask = null, Texture2D normal = null)
        {
            if (spriteName == null || Sprites.ContainsKey(spriteName)) return 1;
            var spriteInfo = Write(main, emissive, control, mask, normal);
            if (spriteInfo == null) return 2;
            spriteInfo.Name = spriteName;
            spriteInfo.CachedBRI = WERenderingHelper.GenerateBri(spriteName, this, spriteInfo);
            Sprites[spriteName] = spriteInfo;
            return 0;
        }

        public void Apply()
        {
            Main.Apply();
            Emissive.Apply();
            Control.Apply();
            Mask.Apply();
            Normal.Apply();
            IsApplied = true;
        }

        private WESpriteInfo Write(Texture2D main, Texture2D emissive, Texture2D control, Texture2D mask, Texture2D normal)
        {
            Rect newRect = rectsPack.Insert(main.width, main.height, Method);
            if (newRect.height == 0)
                return default;

            Main.SetPixels((int)newRect.x, (int)newRect.y, (int)newRect.width, (int)newRect.height, main.GetPixels());
            var spriteInfo = new WESpriteInfo
            {
                Region = newRect,
                HasEmissive = emissive && emissive.width == main.width && emissive.height == main.height,
                HasControl = control && control.width == main.width && control.height == main.height,
                HasMask = mask && mask.width == main.width && mask.height == main.height,
                HasNormal = normal && normal.width == main.width && normal.height == main.height,
            };
            if (spriteInfo.HasEmissive) Emissive.SetPixels((int)newRect.x, (int)newRect.y, (int)newRect.width, (int)newRect.height, emissive.GetPixels());
            if (spriteInfo.HasControl) Control.SetPixels((int)newRect.x, (int)newRect.y, (int)newRect.width, (int)newRect.height, control.GetPixels());
            if (spriteInfo.HasMask) Mask.SetPixels((int)newRect.x, (int)newRect.y, (int)newRect.width, (int)newRect.height, mask.GetPixels());
            if (spriteInfo.HasNormal) Normal.SetPixels((int)newRect.x, (int)newRect.y, (int)newRect.width, (int)newRect.height, normal.GetPixels());

            IsApplied = false;
            return spriteInfo;
        }


        #endregion

        #region Read
        public BasicRenderInformation this[string index] => Sprites.TryGetValue(index, out var spriteInfo) ? spriteInfo.CachedBRI : null;
        #endregion

        public void Dispose()
        {
            if (Main) GameObject.Destroy(Main);
            if (Emissive) GameObject.Destroy(Emissive);
            if (Control) GameObject.Destroy(Control);
            if (Mask) GameObject.Destroy(Mask);
            if (Normal) GameObject.Destroy(Normal);

            ClearSprites();
        }

        private void ClearSprites()
        {
            foreach (var spriteInfo in Sprites.Values)
            {
                spriteInfo.Dispose();
            }
            Sprites.Clear();
        }

        public bool GetAsSingleImage(string spriteName, out Texture2D main, out Texture2D emissive, out Texture2D control, out Texture2D mask, out Texture2D normal)
        {
            main = null;
            emissive = null;
            control = null;
            mask = null;
            normal = null;
            if (!Sprites.TryGetValue(spriteName ?? "", out var spriteInfo)) return false;
            var width = (int)spriteInfo.Region.size.x;
            var height = (int)spriteInfo.Region.size.y;
            var offsetX = (int)spriteInfo.Region.position.x;
            var offsetY = (int)spriteInfo.Region.position.y;

            main = new Texture2D(width, height, TextureFormat.RGBA32, false);
            main.SetPixels(Main.GetPixels(offsetX, offsetY, width, height));
            if (spriteInfo.HasControl)
            {
                control = new Texture2D(width, height, TextureFormat.RGBA32, false);
                control.SetPixels(Control.GetPixels(offsetX, offsetY, width, height));
            }
            if (spriteInfo.HasEmissive)
            {
                emissive = new Texture2D(width, height, TextureFormat.RGBA32, false);
                emissive.SetPixels(Emissive.GetPixels(offsetX, offsetY, width, height));
            }
            if (spriteInfo.HasMask)
            {
                mask = new Texture2D(width, height, TextureFormat.RGBA32, false);
                mask.SetPixels(Mask.GetPixels(offsetX, offsetY, width, height));
            }
            if (spriteInfo.HasNormal)
            {
                normal = new Texture2D(width, height, TextureFormat.RGBA32, false);
                normal.SetPixels(Normal.GetPixels(offsetX, offsetY, width, height));
            }
            return true;
        }

        public void InsertAll(WETextureAtlas other, bool overrideExisting = false)
        {
            foreach (var spriteInfo in other.Sprites.Values)
            {
                if (Sprites.TryGetValue(spriteInfo.Name, out var value))
                {
                    if (overrideExisting)
                    {
                        value.Dispose();
                        Sprites.Remove(spriteInfo.Name);
                    }
                    else
                    {
                        continue;
                    }
                }
                other.GetAsSingleImage(spriteInfo.Name, out var main, out var emissive, out var control, out var mask, out var normal);
                Insert(spriteInfo.Name, main, emissive, control, mask, normal);
                if (main) GameObject.Destroy(main);
                if (emissive) GameObject.Destroy(emissive);
                if (control) GameObject.Destroy(control);
                if (mask) GameObject.Destroy(mask);
                if (normal) GameObject.Destroy(normal);
            }
        }

        #region Serialization
        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(CURRENT_VERSION);
            writer.Write((int)Method);
            writer.Write(rectsPack);
            var textureOrder = new[] { Main, Emissive, Control, Mask, Normal };
            foreach (var tex in textureOrder)
            {
                if (!tex)
                {
                    writer.Write(0);
                    continue;
                }
                var mainTexBytes = new NativeArray<byte>(ZipUtils.ZipBytes(tex.EncodeToPNG()), Allocator.Temp);
                writer.Write(mainTexBytes.Length);
                writer.Write(mainTexBytes);
                mainTexBytes.Dispose();
            }
            writer.Write(Sprites.Count);
            foreach (var spriteInfo in Sprites)
            {
                writer.Write(spriteInfo.Value);
            }
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out uint version);
            if (version > CURRENT_VERSION)
            {
                LogUtils.DoWarnLog($"Invalid version for {GetType()}: {version}");
                return;
            }
            reader.Read(out int method);
            Method = (HeuristicMethod)method;
            rectsPack = new MaxRectsBinPack();
            reader.Read(rectsPack);
            var textureOrder = new[] { Main, Emissive, Control, Mask, Normal };
            for (int i = 0; i < textureOrder.Length; i++)
            {
                Texture2D tex = textureOrder[i];
                reader.Read(out int length);
                var texBytes = new NativeArray<byte>(length, Allocator.Temp);
                reader.Read(texBytes);
                if (!tex.LoadImage(ZipUtils.UnzipBytes(texBytes.ToArray())))
                {
                    return;
                }
                texBytes.Dispose();
            }
            ClearSprites();
            reader.Read(out int spriteCount);
            for (int i = 0; i < spriteCount; i++)
            {
                WESpriteInfo info = new();
                reader.Read(info);
                Sprites[info.Name] = info;
            }
        }

        #endregion
        public bool ContainsKey(FixedString32Bytes spriteName) => Sprites.ContainsKey(spriteName);

        public bool TryGetValue(FixedString32Bytes spriteName, out BasicRenderInformation cachedInfo)
        {
            if (Sprites.TryGetValue(spriteName, out var value))
            {
                cachedInfo = value.CachedBRI;
                return true;
            }
            else
            {
                cachedInfo = null;
                return false;
            }
        }

        internal void _SaveDebug(string atlasName)
        {
            var baseFolder = Path.Combine(BasicIMod.ModSettingsRootFolder, "_DebugAtlases", atlasName);
            KFileUtils.EnsureFolderCreation(baseFolder);
            File.WriteAllBytes(Path.Combine(baseFolder, "__Main.png"), Main.EncodeToPNG());
            File.WriteAllBytes(Path.Combine(baseFolder, "__Emissive.png"), Emissive.EncodeToPNG());
            File.WriteAllBytes(Path.Combine(baseFolder, "__Control.png"), Control.EncodeToPNG());
            File.WriteAllBytes(Path.Combine(baseFolder, "__Mask.png"), Mask.EncodeToPNG());
            File.WriteAllBytes(Path.Combine(baseFolder, "__Normal.png"), Normal.EncodeToPNG());
            File.WriteAllText(Path.Combine(baseFolder, "__AtlasData.xml"), XmlUtils.DefaultXmlSerialize(Sprites.Values.ToArray()));

        }
    }
}