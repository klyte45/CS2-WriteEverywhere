using Belzont.Interfaces;
using Belzont.Utils;
using BelzontWE.Layout;
using BelzontWE.Sprites;
using Colossal.IO.AssetDatabase.VirtualTexturing;
using Colossal.Serialization.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using Color = UnityEngine.Color;
using HeuristicMethod = MaxRectsBinPack.FreeRectChoiceHeuristic;

namespace BelzontWE.Font
{
    public class WETextureAtlas : IDisposable, ISerializable
    {
        public const uint CURRENT_VERSION = 2;
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Size { get; private set; }

        public Dictionary<FixedString32Bytes, WESpriteInfo> Sprites { get; } = [];

        public Texture2D Main_preview => m_main;

        public uint Version { get; set; }
        public bool IsApplied { get; private set; }
        public HeuristicMethod Method { get; private set; }
        public float Occupancy => rectsPack.Occupancy();
        public int Count => Sprites.Count;
        public bool WillSerialize { get; private set; }
        private byte[][] m_serializationOrder;
        public bool IsWritable { get; internal set; } = true;


        public IEnumerable<FixedString32Bytes> Keys => Sprites.Keys;

        private MaxRectsBinPack rectsPack;
        private Texture2D m_main;
        private Texture2D m_emissive;
        private Texture2D m_control;
        private Texture2D m_mask;
        private Texture2D m_normal;

        internal WETextureAtlas()
        {
            WillSerialize = true;
        }

        public WETextureAtlas(int size, HeuristicMethod method = HeuristicMethod.RectBestShortSideFit, bool willSerialize = false)
        {
            if (size < 18 || size > 24)
            {
                throw new ArgumentOutOfRangeException(nameof(size), "Size must be between 18 (512x512) and 24 (4096x4096, inclusive). This is to ensure the atlas is not too small or too large for practical use.");
            }

            Size = size;
            Width = 1 << Mathf.FloorToInt(size / 2f);
            Height = 1 << Mathf.CeilToInt(size / 2f);
            m_main = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            m_emissive = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            m_control = new Texture2D(Width, Height, TextureFormat.RGBA32, false, true);
            m_mask = new Texture2D(Width, Height, TextureFormat.RGBA32, false, true);
            m_normal = new Texture2D(Width, Height, TextureFormat.RGBA32, false, true);
            var pixelsToSet = new Color[Width * Height];
            m_main.SetPixels(pixelsToSet);
            m_main.name = "Main";
            m_emissive.SetPixels(pixelsToSet);
            m_emissive.name = "Emissive";
            m_control.SetPixels(pixelsToSet);
            m_control.name = "Control";
            m_mask.SetPixels(pixelsToSet);
            m_mask.name = "Mask";
            m_normal.SetPixels([.. pixelsToSet.Select(x => new Color(.5f, .5f, 1f))]);
            m_normal.name = "Normal";
            Method = method;
            rectsPack = new MaxRectsBinPack(Width, Height, false);
            WillSerialize = willSerialize;
        }

        #region Write

        internal int Insert(WEImageInfo entry) => Insert(entry.Name, entry.Main, entry.Emissive, entry.ControlMask, entry.MaskMap, entry.Normal);

        public int InsertAndApply(string spriteName, Texture2D main, Texture2D emissive = null, Texture2D control = null, Texture2D mask = null, Texture2D normal = null)
        {
            var result = Insert(spriteName, main, emissive, control, mask, normal);
            if (result == 0) Apply();
            return result;
        }

        public int Insert(string spriteName, Texture2D main, Texture2D emissive = null, Texture2D control = null, Texture2D mask = null, Texture2D normal = null)
        {
            if (spriteName == null || Sprites.ContainsKey(spriteName)) return 1;
            var spriteInfo = Write(main, emissive, control, mask, normal);
            if (spriteInfo == null) return 2;
            spriteInfo.Name = spriteName;
            spriteInfo.CachedBRI = WERenderingHelper.GenerateBri(this, spriteInfo);
            Sprites[spriteName] = spriteInfo;
            return 0;
        }

        public void Apply()
        {
            if (!IsWritable) return;
            m_main.Apply();
            m_emissive.Apply();
            m_control.Apply();
            m_mask.Apply();
            m_normal.Apply();
            if (WillSerialize)
            {
                m_serializationOrder = new byte[][]
                {
                    m_main.EncodeToPNG(),
                    m_emissive.EncodeToPNG(),
                    m_control.EncodeToPNG(),
                    m_mask.EncodeToPNG(),
                    m_normal.EncodeToPNG(),
                };
            }

            IsApplied = true;
        }

        /// <summary>
        /// Compresses all 5 atlas layers to BC7, writes a <see cref="WEAtlasCacheFile"/> to
        /// <paramref name="cacheFilePath"/>, updates the serialization order with BC7 data,
        /// and replaces the RGBA32 <see cref="Texture2D"/> instances with GPU-only BC7 textures.
        /// Must be called after <see cref="Apply()"/>.
        /// </summary>
        internal void WriteBC7CacheAndReplaceTextures(string cacheFilePath, uint checksum)
        {
            if (!IsApplied)
                throw new InvalidOperationException("Apply() must be called before WriteBC7CacheAndReplaceTextures.");

            var layers = new byte[]?[5];
            layers[0] = WEAtlasBC7Utils.CompressToBC7(m_main, false);     // sRGB
            layers[1] = WEAtlasBC7Utils.CompressToBC7(m_emissive, false); // sRGB
            layers[2] = WEAtlasBC7Utils.CompressToBC7(m_control, true);   // linear
            layers[3] = WEAtlasBC7Utils.CompressToBC7(m_mask, true);      // linear
            layers[4] = WEAtlasBC7Utils.CompressToBC7(m_normal, true);    // linear

            var spritesForCache = new System.Collections.Generic.List<Sprites.WEAtlasCacheFile.CachedSprite>(Sprites.Count);
            foreach (var s in Sprites.Values)
                spritesForCache.Add(new Sprites.WEAtlasCacheFile.CachedSprite(s.Name, s.Region, s.ExtraTextures));

            var cache = new Sprites.WEAtlasCacheFile(
                checksum, Width, Height, Size, Method, rectsPack,
                spritesForCache.AsReadOnly(), layers);
            cache.WriteTo(cacheFilePath);

            if (WillSerialize)
                m_serializationOrder = System.Array.ConvertAll(layers, l => l ?? System.Array.Empty<byte>());

            ReplaceWithBC7(ref m_main, layers[0]!, false);
            ReplaceWithBC7(ref m_emissive, layers[1]!, false);
            ReplaceWithBC7(ref m_control, layers[2]!, true);
            ReplaceWithBC7(ref m_mask, layers[3]!, true);
            ReplaceWithBC7(ref m_normal, layers[4]!, true);

            IsWritable = false;
        }

        private void ReplaceWithBC7(ref Texture2D tex, byte[] bc7Data, bool linear)
        {
            var name = tex.name;
            if (tex.isReadable) GameObject.Destroy(tex);
            tex = WEAtlasBC7Utils.CreateFromBC7(Width, Height, bc7Data, linear);
            tex.name = name;
        }

        /// <summary>
        /// Reconstructs a <see cref="WETextureAtlas"/> from a pre-compressed <see cref="Sprites.WEAtlasCacheFile"/>
        /// without re-encoding PNGs. All 5 layer textures are created from the BC7 data in the cache.
        /// Requires a Unity GPU context (uses <see cref="WEAtlasBC7Utils.CreateFromBC7"/>).
        /// </summary>
        internal static WETextureAtlas FromCacheFile(Sprites.WEAtlasCacheFile cache)
        {
            var atlas = new WETextureAtlas
            {
                Width = cache.Width,
                Height = cache.Height,
                Size = cache.Size,
                Method = cache.Method,
                rectsPack = cache.RebuildRectsPack(),
                WillSerialize = false,
                IsApplied = true,
                IsWritable = false,
            };

            atlas.m_main = WEAtlasBC7Utils.CreateFromBC7(cache.Width, cache.Height, cache.LayerBC7[0]!, false);
            atlas.m_main.name = "Main";
            atlas.m_emissive = WEAtlasBC7Utils.CreateFromBC7(cache.Width, cache.Height, cache.LayerBC7[1]!, false);
            atlas.m_emissive.name = "Emissive";
            atlas.m_control = WEAtlasBC7Utils.CreateFromBC7(cache.Width, cache.Height, cache.LayerBC7[2]!, true);
            atlas.m_control.name = "Control";
            atlas.m_mask = WEAtlasBC7Utils.CreateFromBC7(cache.Width, cache.Height, cache.LayerBC7[3]!, true);
            atlas.m_mask.name = "Mask";
            atlas.m_normal = WEAtlasBC7Utils.CreateFromBC7(cache.Width, cache.Height, cache.LayerBC7[4]!, true);
            atlas.m_normal.name = "Normal";

            foreach (var sp in cache.Sprites)
                atlas.Sprites[sp.Name] = new BelzontWE.Sprites.WESpriteInfo { Name = sp.Name, Region = sp.Region, ExtraTextures = sp.Flags };

            return atlas;
        }

        private WESpriteInfo Write(Texture2D newMain, Texture2D newEmissive, Texture2D newControl, Texture2D newMask, Texture2D newNormal)
        {
            var offset = rectsPack.usedRectangles.Count == 0 ? 0 : 2;
            Rect newRect = rectsPack.Insert(newMain.width + offset, newMain.height + offset, Method);
            if (newRect.height == 0)
                return default;

            newRect.xMin += offset / 2;
            newRect.xMax -= offset / 2;
            newRect.yMin += offset / 2;
            newRect.yMax -= offset / 2;

            m_main.SetPixels((int)newRect.x, (int)newRect.y, (int)newRect.width, (int)newRect.height, newMain.GetPixels());
            var spriteInfo = new WESpriteInfo
            {
                Region = newRect,
                HasEmissive = newEmissive && newEmissive.width == newMain.width && newEmissive.height == newMain.height,
                HasControl = newControl && newControl.width == newMain.width && newControl.height == newMain.height,
                HasMaskMap = newMask && newMask.width == newMain.width && newMask.height == newMain.height,
                HasNormal = newNormal && newNormal.width == newMain.width && newNormal.height == newMain.height,
            };
            m_emissive.SetPixels((int)newRect.x, (int)newRect.y, (int)newRect.width, (int)newRect.height, spriteInfo.HasEmissive ? newEmissive.GetPixels() : newMain.GetPixels());
            m_control.SetPixels((int)newRect.x, (int)newRect.y, (int)newRect.width, (int)newRect.height, spriteInfo.HasControl ? newControl.GetPixels() : [.. new Color[(int)newRect.width * (int)newRect.height].Select(x => Color.clear)]);
            m_mask.SetPixels((int)newRect.x, (int)newRect.y, (int)newRect.width, (int)newRect.height, spriteInfo.HasMaskMap ? newMask.GetPixels() : [.. new Color[(int)newRect.width * (int)newRect.height].Select(x => Color.clear)]);
            m_normal.SetPixels((int)newRect.x, (int)newRect.y, (int)newRect.width, (int)newRect.height, spriteInfo.HasNormal ? newNormal.GetPixels() : [.. new Color[(int)newRect.width * (int)newRect.height].Select(x => new Color(.5f, .5f, 1f))]);

            IsApplied = false;
            return spriteInfo;
        }

        #endregion

        public void Dispose()
        {
            if (m_main && m_main.isReadable) GameObject.Destroy(m_main);
            if (m_emissive && m_emissive.isReadable) GameObject.Destroy(m_emissive);
            if (m_control && m_control.isReadable) GameObject.Destroy(m_control);
            if (m_mask && m_mask.isReadable) GameObject.Destroy(m_mask);
            if (m_normal && m_normal.isReadable) GameObject.Destroy(m_normal);
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
            var tempMain = m_main.MakeReadable(out var isCopy);
            main.SetPixels(tempMain.GetPixels(offsetX, offsetY, width, height));
            if (isCopy) GameObject.Destroy(tempMain);
            if (spriteInfo.HasControl)
            {
                control = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
                Texture2D texture2D = m_control.MakeReadable(out isCopy);
                control.SetPixels(texture2D.GetPixels(offsetX, offsetY, width, height));
                if (isCopy) GameObject.Destroy(texture2D);
            }

            if (spriteInfo.HasEmissive)
            {
                emissive = new Texture2D(width, height, TextureFormat.RGBA32, false);
                Texture2D texture2D = m_emissive.MakeReadable(out isCopy);
                emissive.SetPixels(texture2D.GetPixels(offsetX, offsetY, width, height));
                if (isCopy) GameObject.Destroy(texture2D);
            }

            if (spriteInfo.HasMaskMap)
            {
                mask = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
                Texture2D texture2D = m_mask.MakeReadable(out isCopy);
                mask.SetPixels(texture2D.GetPixels(offsetX, offsetY, width, height));
                if (isCopy) GameObject.Destroy(texture2D);
            }

            if (spriteInfo.HasNormal)
            {
                normal = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
                Texture2D texture2D = m_normal.MakeReadable(out isCopy);
                normal.SetPixels(texture2D.GetPixels(offsetX, offsetY, width, height));
                if (isCopy) GameObject.Destroy(texture2D);
            }

            return true;
        }

        public void InsertAll(WETextureAtlas other, bool overrideExisting = false)
        {
            if (!IsWritable)
            {
                throw new InvalidOperationException("This texture atlas is not writable.");
            }

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

            Apply();
        }

        #region Serialization

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            if (!WillSerialize) throw new NotSupportedException("This texture atlas isn't marked to serialize");
            if (!IsWritable) throw new InvalidOperationException("This texture atlas can't be serialized because it's VT atlas.");
            if (m_serializationOrder is null) throw new InvalidDataException("Texture atlas has no data to serialize. Forgot Apply()?");
            writer.Write(CURRENT_VERSION);
            writer.Write((int)Method);
            writer.Write(rectsPack);
            writer.Write(Width);
            writer.Write(Height);
            writer.Write(Size);
            foreach (var tex in m_serializationOrder)
            {
                var mainTexBytes = new NativeArray<byte>(tex, Allocator.Temp);
                writer.Write(mainTexBytes.Length);
                writer.Write(mainTexBytes);
                mainTexBytes.Dispose();
            }

            writer.Write(Sprites.Count);
            foreach (var spriteInfo in Sprites)
            {
                writer.Write(spriteInfo.Value);
            }

            writer.Write(Version);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            throw new InvalidOperationException("Use the method that returns the actions to load the images");
        }

        private struct ImageLoadInfo
        {
            public byte[] pngData;
        }

        public bool Deserialize<TReader>(TReader reader, FixedString32Bytes name, out Action imageLoadAction) where TReader : IReader
        {
            reader.Read(out uint version);
            if (version > CURRENT_VERSION)
            {
                LogUtils.DoWarnLog($"Invalid version for {GetType()}: {version}");
                imageLoadAction = null;
                return false;
            }

            reader.Read(out int method);
            Method = (HeuristicMethod)method;
            rectsPack = new MaxRectsBinPack();
            reader.Read(rectsPack);
            reader.Read(out int width);
            Width = width;
            reader.Read(out int height);
            Height = height;
            if (version >= 2)
            {
                reader.Read(out int size);
                Size = size;
            }
            else
            {
                Size = Convert.ToString(Width - 1, 2).Length + Convert.ToString(Height - 1, 2).Length;
            }

            var bytesArrays = new ImageLoadInfo[5];
            for (int i = 0; i < bytesArrays.Length; i++)
            {
                reader.Read(out int length);
                if (length == 0) continue;
                var texBytes = new NativeArray<byte>(length, Allocator.Temp);
                reader.Read(texBytes);
                bytesArrays[i].pngData = texBytes.ToArray();
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

            imageLoadAction = () =>
            {
                if (BasicIMod.DebugMode) LogUtils.DoLog($"Loading texture atlas '{name}'!");
                if (m_main) GameObject.Destroy(m_main);
                m_main = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
                m_main.LoadImage(bytesArrays[0].pngData);
                if (m_emissive) GameObject.Destroy(m_emissive);
                m_emissive = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
                m_emissive.LoadImage(bytesArrays[1].pngData);
                if (m_control) GameObject.Destroy(m_control);
                m_control = new Texture2D(Width, Height, TextureFormat.RGBA32, false, true);
                m_control.LoadImage(bytesArrays[2].pngData);
                if (m_mask) GameObject.Destroy(m_mask);
                m_mask = new Texture2D(Width, Height, TextureFormat.RGBA32, false, true);
                m_mask.LoadImage(bytesArrays[3].pngData);
                if (m_normal) GameObject.Destroy(m_normal);
                m_normal = new Texture2D(Width, Height, TextureFormat.RGBA32, false, true);
                m_normal.LoadImage(bytesArrays[4].pngData);
                foreach (var sprite in Sprites)
                {
                    if (BasicIMod.DebugMode) LogUtils.DoLog($"Calculating BRI for sprite {name}.{sprite.Key}");
                    sprite.Value.CachedBRI = WERenderingHelper.GenerateBri(this, sprite.Value);
                }

                Apply();
            };
            if (version < 1) return true;
            reader.Read(out uint versionAtlas);
            Version = versionAtlas;

            return true;
        }

        #endregion

        public bool ContainsKey(FixedString32Bytes spriteName) => Sprites.ContainsKey(spriteName);

        public bool TryGetValue(FixedString32Bytes spriteName, out IBasicRenderInformation cachedInfo)
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
            //var baseFolder = Path.Combine(BasicIMod.ModSettingsRootFolder, "_DebugAtlases", Regex.Replace(atlasName, $"[{new string(Path.GetInvalidFileNameChars())}]", "="));
            //KFileUtils.EnsureFolderCreation(baseFolder);
            //File.WriteAllBytes(Path.Combine(baseFolder, "__Main.png"), Main.MakeReadable().EncodeToPNG());
            //File.WriteAllBytes(Path.Combine(baseFolder, "__Emissive.png"), Emissive.MakeReadable().EncodeToPNG());
            //File.WriteAllBytes(Path.Combine(baseFolder, "__Control.png"), Control.MakeReadable().EncodeToPNG());
            //File.WriteAllBytes(Path.Combine(baseFolder, "__Mask.png"), Mask.MakeReadable().EncodeToPNG());
            //File.WriteAllBytes(Path.Combine(baseFolder, "__Normal.png"), Normal.MakeReadable().EncodeToPNG());
            //File.WriteAllText(Path.Combine(baseFolder, "__AtlasData.xml"), XmlUtils.DefaultXmlSerialize(Sprites.Values.ToArray()));
        }

        public WEImageInfo[] ToImageInfoArray()
        {
            return Sprites.Keys.Select(x =>
            {
                GetAsSingleImage(x.ToString(), out var main, out var emissive, out var control, out var mask, out var normal);
                return new WEImageInfo
                {
                    ControlMask = control,
                    Emissive = emissive,
                    MaskMap = mask,
                    Normal = normal,
                    Main = main,
                    Name = x.ToString()
                };
            }).ToArray();
        }

        internal void Init()
        {
        }
        public Material GenerateMaterial(WEShader shader, TextureStreamingSystem tss) => WERenderingHelper.GenerateMaterial(shader, m_main, m_normal, m_mask, m_control, m_emissive);
    }
}