using Belzont.Interfaces;
using Belzont.Serialization;
using Belzont.Utils;
using BelzontWE;
using BelzontWE.Font;
using BelzontWE.Font.Utility;
using Colossal.Serialization.Entities;
using Game;
using Game.SceneFlow;
using Game.UI.Localization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using WriteEverywhere.Layout;

namespace WriteEverywhere.Sprites
{
    public partial class WEAtlasesLibrary : GameSystemBase, IBelzontSerializableSingleton<WEAtlasesLibrary>
    {
        public static string IMAGES_FOLDER = Path.Combine(BasicIMod.ModSettingsRootFolder, "imageAtlases");
        private const string GEN_IMAGE_ATLAS_CACHE_NOTIFICATION_ID = "generatingAtlasesCache";

        public static WEAtlasesLibrary Instance { get; private set; }
        private readonly Queue<Action> actionQueue = new();

        protected override void OnCreate()
        {
            Instance = this;
            KFileUtils.EnsureFolderCreation(IMAGES_FOLDER);
            actionQueue.Enqueue(() => LoadImagesFromLocalFolders());
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            foreach (var item in LocalAtlases)
            {
                item.Value?.Dispose();
            }
        }

        private const string INTERNAL_ATLAS_NAME = @"\/INTERNAL\/";

        private Dictionary<FixedString32Bytes, WETextureAtlas> LocalAtlases { get; } = new();
        private Dictionary<FixedString32Bytes, WETextureAtlas> CityAtlases { get; } = new();

        #region Getters

        public string[] ListAvailableAtlases() => LocalAtlases.Where(x => x.Key != INTERNAL_ATLAS_NAME && !CityAtlases.ContainsKey(x.Key) && x.Value.Count > 0).Concat(CityAtlases).Select(x => x.Key.ToString()).ToArray();

        public string[] ListAvailableAtlasImages(string atlasName) => CityAtlases.TryGetValue(atlasName ?? "", out var arr) || LocalAtlases.TryGetValue(atlasName ?? "", out arr) ? arr.Keys.Select(x => x.ToString()).ToArray() : new string[0];

        internal BasicRenderInformation GetFromLocalAtlases(WEImages image) => GetFromAvailableAtlases(INTERNAL_ATLAS_NAME, image.ToString());

        public BasicRenderInformation GetFromAvailableAtlases(FixedString32Bytes atlasName, FixedString32Bytes spriteName, bool fallbackOnInvalid = false)
            => spriteName.Trim().Length == 0
                ? fallbackOnInvalid
                    ? GetFromLocalAtlases(WEImages.FrameParamsInvalidImage)
                    : null
                : CityAtlases.TryGetValue(atlasName, out var resultDicCache) && resultDicCache.TryGetValue(spriteName, out var cachedInfo) && (cachedInfo == null || cachedInfo.Main)
                    ? cachedInfo
                    : LocalAtlases.TryGetValue(atlasName, out resultDicCache) && resultDicCache.TryGetValue(spriteName, out cachedInfo) && (cachedInfo == null || cachedInfo.Main)
                        ? cachedInfo
                        : fallbackOnInvalid
                            ? GetFromLocalAtlases(WEImages.FrameParamsInvalidImage)
                            : null;

        public BasicRenderInformation GetSlideFromAvailable(FixedString32Bytes atlasName, Func<int, int> idxFunc, bool fallbackOnInvalid = false)
            => !CityAtlases.TryGetValue(atlasName, out var atlas) && !LocalAtlases.TryGetValue(atlasName, out atlas)
                ? fallbackOnInvalid
                    ? GetFromLocalAtlases(WEImages.FrameParamsInvalidFolder)
                    : null
                : GameManager.instance.gameMode == GameMode.Editor
                    ? GetFromLocalAtlases(WEImages.FrameBorder)
                    : GetFromAvailableAtlases(atlasName, atlas.Keys.ElementAt(idxFunc(atlas.Count - 1) + 1), fallbackOnInvalid);

        #endregion

        #region Loading

        private Coroutine currentJobRunning;
        public void LoadImagesFromLocalFolders() => currentJobRunning ??= GameManager.instance.StartCoroutine(LoadImagesFromLocalFoldersCoroutine());
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
                var argsNotif = new Dictionary<string, ILocElement>()
                {
                    ["progress"] = LocalizedString.Value($"{i + 1}/{folders.Length}"),
                    ["atlasName"] = LocalizedString.Value(isRoot ? "<ROOT>" : dir[(IMAGES_FOLDER.Length + 1)..])
                };
                NotificationHelper.NotifyProgress(GEN_IMAGE_ATLAS_CACHE_NOTIFICATION_ID, Mathf.RoundToInt((70f * i / folders.Length) + 25), textI18n: "generatingAtlasesCache.loadingFolders", argsText: argsNotif);
                yield return 0;
                var spritesToAdd = new List<WEImageInfo>();
                WEAtlasLoadingUtils.LoadAllImagesFromFolderRef(dir, spritesToAdd, ref errors);
                if (isRoot || spritesToAdd.Count > 0)
                {
                    var atlasName = isRoot ? string.Empty : Path.GetFileNameWithoutExtension(dir);
                    LocalAtlases[atlasName] = new(512);
                    for (int j = 0; j < spritesToAdd.Count; j++)
                    {
                        WEImageInfo entry = spritesToAdd[j];
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

                        if (j % 3 == 2)
                        {
                            NotificationHelper.NotifyProgress(GEN_IMAGE_ATLAS_CACHE_NOTIFICATION_ID, Mathf.RoundToInt((70f * (i + ((j + 1f) / spritesToAdd.Count)) / folders.Length) + 25), textI18n: "generatingAtlasesCache.loadingFolders", argsText: argsNotif);
                        }
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

        #region City management
        public bool CopyToCity(FixedString32Bytes atlasName, FixedString32Bytes newName)
        {
            if (!LocalAtlases.TryGetValue(atlasName, out var atlas) || CityAtlases.ContainsKey(newName))
            {
                return false;
            }
            CityAtlases[newName] = new WETextureAtlas(atlas.Width);
            CityAtlases[newName].InsertAll(atlas);
            return true;
        }

        public bool RemoveFromCity(FixedString32Bytes atlasName)
        {
            if (!CityAtlases.ContainsKey(atlasName)) return false;
            CityAtlases[atlasName].Dispose();
            CityAtlases.Remove(atlasName);
            return true;
        }

        public string ExportCityAtlas(FixedString32Bytes atlasName, string folderName)
        {
            if (!CityAtlases.TryGetValue(atlasName, out var atlas)) return null;
            KFileUtils.EnsureFolderCreation(IMAGES_FOLDER);
            var targetDir = Path.Combine(IMAGES_FOLDER, folderName);
            if (Directory.Exists(targetDir))
            {
                for (int i = 1; Directory.Exists(targetDir); i++)
                {
                    targetDir = Path.Combine(WETemplateManager.SAVED_PREFABS_FOLDER, $"{folderName}_{i}");
                }
            }
            var weInfoArray = atlas.ToImageInfoArray();
            foreach (var info in weInfoArray)
            {
                info.ExportAt(targetDir);
                info.Dispose();
            }
            return targetDir;
        }
        #endregion

        #region Geometry
        private static BasicRenderInformation m_bgTexture;
        public static BasicRenderInformation GetWhiteTextureBRI()
        {
            m_bgTexture ??= WERenderingHelper.GenerateBri("\0whiteTexture\0", new WEImageInfo() { Main = Texture2D.whiteTexture });
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

        #region Serialization
        private const uint CURRENT_VERSION = 0;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(CURRENT_VERSION);
            writer.Write(CityAtlases.Count);
            foreach (var entry in CityAtlases)
            {
                writer.Write(entry.Key);
                writer.Write(entry.Value);
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
            reader.Read(out int count);
            CityAtlases.Clear();
            for (int i = 0; i < count; i++)
            {
                reader.Read(out FixedString32Bytes key);
                var atlas = new WETextureAtlas();
                reader.Read(atlas);
                CityAtlases[key] = atlas;
            }
        }

        public JobHandle SetDefaults(Context context)
        {
            CityAtlases.Clear();
            return Dependency;
        }
        #endregion
    }
}