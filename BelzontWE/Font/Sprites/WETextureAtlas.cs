using Belzont.Interfaces;
using Belzont.Utils;
using BelzontWE.AssetDatabases;
using BelzontWE.Layout;
using BelzontWE.Sprites;
using Colossal.IO.AssetDatabase;
using Colossal.IO.AssetDatabase.VirtualTexturing;
using Colossal.Serialization.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Assertions;
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

        public Dictionary<FixedString32Bytes, WESpriteInfo> Sprites { get; } = new();
        public Texture2D Main
        {
            get => IsWritable ? main : mainVT.Load() as Texture2D;
        }
        public Texture2D Emissive
        {
            get => IsWritable ? emissive : emissiveVT?.Load() as Texture2D;
        }
        public Texture2D Control
        {
            get => IsWritable ? control : controlVT?.Load() as Texture2D;
        }
        public Texture2D Mask
        {
            get => IsWritable ? mask : maskVT?.Load() as Texture2D;
        }
        public Texture2D Normal
        {
            get => IsWritable ? normal : normalVT?.Load() as Texture2D;
        }
        public uint Version { get; set; }
        public bool IsApplied { get; private set; }
        public HeuristicMethod Method { get; private set; }
        public float Occupancy => rectsPack.Occupancy();
        public int Count => Sprites.Count;
        public bool WillSerialize { get; private set; }
        private byte[][] m_serializationOrder;

        private ulong Checksum;

        public bool IsWritable { get; private set; } = true;

        public Material DefaultMaterial => defaultSurface?.Load();
        public Material GlassMaterial => glassSurface?.Load();
        public Material DecalMaterial => decalSurface?.Load();


        public IEnumerable<FixedString32Bytes> Keys => Sprites.Keys;

        private MaxRectsBinPack rectsPack;
        private Texture2D main;
        private Texture2D emissive;
        private Texture2D control;
        private Texture2D mask;
        private Texture2D normal;

        private VTTextureAsset mainVT;
        private VTTextureAsset emissiveVT;
        private VTTextureAsset controlVT;
        private VTTextureAsset maskVT;
        private VTTextureAsset normalVT;

        private SurfaceAsset defaultSurface;
        private SurfaceAsset glassSurface;
        private SurfaceAsset decalSurface;

        internal WETextureAtlas()
        {
            WillSerialize = true;
        }

        public WETextureAtlas(int size, HeuristicMethod method = HeuristicMethod.RectBestShortSideFit, bool willSerialize = false)
        {
            if (size < 18 || size > 28)
            {
                throw new ArgumentOutOfRangeException(nameof(size), "Size must be between 18 (512x512) and 28 (16384x16384, inclusive). This is to ensure the atlas is not too small or too large for practical use.");
            }
            Size = size;
            Width = 1 << Mathf.FloorToInt(size / 2f);
            Height = 1 << Mathf.CeilToInt(size / 2f);
            main = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            emissive = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            control = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            mask = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            normal = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            var pixelsToSet = new Color[Width * Height];
            main.SetPixels(pixelsToSet);
            main.name = "Main";
            emissive.SetPixels(pixelsToSet);
            emissive.name = "Emissive";
            control.SetPixels(pixelsToSet);
            control.name = "Control";
            mask.SetPixels(pixelsToSet);
            mask.name = "Mask";
            normal.SetPixels(pixelsToSet.Select(x => new Color(.5f, .5f, 1f)).ToArray());
            normal.name = "Normal";
            Method = method;
            rectsPack = new MaxRectsBinPack(Width, Height, false);
            WillSerialize = willSerialize;
        }

        public WETextureAtlas(XmlVTAtlasInfo atlasInfo, IAssetDatabase db)
        {
            IsWritable = false;
            mainVT = db.GetAsset<VTTextureAsset>(atlasInfo.MainTex);
            controlVT = db.GetAsset<VTTextureAsset>(atlasInfo.ControlMap);
            emissiveVT = db.GetAsset<VTTextureAsset>(atlasInfo.Emissive);
            maskVT = db.GetAsset<VTTextureAsset>(atlasInfo.MaskMap);
            normalVT = db.GetAsset<VTTextureAsset>(atlasInfo.Normal);

            Assert.IsNotNull(mainVT);

            mainVT.Load();
            controlVT?.LoadHeader();
            emissiveVT?.LoadHeader();
            maskVT?.LoadHeader();
            normalVT?.LoadHeader();

            LogUtils.DoInfoLog($"State main = {mainVT.state}");
            LogUtils.DoInfoLog($"State control = {controlVT?.state}");
            LogUtils.DoInfoLog($"State emissive = {emissiveVT?.state}");
            LogUtils.DoInfoLog($"State mask = {maskVT?.state}");
            LogUtils.DoInfoLog($"State normal = {normalVT?.state}");

            Sprites = atlasInfo.Sprites.ToDictionary(x => (FixedString32Bytes)x.Name, x =>
            {
                var spriteInfo = new WESpriteInfo
                {
                    Name = x.Name,
                    Region = new Rect(x.MinX, x.MinY, x.Width, x.Height),
                    HasControl = x.HasControl,
                    HasEmissive = x.HasEmissive,
                    HasMaskMap = x.HasMaskMap,
                    HasNormal = x.HasNormal,

                };
                spriteInfo.CachedBRI = WERenderingHelper.GenerateBri(this, spriteInfo);
                return spriteInfo;
            });

            Width = mainVT.width;
            Height = mainVT.height;
            IsApplied = true;

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
            Main.Apply();
            Emissive.Apply();
            Control.Apply();
            Mask.Apply();
            Normal.Apply();
            if (WillSerialize)
            {
                m_serializationOrder = new byte[][] {
                    Main.EncodeToPNG(),
                    Emissive.EncodeToPNG(),
                    Control.EncodeToPNG(),
                    Mask.EncodeToPNG(),
                    Normal.EncodeToPNG(),
                };
            }

            IsApplied = true;
        }

        private WESpriteInfo Write(Texture2D main, Texture2D emissive, Texture2D control, Texture2D mask, Texture2D normal)
        {
            var offset = rectsPack.usedRectangles.Count == 0 ? 0 : 2;
            Rect newRect = rectsPack.Insert(main.width + offset, main.height + offset, Method);
            if (newRect.height == 0)
                return default;

            newRect.xMin += offset / 2;
            newRect.xMax -= offset / 2;
            newRect.yMin += offset / 2;
            newRect.yMax -= offset / 2;

            Main.SetPixels((int)newRect.x, (int)newRect.y, (int)newRect.width, (int)newRect.height, main.GetPixels());
            var spriteInfo = new WESpriteInfo
            {
                Region = newRect,
                HasEmissive = emissive && emissive.width == main.width && emissive.height == main.height,
                HasControl = control && control.width == main.width && control.height == main.height,
                HasMaskMap = mask && mask.width == main.width && mask.height == main.height,
                HasNormal = normal && normal.width == main.width && normal.height == main.height,
            };
            if (spriteInfo.HasEmissive) Emissive.SetPixels((int)newRect.x, (int)newRect.y, (int)newRect.width, (int)newRect.height, emissive.GetPixels());
            if (spriteInfo.HasControl) Control.SetPixels((int)newRect.x, (int)newRect.y, (int)newRect.width, (int)newRect.height, control.GetPixels());
            if (spriteInfo.HasMaskMap) Mask.SetPixels((int)newRect.x, (int)newRect.y, (int)newRect.width, (int)newRect.height, mask.GetPixels());
            if (spriteInfo.HasNormal) Normal.SetPixels((int)newRect.x, (int)newRect.y, (int)newRect.width, (int)newRect.height, normal.GetPixels());

            IsApplied = false;
            return spriteInfo;
        }


        #endregion
        public void Dispose()
        {
            if (Main && Main.isReadable) GameObject.Destroy(Main);
            if (Emissive && Emissive.isReadable) GameObject.Destroy(Emissive);
            if (Control && Control.isReadable) GameObject.Destroy(Control);
            if (Mask && Mask.isReadable) GameObject.Destroy(Mask);
            if (Normal && Normal.isReadable) GameObject.Destroy(Normal);
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
            main.SetPixels(Main.MakeReadable().GetPixels(offsetX, offsetY, width, height));
            if (spriteInfo.HasControl)
            {
                control = new Texture2D(width, height, TextureFormat.RGBA32, false);
                control.SetPixels(Control.MakeReadable().GetPixels(offsetX, offsetY, width, height));
            }
            if (spriteInfo.HasEmissive)
            {
                emissive = new Texture2D(width, height, TextureFormat.RGBA32, false);
                emissive.SetPixels(Emissive.MakeReadable().GetPixels(offsetX, offsetY, width, height));
            }
            if (spriteInfo.HasMaskMap)
            {
                mask = new Texture2D(width, height, TextureFormat.RGBA32, false);
                mask.SetPixels(Mask.MakeReadable().GetPixels(offsetX, offsetY, width, height));
            }
            if (spriteInfo.HasNormal)
            {
                normal = new Texture2D(width, height, TextureFormat.RGBA32, false);
                normal.SetPixels(Normal.MakeReadable().GetPixels(offsetX, offsetY, width, height));
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
                if (main) GameObject.Destroy(main); main = new Texture2D(Width, Height, TextureFormat.RGBA32, false); main.LoadImage(bytesArrays[0].pngData);
                if (emissive) GameObject.Destroy(emissive); emissive = new Texture2D(Width, Height, TextureFormat.RGBA32, false); emissive.LoadImage(bytesArrays[1].pngData);
                if (control) GameObject.Destroy(control); control = new Texture2D(Width, Height, TextureFormat.RGBA32, false); control.LoadImage(bytesArrays[2].pngData);
                if (mask) GameObject.Destroy(mask); mask = new Texture2D(Width, Height, TextureFormat.RGBA32, false); mask.LoadImage(bytesArrays[3].pngData);
                if (normal) GameObject.Destroy(normal); normal = new Texture2D(Width, Height, TextureFormat.RGBA32, false); normal.LoadImage(bytesArrays[4].pngData);
                foreach (var sprite in Sprites)
                {
                    if (BasicIMod.DebugMode) LogUtils.DoLog($"Calculating BRI for sprite {name}.{sprite.Key}");
                    sprite.Value.CachedBRI = WERenderingHelper.GenerateBri(this, sprite.Value);
                }
                Apply();
            };
            if (version >= 1)
            {
                reader.Read(out uint versionAtlas);
                Version = versionAtlas;
            }
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

        internal XmlVTAtlasInfo GetVTDataXml(AssetDatabase<K45WE_VTLocalDatabase> db, string atlasName, string fullName, ulong checksum)
        {
            if (IsWritable)
            {
                Apply();
                Checksum = checksum;

                LogUtils.DoInfoLog($"Creating virtual textures for atlas {atlasName} - original size: {main.width}x{main.height}");

                VirtualTexturingConfig virtualTexturingConfig = Resources.Load<VirtualTexturingConfig>("VirtualTexturingConfig");

                //mainVT = db.AddAsset<VTTextureAsset>(AssetDataPath.Create($"{atlasName}_Main", EscapeStrategy.Filename), default);
                //    mainVT.Save(0, db.AddAsset(main), main.width, 0, virtualTexturingConfig);
                ////if (mask)
                //{
                //    File.WriteAllBytes(Path.Combine(K45WE_VTLocalDatabase.EffectivePath, $"{atlasName}__Mask.png"), mask.EncodeToPNG());
                //    maskVT = db.AddAsset<VTTextureAsset>(AssetDataPath.Create($"{atlasName}_Mask", EscapeStrategy.Filename), default);
                //    var textureAsset = db.AddAsset(mask);
                //    //     maskVT.Save(0, textureAsset, mask.width, 0, virtualTexturingConfig);
                //}
                ////if (normal)
                //{
                //    normalVT = db.AddAsset<VTTextureAsset>(AssetDataPath.Create($"{atlasName}_Normal", EscapeStrategy.Filename), default);
                //    //    normalVT.Save(0, db.AddAsset(normal), normal.width, 0, virtualTexturingConfig);
                //}
                //if (control)
                //{
                //    controlVT = db.AddAsset<VTTextureAsset>(AssetDataPath.Create($"{atlasName}_ControlMask", EscapeStrategy.Filename), default);
                //    var textureAsset = db.AddAsset(control);
                //    //   controlVT.Save(0, textureAsset, control.width, 0, virtualTexturingConfig);
                //}
                ////if (emissive)
                //{
                //    emissiveVT = db.AddAsset<VTTextureAsset>(AssetDataPath.Create($"{atlasName}_Emissive", EscapeStrategy.Filename), default);
                //    var textureAsset = db.AddAsset(emissive);
                //    //        emissiveVT.Save(0, textureAsset, emissive.width, 0, virtualTexturingConfig);
                //}

                var assetControlMask = AssetDataPath.Create($"{atlasName}_ControlMask", EscapeStrategy.Filename);
                var assetEmissive = AssetDataPath.Create($"{atlasName}_Emissive", EscapeStrategy.Filename);
                var assetMaskMap = AssetDataPath.Create($"{atlasName}_MaskMap", EscapeStrategy.Filename);
                var assetNormal = AssetDataPath.Create($"{atlasName}_Normal", EscapeStrategy.Filename);
                var assetMain = AssetDataPath.Create($"{atlasName}_Main", EscapeStrategy.Filename);

                VTSurfaceUtility.CreateVTSurfaceAsset(main, normal, mask, control, emissive, db, $"{atlasName}_DefMat", out defaultSurface, out glassSurface, out decalSurface);


                IsWritable = false;
                GameObject.Destroy(main);
                GameObject.Destroy(control);
                GameObject.Destroy(emissive);
                GameObject.Destroy(mask);
                GameObject.Destroy(normal);

            }
            return new XmlVTAtlasInfo
            {
                Checksum = Checksum,
                MainTex = default,// mainVT.id.guid,
                ControlMap = default,// controlVT.id.guid,
                Emissive = default,// emissiveVT.id.guid,
                MaskMap = default,//maskVT.id.guid,
                Normal = default,//normalVT.id.guid,
                SurfDcl = decalSurface.id.guid,
                SurfDef = defaultSurface.id.guid,
                SurfGls = glassSurface.id.guid,
                Name = atlasName,
                FullName = fullName,
                Sprites = Sprites.Values.Select(x => new XmlVTAtlasInfo.XmlSpriteEntry
                {
                    Name = x.Name,
                    MinX = x.Region.x,
                    MinY = x.Region.y,
                    Width = x.Region.width,
                    Height = x.Region.height,
                    HasControl = x.HasControl,
                    HasEmissive = x.HasEmissive,
                    HasMaskMap = x.HasMaskMap,
                    HasNormal = x.HasNormal,
                }).ToArray()
            };
        }
    }
}
