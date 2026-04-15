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
        public const uint CURRENT_VERSION = 3;

        /// <summary>VT stack config index for DefaultPVTStack (basecolor, normal, mask).</summary>
        /// <remarks>
        /// <b>VT streaming memory analysis (vs. direct textures):</b>
        /// <para>Without VT: each atlas keeps 5 full BC7 textures in GPU VRAM.
        /// A 1024×1024 atlas = 5 × 1 MiB BC7 = 5 MiB VRAM per atlas.</para>
        /// <para>With VT: only visible tiles (512×512 + 8px padding) are resident.
        /// For a 1024×1024 atlas with ~25% screen coverage, typically 1–2 tiles
        /// per layer are resident ≈ 0.5–1 MiB per atlas (80–90% reduction).</para>
        /// <para>For 2048×2048 atlases (20 MiB each), savings are even larger
        /// since VT streams only the tiles the camera actually sees.</para>
        /// </remarks>
        internal const int VT_STACK_DEFAULT = 0;
        /// <summary>VT stack config index for ExtendedPVTStack (control, emissive).</summary>
        internal const int VT_STACK_EXTENDED = 1;

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

        // ── VT registration state ──────────────────────────────────────────────
        internal bool IsVTRegistered { get; private set; }
        internal VTAtlassingInfo VTAtlasInfoStack0 { get; private set; }
        internal VTAtlassingInfo VTAtlasInfoStack1 { get; private set; }
        internal VTTextureParamBlock VTParamBlock0 { get; private set; }
        internal VTTextureParamBlock VTParamBlock1 { get; private set; }
        internal Colossal.Hash128[] VTLayerGuids => m_vtLayerGuids;
        private Colossal.Hash128[] m_vtLayerGuids;
        private int m_vtRegistrationEpoch;
        private TextureStreamingSystem m_textureStreamingSystem;

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
                // Compress to BC7 for GPU-efficient savegame storage (version 3+).
                // NOTE: We do NOT replace textures here — BRI materials already hold references
                // to the RGBA32 textures (captured synchronously in PrimitiveRenderInformation
                // constructor). Replacing textures would destroy those references → white quads.
                // Serialization uses m_serializationOrder (byte arrays); textures stay RGBA32 for rendering.
                var bc7Main = WEAtlasBC7Utils.CompressToBC7(m_main, false);
                var bc7Emissive = WEAtlasBC7Utils.CompressToBC7(m_emissive, false);
                var bc7Control = WEAtlasBC7Utils.CompressToBC7(m_control, true);
                var bc7Mask = WEAtlasBC7Utils.CompressToBC7(m_mask, true);
                var bc7Normal = WEAtlasBC7Utils.CompressToBC7(m_normal, true);
                m_serializationOrder = new byte[][] { bc7Main, bc7Emissive, bc7Control, bc7Mask, bc7Normal };
                IsWritable = false;
            }

            IsApplied = true;
        }

        /// <summary>
        /// Compresses all 5 atlas layers to BC7 and writes a <see cref="WEAtlasCacheFile"/> to
        /// <paramref name="cacheFilePath"/>. Textures are intentionally left as RGBA32 so that
        /// BRI materials (which already reference them) continue to render correctly.
        /// On the next game startup, the atlas will be loaded directly from the BC7 cache.
        /// Must be called after <see cref="Apply()"/>.
        /// </summary>
        internal void WriteBC7CacheAndReplaceTextures(string cacheFilePath, uint checksum)
        {
            if (!IsApplied)
                throw new InvalidOperationException("Apply() must be called before WriteBC7CacheAndReplaceTextures.");

            var layers = new byte[]?[5];
            layers[0] = CompressLayer(m_main,     "main",     false);
            layers[1] = CompressLayer(m_emissive, "emissive", false);
            layers[2] = CompressLayer(m_control,  "control",  true);
            layers[3] = CompressLayer(m_mask,     "mask",     true);
            layers[4] = CompressLayer(m_normal,   "normal",   true);

            if (System.Array.Exists(layers, l => l is null))
                throw new InvalidOperationException("One or more atlas layers failed BC7 compression — see preceding log entries for details.");

            var spritesForCache = new System.Collections.Generic.List<Sprites.WEAtlasCacheFile.CachedSprite>(Sprites.Count);
            foreach (var s in Sprites.Values)
                spritesForCache.Add(new Sprites.WEAtlasCacheFile.CachedSprite(s.Name, s.Region, s.ExtraTextures));

            var cache = new Sprites.WEAtlasCacheFile(
                checksum, Width, Height, Size, Method, rectsPack,
                spritesForCache.AsReadOnly(), layers);
            cache.WriteTo(cacheFilePath);

            if (WillSerialize)
                m_serializationOrder = System.Array.ConvertAll(layers, l => l ?? System.Array.Empty<byte>());

            // Do NOT call ReplaceWithBC7 here — BRI materials already hold references
            // to the RGBA32 textures. Replacing them would destroy those references → white quads.
            // The BC7 data is persisted in the cache file; next startup loads directly from BC7.
            IsWritable = false;
        }

        private static byte[]? CompressLayer(Texture2D tex, string layerName, bool linear)
        {
            try
            {
                return WEAtlasBC7Utils.CompressToBC7(tex, linear);
            }
            catch (System.Exception ex)
            {
                Belzont.Utils.LogUtils.DoWarnLog($"[WETextureAtlas] BC7 compression failed for layer '{layerName}' ({tex.width}x{tex.height} {tex.format}): {ex.GetType().Name}: {ex.Message}");
                return null;
            }
        }

        private void ReplaceWithBC7(ref Texture2D tex, byte[] bc7Data, bool linear)
        {
            var name = tex.name;
            if (tex) GameObject.Destroy(tex);
            tex = WEAtlasBC7Utils.CreateFromBC7(Width, Height, bc7Data, linear);
            tex.name = name;
        }

        /// <summary>
        /// Replaces all 5 RGBA32 atlas textures with BC7 equivalents using the data
        /// already present in <see cref="m_serializationOrder"/>. After this call the
        /// atlas is non-writable and textures are GPU-only BC7. Used when migrating
        /// legacy (version &lt; 3) savegame data so that BRIs reference BC7 textures.
        /// </summary>
        private void ConvertToBC7InPlace()
        {
            if (m_serializationOrder is null || m_serializationOrder.Length < 5)
                throw new InvalidOperationException("m_serializationOrder must be populated before ConvertToBC7InPlace.");

            ReplaceWithBC7(ref m_main,     m_serializationOrder[0], false);
            ReplaceWithBC7(ref m_emissive, m_serializationOrder[1], false);
            ReplaceWithBC7(ref m_control,  m_serializationOrder[2], true);
            ReplaceWithBC7(ref m_mask,     m_serializationOrder[3], true);
            ReplaceWithBC7(ref m_normal,   m_serializationOrder[4], true);
            IsWritable = false;
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

            // Keep raw BC7 bytes for VT tile uploads (and for serialization if applicable).
            atlas.m_serializationOrder = new byte[][] { cache.LayerBC7[0]!, cache.LayerBC7[1]!, cache.LayerBC7[2]!, cache.LayerBC7[3]!, cache.LayerBC7[4]! };

            foreach (var sp in cache.Sprites)
            {
                var spriteInfo = new BelzontWE.Sprites.WESpriteInfo { Name = sp.Name, Region = sp.Region, ExtraTextures = sp.Flags };
                spriteInfo.CachedBRI = WERenderingHelper.GenerateBri(atlas, spriteInfo);
                atlas.Sprites[sp.Name] = spriteInfo;
            }

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
            DeregisterFromVT(m_textureStreamingSystem);
            // Destroy all owned textures regardless of readability.
            // Previously this skipped non-readable (BC7) textures, causing GPU memory leaks
            // when caches loaded via FromCacheFile were later disposed.
            if (m_main) GameObject.Destroy(m_main);
            if (m_emissive) GameObject.Destroy(m_emissive);
            if (m_control) GameObject.Destroy(m_control);
            if (m_mask) GameObject.Destroy(m_mask);
            if (m_normal) GameObject.Destroy(m_normal);
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

            bool isBC7 = version >= 3;
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
                if (isBC7)
                {
                    // Version 3+: raw BC7 bytes, create GPU textures directly
                    if (m_main) GameObject.Destroy(m_main);
                    m_main = WEAtlasBC7Utils.CreateFromBC7(Width, Height, bytesArrays[0].pngData, false);
                    m_main.name = "Main";
                    if (m_emissive) GameObject.Destroy(m_emissive);
                    m_emissive = WEAtlasBC7Utils.CreateFromBC7(Width, Height, bytesArrays[1].pngData, false);
                    m_emissive.name = "Emissive";
                    if (m_control) GameObject.Destroy(m_control);
                    m_control = WEAtlasBC7Utils.CreateFromBC7(Width, Height, bytesArrays[2].pngData, true);
                    m_control.name = "Control";
                    if (m_mask) GameObject.Destroy(m_mask);
                    m_mask = WEAtlasBC7Utils.CreateFromBC7(Width, Height, bytesArrays[3].pngData, true);
                    m_mask.name = "Mask";
                    if (m_normal) GameObject.Destroy(m_normal);
                    m_normal = WEAtlasBC7Utils.CreateFromBC7(Width, Height, bytesArrays[4].pngData, true);
                    m_normal.name = "Normal";
                    // Keep raw BC7 bytes for VT tile uploads and serialization.
                    m_serializationOrder = new byte[][] { bytesArrays[0].pngData, bytesArrays[1].pngData, bytesArrays[2].pngData, bytesArrays[3].pngData, bytesArrays[4].pngData };
                    IsWritable = false;
                    IsApplied = true;
                }
                else
                {
                    // Legacy (version < 3): PNG data → decode to RGBA32 staging, compress to BC7,
                    // then discard the RGBA32 textures so only BC7 lives in memory.
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
                    // Apply() to finalize RGBA32 + build m_serializationOrder as BC7 bytes
                    Apply();
                    // Now replace RGBA32 textures with BC7 so BRIs are created from BC7
                    ConvertToBC7InPlace();
                }
                foreach (var sprite in Sprites)
                {
                    if (BasicIMod.DebugMode) LogUtils.DoLog($"Calculating BRI for sprite {name}.{sprite.Key}");
                    sprite.Value.CachedBRI = WERenderingHelper.GenerateBri(this, sprite.Value);
                }
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
        public Material GenerateMaterial(WEShader shader, TextureStreamingSystem tss)
        {
            if (IsVTRegistered)
            {
                var material = WERenderingHelper.CreateDefaultMaterial(shader);
                if (material is null) return null;

                // Bind VT stacks (sets shader properties and notifies VT system)
                tss.BindMaterial(material, VTAtlasInfoStack0.stackGlobalIndex, VT_STACK_DEFAULT, VTParamBlock0);
                tss.BindMaterial(material, VTAtlasInfoStack1.stackGlobalIndex, VT_STACK_EXTENDED, VTParamBlock1);

                // Enable VT sampling in the shader
                material.EnableKeyword("ENABLE_VT");

                return material;
            }

            return WERenderingHelper.GenerateMaterial(shader, m_main, m_normal, m_mask, m_control, m_emissive);
        }

        /// <summary>
        /// Reserves rectangular regions in the game's VT atlas for this texture atlas.
        /// Stack 0 (<c>DefaultPVTStack</c>, 4 layers): main (L0), mask (L1), normal (L2), control (L3).
        /// Stack 1 (<c>ExtendedPVTStack</c>, 1 layer): emissive (L0).
        /// After successful reservation, <see cref="IsVTRegistered"/> is set to <c>true</c>
        /// and the param blocks are available via <see cref="VTParamBlock0"/>/<see cref="VTParamBlock1"/>.
        /// </summary>
        /// <param name="tss">The game's texture streaming system.</param>
        /// <returns><c>true</c> if both reservations succeeded; <c>false</c> on failure (atlas left unregistered).</returns>
        internal bool ReserveVTSpace(TextureStreamingSystem tss)
        {
            if (IsVTRegistered) return true;

            try
            {
                WEAtlasVTUtils.VTCrashLog($"[ReserveVTSpace] START {Width}x{Height}");
                var info0 = tss.ReserveTextureRect(VT_STACK_DEFAULT, Width, Height);
                WEAtlasVTUtils.VTCrashLog($"[ReserveVTSpace] DefaultPVT: stackGlobalIndex={info0.stackGlobalIndex} indexInStack={info0.indexInStack}");
                if (info0.stackGlobalIndex < 0 || info0.indexInStack < 0)
                {
                    Belzont.Utils.LogUtils.DoWarnLog($"[WETextureAtlas] VT reservation failed for DefaultPVTStack ({Width}x{Height}): stackGlobalIndex={info0.stackGlobalIndex}, indexInStack={info0.indexInStack}");
                    return false;
                }

                var info1 = tss.ReserveTextureRect(VT_STACK_EXTENDED, Width, Height);
                WEAtlasVTUtils.VTCrashLog($"[ReserveVTSpace] ExtendedPVT: stackGlobalIndex={info1.stackGlobalIndex} indexInStack={info1.indexInStack}");
                if (info1.stackGlobalIndex < 0 || info1.indexInStack < 0)
                {
                    Belzont.Utils.LogUtils.DoWarnLog($"[WETextureAtlas] VT reservation failed for ExtendedPVTStack ({Width}x{Height}): stackGlobalIndex={info1.stackGlobalIndex}, indexInStack={info1.indexInStack}");
                    return false;
                }

                VTAtlasInfoStack0 = info0;
                VTAtlasInfoStack1 = info1;
                VTParamBlock0 = tss.GetTextureParamBlock(info0);
                VTParamBlock1 = tss.GetTextureParamBlock(info1);
                IsVTRegistered = true;
                m_textureStreamingSystem = tss;
                return true;
            }
            catch (System.Exception ex)
            {
                Belzont.Utils.LogUtils.DoWarnLog($"[WETextureAtlas] VT reservation exception ({Width}x{Height}): {ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Uploads all 5 atlas layers to the VT streaming system after reservation.
        /// <list type="bullet">
        ///   <item>Stack 0 (DefaultPVTStack, 4 layers): main (L0, SRGB), mask (L1, SRGB), normal (L2, UNorm), control (L3, SRGB)</item>
        ///   <item>Stack 1 (ExtendedPVTStack, 1 layer): emissive (L0, SRGB)</item>
        /// </list>
        /// Requires <see cref="IsVTRegistered"/> and valid BC7 data in <c>m_serializationOrder</c>.
        /// </summary>
        /// <param name="tss">The game's texture streaming system.</param>
        /// <returns><c>true</c> if all 5 layers were uploaded; <c>false</c> on failure.</returns>
        internal bool UploadTilesToVT(TextureStreamingSystem tss)
        {
            if (!IsVTRegistered) return false;
            if (m_serializationOrder == null || m_serializationOrder.Length < 5) return false;
            if (System.Array.Exists(m_serializationOrder, layer => layer == null || layer.Length == 0)) return false;

            int tileSize = tss.tileSize;
            if (Belzont.Interfaces.BasicIMod.VerboseMode) Belzont.Utils.LogUtils.DoVerboseLog(
                "[WETextureAtlas] UploadTilesToVT: {0}x{1} tileSize={2} stack0={3}/{4} stack1={5}/{6}",
                Width, Height, tileSize,
                VTAtlasInfoStack0.stackGlobalIndex, VTAtlasInfoStack0.indexInStack,
                VTAtlasInfoStack1.stackGlobalIndex, VTAtlasInfoStack1.indexInStack);

            // Use epoch to ensure unique GUIDs across re-registrations of the same atlas.
            int guidSeed = GetHashCode() ^ m_vtRegistrationEpoch;
            m_vtRegistrationEpoch++;
            try
            {
                // Stack 0 (DefaultPVTStack, 4 layers): game formats [BC7_SRGB, BC7_SRGB, BC7_UNorm, BC7_SRGB]
                // Layer mapping: L0=_BaseColorMap, L1=_MaskMap, L2=_NormalMap, L3=control
                // m_serializationOrder: [0]=main, [1]=emissive, [2]=control, [3]=mask, [4]=normal
                var guid0 = WEAtlasVTUtils.GenerateLayerGuid(guidSeed, VTAtlasInfoStack0.stackGlobalIndex, 0);
                WEAtlasVTUtils.UploadLayerToVT(tss, VTAtlasInfoStack0, 0, m_serializationOrder[0], Width, Height,
                    WEAtlasVTUtils.GetBC7Format(false), guid0, tileSize); // main → L0, SRGB

                var guid1 = WEAtlasVTUtils.GenerateLayerGuid(guidSeed, VTAtlasInfoStack0.stackGlobalIndex, 1);
                WEAtlasVTUtils.UploadLayerToVT(tss, VTAtlasInfoStack0, 1, m_serializationOrder[3], Width, Height,
                    WEAtlasVTUtils.GetBC7Format(false), guid1, tileSize); // mask → L1, SRGB

                var guid2 = WEAtlasVTUtils.GenerateLayerGuid(guidSeed, VTAtlasInfoStack0.stackGlobalIndex, 2);
                WEAtlasVTUtils.UploadLayerToVT(tss, VTAtlasInfoStack0, 2, m_serializationOrder[4], Width, Height,
                    WEAtlasVTUtils.GetBC7Format(true), guid2, tileSize); // normal → L2, UNorm

                var guid3 = WEAtlasVTUtils.GenerateLayerGuid(guidSeed, VTAtlasInfoStack0.stackGlobalIndex, 3);
                WEAtlasVTUtils.UploadLayerToVT(tss, VTAtlasInfoStack0, 3, m_serializationOrder[2], Width, Height,
                    WEAtlasVTUtils.GetBC7Format(false), guid3, tileSize); // control → L3, SRGB

                // Invalidate Stack 0 region ONCE after all 4 layers are committed
                // (matches game's TexturesAsyncLoader.CompleteIfReady pattern)
                WEAtlasVTUtils.VTCrashLog($"[UploadTilesToVT] InvalidateRegion stack0={VTAtlasInfoStack0.stackGlobalIndex} idx={VTAtlasInfoStack0.indexInStack}");
                tss.InvalidateRegion(VTAtlasInfoStack0.stackGlobalIndex, VTAtlasInfoStack0.indexInStack);
                WEAtlasVTUtils.VTCrashLog($"[UploadTilesToVT] InvalidateRegion stack0 done");

                // Stack 1 (ExtendedPVTStack, 1 layer only): emissive=L0
                var guid4 = WEAtlasVTUtils.GenerateLayerGuid(guidSeed, VTAtlasInfoStack1.stackGlobalIndex, 0);
                WEAtlasVTUtils.UploadLayerToVT(tss, VTAtlasInfoStack1, 0, m_serializationOrder[1], Width, Height,
                    WEAtlasVTUtils.GetBC7Format(false), guid4, tileSize); // emissive → L0, SRGB

                // Invalidate Stack 1 region
                WEAtlasVTUtils.VTCrashLog($"[UploadTilesToVT] InvalidateRegion stack1={VTAtlasInfoStack1.stackGlobalIndex} idx={VTAtlasInfoStack1.indexInStack}");
                tss.InvalidateRegion(VTAtlasInfoStack1.stackGlobalIndex, VTAtlasInfoStack1.indexInStack);
                WEAtlasVTUtils.VTCrashLog($"[UploadTilesToVT] InvalidateRegion stack1 done");

                m_vtLayerGuids = new[] { guid0, guid1, guid2, guid3, guid4 };

                if (Belzont.Interfaces.BasicIMod.VerboseMode) Belzont.Utils.LogUtils.DoVerboseLog(
                    "[WETextureAtlas] VT upload complete ({0}x{1}): 5 layers committed + 2 regions invalidated", Width, Height);

                return true;
            }
            catch (System.Exception ex)
            {
                Belzont.Utils.LogUtils.DoWarnLog($"[WETextureAtlas] VT upload exception ({Width}x{Height}): {ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Deregisters this atlas from the VT streaming system.
        /// Invalidates VT regions for both stacks, resets all VT state, and clears stored GUIDs.
        /// <para>
        /// Note: the game provides no public API to release reserved VT atlas rects.
        /// This method invalidates the regions (so the GPU stops requesting tiles) and
        /// clears internal state. The VT rect slots are effectively leaked but this
        /// matches the game's own <c>SurfaceAsset.ClearVTAtlassingInfos</c> pattern.
        /// </para>
        /// </summary>
        /// <param name="tss">The game's texture streaming system (may be null during shutdown).</param>
        internal void DeregisterFromVT(TextureStreamingSystem tss)
        {
            if (!IsVTRegistered) return;

            try
            {
                if (tss != null)
                {
                    // Invalidate GPU tile regions so they stop being requested
                    tss.InvalidateRegion(VTAtlasInfoStack0.stackGlobalIndex, VTAtlasInfoStack0.indexInStack);
                    tss.InvalidateRegion(VTAtlasInfoStack1.stackGlobalIndex, VTAtlasInfoStack1.indexInStack);
                }
            }
            catch (System.Exception ex)
            {
                Belzont.Utils.LogUtils.DoWarnLog($"[WETextureAtlas] VT deregistration exception ({Width}x{Height}): {ex.GetType().Name}: {ex.Message}");
            }

            // Clean up cached tile files for re-streaming
            if (m_vtLayerGuids != null)
            {
                foreach (var guid in m_vtLayerGuids)
                {
                    WEAtlasVTUtils.DeleteCachedTileFile(guid);
                }
            }

            // Reset all VT state
            IsVTRegistered = false;
            VTAtlasInfoStack0 = default;
            VTAtlasInfoStack1 = default;
            VTParamBlock0 = default;
            VTParamBlock1 = default;
            m_vtLayerGuids = null;
            m_textureStreamingSystem = null;
        }
    }
}