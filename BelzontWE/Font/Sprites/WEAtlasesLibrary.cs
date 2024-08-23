using Belzont.Interfaces;
using Belzont.Utils;
using BelzontWE.Font.Utility;
using Game;
using Game.SceneFlow;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using WriteEverywhere.Layout;

namespace WriteEverywhere.Sprites
{
    public partial class WEAtlasesLibrary : GameSystemBase
    {
        public static string IMAGES_FOLDER = Path.Combine(BasicIMod.ModSettingsRootFolder, "imageAtlases");

        public static WEAtlasesLibrary Instance { get; private set; }
        private readonly Queue<Action> actionQueue = new Queue<Action>();

        protected override void OnCreate()
        {
            Instance = this;
            //ReloadAssetImages();
            KFileUtils.EnsureFolderCreation(IMAGES_FOLDER);
            LoadImagesFromLocalFolders();
        }

        //public void ReloadAssetImages()
        //{
        //    foreach (var asset in
        //        VehiclesIndexes.instance.PrefabsData
        //        .Concat(BuildingIndexes.instance.PrefabsData)
        //        .Where(x => x.Value.PackageName.TrimToNull() != null)
        //        .Select(x => Tuple.New(x, KFileUtils.GetRootFolderForK45(x.Value.Info)))
        //        .GroupBy(x => x.Second)
        //        .Select(x => x.First())
        //        )
        //    {
        //        if (asset.Second is string str)
        //        {
        //            var filePath = Path.Combine(str, WEMainController.EXTRA_SPRITES_FILES_FOLDER_ASSETS);
        //            if (BasicIMod.DebugMode) LogUtils.DoLog($"Trying load path: {filePath}");
        //            if (Directory.Exists(filePath))
        //                CreateAtlasEntry(AssetAtlases, AssetEntryNameFromData(asset.First.Value), filePath, asset.First.Value.Info);
        //        }
        //    }
        //}
        #region Imported atlas

        private const string INTERNAL_ATLAS_NAME = @"\/INTERNAL\/";

        private Dictionary<FixedString32Bytes, Dictionary<FixedString32Bytes, WEImageInfo>> LocalAtlases { get; } = new();
        //   private Dictionary<string, Dictionary<string, WEImageInfo>> AssetAtlases { get; } = new Dictionary<string, Dictionary<string, WEImageInfo>>();
        private Dictionary<FixedString32Bytes, Dictionary<FixedString32Bytes, BasicRenderInformation>> LocalAtlasesCache { get; } = new();
        //  private Dictionary<string, Dictionary<string, BasicRenderInformation>> AssetAtlasesCache { get; } = new Dictionary<string, Dictionary<string, BasicRenderInformation>>();


        #region Getters

        public string[] ListLocalAtlases()
        {
            return LocalAtlases.Where(x => x.Key != INTERNAL_ATLAS_NAME && x.Value.Count > 0).Select(x => x.Key.ToString()).ToArray();
        }
        public string[] ListLocalAtlasImages(string atlasName)
        {
            return LocalAtlases.TryGetValue(atlasName ?? "", out var arr) ? arr.Keys.Select(x => x.ToString()).ToArray() : new string[0];
        }

        public void GetSpriteLib(string atlasName, out Dictionary<FixedString32Bytes, WEImageInfo> result)
        {
            if (!LocalAtlases.TryGetValue(atlasName ?? string.Empty, out result))
            {
                //AssetAtlases.TryGetValue(atlasName ?? string.Empty, out result);
            }
        }

        public string[] GetSpritesFromLocalAtlas(FixedString32Bytes atlasName) => LocalAtlases.TryGetValue(atlasName, out var atlas) ? atlas.Keys.Select(x => x.ToString()).ToArray() : null;
        //public string[] GetSpritesFromAssetAtlas(string packageName) => AssetAtlases.TryGetValue(packageName, out Dictionary<string, WEImageInfo> atlas) ? atlas.Keys.ToArray() : null;
        //public bool HasAssetAtlas(IIndexedPrefabData prefabData) => AssetAtlases.ContainsKey(AssetEntryNameFromData(prefabData));

        internal BasicRenderInformation GetFromLocalAtlases(WEImages image)
        {
            return GetFromLocalAtlases(INTERNAL_ATLAS_NAME, image.ToString());
        }
        public BasicRenderInformation GetFromLocalAtlases(FixedString32Bytes atlasName, FixedString32Bytes spriteName, bool fallbackOnInvalid = false)
        {
            if (spriteName.Trim().Length == 0)
            {
                return fallbackOnInvalid ? GetFromLocalAtlases(WEImages.FrameParamsInvalidImage) : null;
            }
            BasicRenderInformation cachedInfo = null;
            if (LocalAtlasesCache.TryGetValue(atlasName, out var resultDicCache) && resultDicCache.TryGetValue(spriteName, out cachedInfo) && (cachedInfo == null || cachedInfo.GeneratedMaterial))
            {
                return cachedInfo;
            }
            if (cachedInfo != null && !cachedInfo.GeneratedMaterial)
            {
                LocalAtlases.Clear();
            }
            if (!LocalAtlases.TryGetValue(atlasName, out var atlas) || !atlas.ContainsKey(spriteName))
            {
                return fallbackOnInvalid ? GetFromLocalAtlases(WEImages.FrameParamsInvalidImage) : null;
            }
            if (resultDicCache == null)
            {
                LocalAtlasesCache[atlasName] = new();
            }
            actionQueue.Enqueue(() => LocalAtlasesCache[atlasName][spriteName] = CreateItemAtlasCoroutine(LocalAtlases, atlasName, spriteName) ?? GetFromLocalAtlases(WEImages.FrameParamsInvalidImage));
            return LocalAtlasesCache[atlasName][spriteName] = null;

        }
        public BasicRenderInformation GetSlideFromLocal(FixedString32Bytes atlasName, Func<int, int> idxFunc, bool fallbackOnInvalid = false) => !LocalAtlases.TryGetValue(atlasName, out var atlas)
                ? fallbackOnInvalid ? GetFromLocalAtlases(WEImages.FrameParamsInvalidFolder) : null
                : GameManager.instance.gameMode == Game.GameMode.Editor ? GetFromLocalAtlases(WEImages.FrameBorder) : GetFromLocalAtlases(atlasName, atlas.Keys.ElementAt(idxFunc(atlas.Count - 1) + 1), fallbackOnInvalid);

        //public BasicRenderInformation GetSlideFromAsset(IIndexedPrefabData prefabData, Func<int, int> idxFunc, bool fallbackOnInvalid = false)
        //    => !AssetAtlases.TryGetValue(AssetEntryNameFromData(prefabData), out Dictionary<string, WEImageInfo> atlas)
        //        ? fallbackOnInvalid ? GetFromLocalAtlases(WEImages.FrameParamsInvalidFolder) : null
        //        : GameManager.instance.gameMode == Game.GameMode.Editor
        //            ? GetFromLocalAtlases(WEImages.FrameBorder)
        //            : GetFromAssetAtlases(prefabData, atlas.Keys.ElementAt(idxFunc(atlas.Count - 1) + 1), fallbackOnInvalid);

        //public BasicRenderInformation GetFromAssetAtlases(IIndexedPrefabData prefabData, string spriteName, bool fallbackOnInvalid = false)
        //{
        //    var assetId = AssetEntryNameFromData(prefabData);
        //    if (spriteName.IsNullOrWhitespace() || !AssetAtlases.ContainsKey(assetId))
        //    {
        //        return null;
        //    }
        //    if (AssetAtlasesCache.TryGetValue(assetId, out Dictionary<string, BasicRenderInformation> resultDicCache) && resultDicCache.TryGetValue(spriteName ?? "", out BasicRenderInformation cachedInfo))
        //    {
        //        return cachedInfo;
        //    }
        //    if (!AssetAtlases.TryGetValue(assetId, out Dictionary<string, WEImageInfo> atlas) || !atlas.ContainsKey(spriteName))
        //    {
        //        return fallbackOnInvalid ? GetFromLocalAtlases(WEImages.FrameParamsInvalidImageAsset) : null;
        //    }
        //    if (resultDicCache == null)
        //    {
        //        AssetAtlasesCache[assetId] = new Dictionary<string, BasicRenderInformation>();
        //    }

        //    AssetAtlasesCache[assetId][spriteName] = null;
        //    StartCoroutine(CreateItemAtlasCoroutine(AssetAtlases, AssetAtlasesCache, assetId, spriteName));
        //    return null;
        //}
        private BasicRenderInformation CreateItemAtlasCoroutine<T>(Dictionary<T, Dictionary<FixedString32Bytes, WEImageInfo>> spriteDict, T assetId, FixedString32Bytes spriteName)
        {
            if (!spriteDict.TryGetValue(assetId, out var targetAtlas))
            {
                LogUtils.DoWarnLog($"ATLAS NOT FOUND: {assetId}");
                return null;
            }
            if (targetAtlas[spriteName] is null)
            {
                LogUtils.DoWarnLog($"SPRITE NOT FOUND: {spriteName}");
                return null;
            }
            var info = targetAtlas[spriteName];
            var bri = WERenderingHelper.GenerateBri(spriteName, info.Texture);
            if ((assetId as string) == INTERNAL_ATLAS_NAME)
            {
                bri.m_isError = true;
            }
            return bri;
        }
        #endregion

        #region Loading
        public void LoadImagesFromLocalFolders()
        {
            LocalAtlases.Clear();
            var errors = new List<string>();
            var folders = new string[] { IMAGES_FOLDER }.Concat(Directory.GetDirectories(IMAGES_FOLDER));
            foreach (var dir in folders)
            {
                bool isRoot = dir == IMAGES_FOLDER;
                var spritesToAdd = new List<WEImageInfo>();
                WEAtlasLoadingUtils.LoadAllImagesFromFolderRef(dir, ref spritesToAdd, ref errors, false);
                if (isRoot || (GameManager.instance.gameMode != Game.GameMode.Editor && spritesToAdd.Count > 0))
                {
                    var atlasName = isRoot ? string.Empty : Path.GetFileNameWithoutExtension(dir);
                    LocalAtlases[atlasName] = new();
                    foreach (var entry in spritesToAdd)
                    {
                        LocalAtlases[atlasName][entry.Name] = entry;
                    }
                }
            }
            LocalAtlases[INTERNAL_ATLAS_NAME] = new();
            foreach (var img in Enum.GetValues(typeof(WEImages)).Cast<WEImages>())
            {
                LocalAtlases[INTERNAL_ATLAS_NAME][img.ToString()] = new WEImageInfo(null) { Texture = KResourceLoader.LoadTextureMod(img.ToString()) };
            }
            LocalAtlasesCache.Clear();
            if (errors.Count > 0)
            {
                for (var i = 0; i < errors.Count; i++)
                {
                    LogUtils.DoWarnLog($"Error loading WE atlases: {errors[i]}");
                }
                //KwyttoDialog.ShowModal(new KwyttoDialog.BindProperties
                //{
                //    message = $"{Str.WTS_CUSTOMSPRITE_ERRORHEADER}:",
                //    scrollText = $"\t{string.Join("\n\t", errors.ToArray())}",
                //    buttons = new[]{
                //        KwyttoDialog.SpaceBtn,
                //        new KwyttoDialog.ButtonDefinition
                //        {
                //            title = KStr.comm_releaseNotes_Ok,
                //            onClick=() => true,
                //        },
                //        KwyttoDialog.SpaceBtn
                //    }
                //});
            }
            LogUtils.DoInfoLog($"Loaded atlases: {string.Join(", ", LocalAtlases.Select(x => x.Key))}");
        }

        private WEImageInfo CloneWEImageInfo(SpriteInfo x) => x is null ? null : new WEImageInfo(null)
        {
            Borders = new Vector4(x.border.left / x.width, x.border.bottom / x.height, x.border.right / x.width, x.border.top / x.height),
            Name = x.name,
            Texture = x.texture,
            PixelsPerMeter = 100
        };


        //private static string AssetEntryNameFromData(IIndexedPrefabData vi)
        //{
        //    return vi.WorkshopId != ~0ul ? vi.WorkshopId.ToString() : vi.PackageName;
        //}

        //private void CreateAtlasEntry<T>(Dictionary<T, Dictionary<string, WEImageInfo>> atlasDic, T atlasName, string path, PrefabInfo info)
        //{
        //    WTSAtlasLoadingUtils.LoadAllImagesFromFolder(path, out List<WEImageInfo> spritesToAdd, out List<string> errors, false);
        //    foreach (string error in errors)
        //    {
        //        LogUtils.DoErrorLog($"ERROR LOADING IMAGE: {error}");
        //    }
        //    if (errors.Count > 0)
        //    {
        //        //KwyttoDialog.ShowModal(new KwyttoDialog.BindProperties
        //        //{
        //        //    message = string.Format(Str.we_general_errorsLoadingImagesFromAssetSpriteFolderHeader, $"{info.GetUncheckedLocalizedTitle()} ({info.name})"),
        //        //    scrollText = "\t-" + string.Join("\n\t-", errors.ToArray()),
        //        //    buttons = KwyttoDialog.basicOkButtonBar
        //        //});
        //    }

        //    atlasDic[atlasName] = spritesToAdd.ToDictionary(x => x.Name, x => x);
        //}
        #endregion

        #endregion

        #region Geometry




        //private static void RegisterMesh(string sprite, BasicRenderInformation bri, Dictionary<string, BasicRenderInformation> cache)
        //{
        //    RegisterMeshSingle(sprite, bri, cache);
        //}

        //internal static void RegisterMeshSingle<T>(T sprite, BasicRenderInformation bri, Dictionary<T, BasicRenderInformation> cache)
        //{
        //    bri.m_mesh.RecalculateTangents();
        //    if (cache.TryGetValue(sprite, out BasicRenderInformation currentVal) && currentVal == null)
        //    {
        //        cache[sprite] = bri;
        //    }
        //    else
        //    {
        //        cache.Remove(sprite);
        //    }
        //}

        private static BasicRenderInformation m_bgTexture;
        public static BasicRenderInformation GetWhiteTextureBRI()
        {
            m_bgTexture ??= WERenderingHelper.GenerateBri("\0whiteTexture\0", Texture2D.whiteTexture);
            return m_bgTexture;
        }
        #endregion

        protected override void OnUpdate()
        {
            while (actionQueue.TryDequeue(out var action))
            {
                action();
            }
        }
    }
}