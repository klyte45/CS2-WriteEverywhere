using Belzont.Interfaces;
using Belzont.Utils;
using BelzontWE;
using BelzontWE.Font;
using BelzontWE.Font.Utility;
using Game;
using Game.SceneFlow;
using Game.UI.Localization;
using System;
using System.Collections;
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
        private const string GEN_IMAGE_ATLAS_CACHE_NOTIFICATION_ID = "generatingAtlasesCache";

        public static WEAtlasesLibrary Instance { get; private set; }
        private readonly Queue<Action> actionQueue = new Queue<Action>();

        protected override void OnCreate()
        {
            Instance = this;
            KFileUtils.EnsureFolderCreation(IMAGES_FOLDER);
            LoadImagesFromLocalFolders();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            foreach (var item in LocalAtlases)
            {
                item.Value?.Dispose();
            }
        }
        #region Imported atlas

        private const string INTERNAL_ATLAS_NAME = @"\/INTERNAL\/";

        private Dictionary<FixedString32Bytes, WETextureAtlas> LocalAtlases { get; } = new();


        #region Getters

        public string[] ListLocalAtlases()
        {
            return LocalAtlases.Where(x => x.Key != INTERNAL_ATLAS_NAME && x.Value.Count > 0).Select(x => x.Key.ToString()).ToArray();
        }
        public string[] ListLocalAtlasImages(string atlasName)
        {
            return LocalAtlases.TryGetValue(atlasName ?? "", out var arr) ? arr.Keys.Select(x => x.ToString()).ToArray() : new string[0];
        }

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
            if (LocalAtlases.TryGetValue(atlasName, out var resultDicCache) && resultDicCache.TryGetValue(spriteName, out cachedInfo) && (cachedInfo == null || cachedInfo.Main))
            {
                return cachedInfo;
            }
            else
            {
                return fallbackOnInvalid ? GetFromLocalAtlases(WEImages.FrameParamsInvalidImage) : null;
            }
        }
        public BasicRenderInformation GetSlideFromLocal(FixedString32Bytes atlasName, Func<int, int> idxFunc, bool fallbackOnInvalid = false) => !LocalAtlases.TryGetValue(atlasName, out var atlas)
                ? fallbackOnInvalid ? GetFromLocalAtlases(WEImages.FrameParamsInvalidFolder) : null
                : GameManager.instance.gameMode == Game.GameMode.Editor ? GetFromLocalAtlases(WEImages.FrameBorder) : GetFromLocalAtlases(atlasName, atlas.Keys.ElementAt(idxFunc(atlas.Count - 1) + 1), fallbackOnInvalid);

        #endregion

        #region Loading

        private Coroutine currentJobRunning;
        public void LoadImagesFromLocalFolders()
        {
            if (currentJobRunning is null)
            {
                currentJobRunning = GameManager.instance.StartCoroutine(LoadImagesFromLocalFoldersCoroutine());
            }
        }
        public IEnumerator LoadImagesFromLocalFoldersCoroutine()
        {
            NotificationHelper.NotifyProgress(GEN_IMAGE_ATLAS_CACHE_NOTIFICATION_ID, 0);
            yield return 0;
            var values = LocalAtlases.Values.ToArray();
            for (int i = 0; i < values.Length; i++)
            {
                var item = values[i];
                if (item != null)
                {
                    item.Dispose();
                    NotificationHelper.NotifyProgress(GEN_IMAGE_ATLAS_CACHE_NOTIFICATION_ID, Mathf.RoundToInt((i + 1f) / values.Length * 25), textI18n: "generatingAtlasesCache.deletingOld");
                    yield return 0;
                }
            }
            LocalAtlases.Clear();
            var errors = new List<string>();
            var folders = new string[] { IMAGES_FOLDER }.Concat(Directory.GetDirectories(IMAGES_FOLDER)).ToArray();
            for (int i = 0; i < folders.Length; i++)
            {
                string dir = folders[i];
                bool isRoot = dir == IMAGES_FOLDER;
                NotificationHelper.NotifyProgress(GEN_IMAGE_ATLAS_CACHE_NOTIFICATION_ID, Mathf.RoundToInt((70f * i / folders.Length) + 25), textI18n: "generatingAtlasesCache.loadingFolders", argsText: new()
                {
                    ["progress"] = LocalizedString.Value($"{i + 1}/{folders.Length}"),
                    ["atlasName"] = LocalizedString.Value(isRoot ? "<ROOT>" : dir[IMAGES_FOLDER.Length..])
                });
                yield return 0;
                var spritesToAdd = new List<WEImageInfo>();
                WEAtlasLoadingUtils.LoadAllImagesFromFolderRef(dir, ref spritesToAdd, ref errors, false);
                if (isRoot || spritesToAdd.Count > 0)
                {
                    var atlasName = isRoot ? string.Empty : Path.GetFileNameWithoutExtension(dir);
                    LocalAtlases[atlasName] = new(512);
                    foreach (var entry in spritesToAdd)
                    {
                        while (LocalAtlases[atlasName].Insert(entry) == 2)
                        {
                            var currentSize = LocalAtlases[atlasName].Width;
                            if (currentSize > 8196) break;
                            var newAtlas = new WETextureAtlas(currentSize * 2);
                            newAtlas.InsertAll(LocalAtlases[atlasName]);
                            LocalAtlases[atlasName].Dispose();
                            LocalAtlases[atlasName] = newAtlas;
                        }
                        entry.Dispose();
                    }
                    LocalAtlases[atlasName].Apply();
                    if (BasicIMod.DebugMode) LocalAtlases[atlasName]._SaveDebug(atlasName);
                }
            }
            NotificationHelper.NotifyProgress(GEN_IMAGE_ATLAS_CACHE_NOTIFICATION_ID, 95, textI18n: "generatingAtlasesCache.loadingInternalAtlas");
            yield return 0;
            LocalAtlases[INTERNAL_ATLAS_NAME] = new(2048);
            foreach (var img in Enum.GetValues(typeof(WEImages)).Cast<WEImages>())
            {
                var Texture = KResourceLoader.LoadTextureMod(img.ToString());
                while (LocalAtlases[INTERNAL_ATLAS_NAME].Insert(img.ToString(), Texture) == 2)
                {
                    var currentSize = LocalAtlases[INTERNAL_ATLAS_NAME].Width;
                    if (currentSize > 8196) break;
                    var newAtlas = new WETextureAtlas(currentSize * 2);
                    newAtlas.InsertAll(LocalAtlases[INTERNAL_ATLAS_NAME]);
                    LocalAtlases[INTERNAL_ATLAS_NAME].Dispose();
                    LocalAtlases[INTERNAL_ATLAS_NAME] = newAtlas;
                }
                GameObject.Destroy(Texture);
            }
            LocalAtlases[INTERNAL_ATLAS_NAME].Apply();
            if (errors.Count > 0)
            {
                for (var i = 0; i < errors.Count; i++)
                {
                    LogUtils.DoWarnLog($"Error loading WE atlases: {errors[i]}");
                }
            }
            if (BasicIMod.DebugMode) LogUtils.DoLog($"Loaded atlases: {string.Join(", ", LocalAtlases.Select(x => x.Key))}");

            NotificationHelper.NotifyProgress(GEN_IMAGE_ATLAS_CACHE_NOTIFICATION_ID, 100, textI18n: "generatingAtlasesCache.complete");
            currentJobRunning = null;
        }


        #endregion

        #endregion

        #region Geometry
        private static BasicRenderInformation m_bgTexture;
        public static BasicRenderInformation GetWhiteTextureBRI()
        {
            m_bgTexture ??= WERenderingHelper.GenerateBri("\0whiteTexture\0", new WEImageInfo(null) { Texture = Texture2D.whiteTexture });
            return m_bgTexture;
        }
        private static Material m_whiteBriMaterial;
        public static Material DefaultMaterialWhiteTexture()
        {
            if (!m_whiteBriMaterial)
            {
                m_whiteBriMaterial = WERenderingHelper.GenerateMaterial(m_bgTexture, WEShader.Default);
                m_whiteBriMaterial.mainTexture = Texture2D.whiteTexture;
            }
            return m_whiteBriMaterial;
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