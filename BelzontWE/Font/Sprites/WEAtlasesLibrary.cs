using Belzont.Interfaces;
using Belzont.Serialization;
using Belzont.Utils;
using BelzontWE.Font;
using BelzontWE.Font.Utility;
using BelzontWE.Layout;
using Colossal.OdinSerializer.Utilities;
using Colossal.Serialization.Entities;
using Game;
using Game.Common;
using Game.SceneFlow;
using Game.Tools;
using Game.UI.Localization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace BelzontWE.Sprites
{
    public partial class WEAtlasesLibrary : GameSystemBase, IBelzontSerializableSingleton<WEAtlasesLibrary>
    {
        public static string IMAGES_FOLDER = Path.Combine(BasicIMod.ModSettingsRootFolder, "imageAtlases");
        public static string ATLAS_EXPORT_FOLDER = Path.Combine(BasicIMod.ModSettingsRootFolder, "exportedAtlases");
        private const string GEN_IMAGE_ATLAS_CACHE_NOTIFICATION_ID = "generatingAtlasesCache";

        public static WEAtlasesLibrary Instance { get; private set; }
        private readonly Queue<Action> actionQueue = new();
        private EntityQuery m_atlasUsageQuery;

        protected override void OnCreate()
        {
            Instance = this;
            KFileUtils.EnsureFolderCreation(IMAGES_FOLDER);
            actionQueue.Enqueue(() => LoadImagesFromLocalFolders());
            m_atlasUsageQuery = GetEntityQuery(new EntityQueryDesc[]
              {
                    new ()
                    {
                        All = new ComponentType[]
                        {
                            ComponentType.ReadOnly<WETextDataMesh>(),
                            ComponentType.ReadOnly<WETextDataMain>(),
                        },
                        None = new ComponentType[]
                        {
                            ComponentType.ReadOnly<WEWaitingRendering>(),
                            ComponentType.ReadOnly<Temp>(),
                            ComponentType.ReadOnly<Deleted>(),
        }
                    }
              });
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            foreach (var atlas in CityAtlases.Values)
            {
                atlas.Init();
            }
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

        public Dictionary<string, bool> ListAvailableAtlases() => LocalAtlases.Where(x => x.Key != INTERNAL_ATLAS_NAME && !CityAtlases.ContainsKey(x.Key) && x.Value.Count > 0).Select(x => (x.Key.ToString(), false)).Concat(CityAtlases.Select(x => (x.Key.ToString(), true))).ToDictionary(x => x.Item1, x => x.Item2);

        public string[] ListAvailableAtlasImages(string atlasName) => !atlasName.IsNullOrWhitespace() && (CityAtlases.TryGetValue(atlasName, out var arr) || LocalAtlases.TryGetValue(atlasName, out arr)) ? arr.Keys.Select(x => x.ToString()).ToArray() : new string[0];

        internal BasicRenderInformation GetFromLocalAtlases(WEImages image) => GetFromAvailableAtlases(INTERNAL_ATLAS_NAME, image.ToString());

        public bool TryGetAtlas(string atlasName, out WETextureAtlas atlas) => CityAtlases.TryGetValue(atlasName, out atlas) || LocalAtlases.TryGetValue(atlasName, out atlas);

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
            ClearAtlasDict(LocalAtlases);
            var errors = new List<string>();
            var folders = Directory.GetDirectories(IMAGES_FOLDER);
            for (int i = 0; i < folders.Length; i++)
            {
                string dir = folders[i];
                var argsNotif = new Dictionary<string, ILocElement>()
                {
                    ["progress"] = LocalizedString.Value($"{i + 1}/{folders.Length}"),
                    ["atlasName"] = LocalizedString.Value(dir[(IMAGES_FOLDER.Length + 1)..])
                };
                NotificationHelper.NotifyProgress(GEN_IMAGE_ATLAS_CACHE_NOTIFICATION_ID, Mathf.RoundToInt((70f * i / folders.Length) + 25), textI18n: "generatingAtlasesCache.loadingFolders", argsText: argsNotif);
                yield return 0;
                var spritesToAdd = new List<WEImageInfo>();
                WEAtlasLoadingUtils.LoadAllImagesFromFolderRef(dir, spritesToAdd, ref errors);
                if (spritesToAdd.Count > 0)
                {
                    var atlasName = Path.GetFileNameWithoutExtension(dir);
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

        private void ClearAtlasDict(Dictionary<FixedString32Bytes, WETextureAtlas> atlasDict)
        {
            var values = atlasDict.Values.ToArray();
            for (int i = 0; i < values.Length; i++)
            {
                var item = values[i];
                actionQueue.Enqueue(() => item?.Dispose());
            }
            atlasDict.Clear();
        }


        #endregion

        #region City management
        public bool CopyToCity(FixedString32Bytes atlasName, FixedString32Bytes newName)
        {
            if (!LocalAtlases.TryGetValue(atlasName, out var atlas) || CityAtlases.ContainsKey(newName))
            {
                return false;
            }
            CityAtlases[newName] = new WETextureAtlas(atlas.Width, true);
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
            KFileUtils.EnsureFolderCreation(ATLAS_EXPORT_FOLDER);
            var targetDir = Path.Combine(ATLAS_EXPORT_FOLDER, folderName);
            var targetFolderName = folderName;
            if (Directory.Exists(targetDir))
            {
                for (int i = 1; Directory.Exists(targetDir); i++)
                {
                    targetFolderName = $"{folderName}_{i}";
                    targetDir = Path.Combine(ATLAS_EXPORT_FOLDER, $"{targetFolderName}");
                }
            }
            KFileUtils.EnsureFolderCreation(targetDir);
            foreach (var sprite in atlas.Sprites)
            {
                atlas.GetAsSingleImage(sprite.Value.Name, out var main, out var emissive, out var control, out var mask, out var normal);
                var baseName = Path.Combine(targetDir, string.Join("_", sprite.Value.Name.Split(Path.GetInvalidFileNameChars())));
                File.WriteAllBytes($"{baseName}.png", main.EncodeToPNG());
                if (control) File.WriteAllBytes($"{baseName}{WEImageInfo.CONTROL_MASK_MAP_EXTENSION}", control.EncodeToPNG());
                if (mask) File.WriteAllBytes($"{baseName}{WEImageInfo.MASK_MAP_EXTENSION}", mask.EncodeToPNG());
                if (normal) File.WriteAllBytes($"{baseName}{WEImageInfo.NORMAL_MAP_EXTENSION}", normal.EncodeToPNG());
                if (emissive) File.WriteAllBytes($"{baseName}{WEImageInfo.EMISSIVE_MAP_EXTENSION}", emissive.EncodeToPNG());
            }
            return targetFolderName;
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
            ClearAtlasDict(CityAtlases);
            for (int i = 0; i < count; i++)
            {
                reader.Read(out FixedString32Bytes key);
                var atlas = new WETextureAtlas();
                if (atlas.Deserialize(reader, key, out var action))
                {
                    actionQueue.Enqueue(action);
                }
                CityAtlases[key] = atlas;
            }
        }

        public JobHandle SetDefaults(Context context)
        {
            ClearAtlasDict(CityAtlases);
            return Dependency;
        }
        #endregion

        #region UI extra
        public bool AtlasExists(string name) => name != null && (CityAtlases.ContainsKey(name) || LocalAtlases.ContainsKey(name));

        public unsafe int GetAtlasUsageCount(string name)
        {
            if (m_atlasUsageQuery.IsEmptyIgnoreFilter || !AtlasExists(name)) return 0;
            var counterResult = 0;
            var job = new WEPlaceholcerAtlasesUsageCount
            {
                atlasToCheck = name,
                m_textDataMeshHdl = GetComponentTypeHandle<WETextDataMesh>(),
                m_counter = &counterResult
            };
            job.Schedule(m_atlasUsageQuery, Dependency).Complete();
            return counterResult;

        }
        public bool AtlasExistsInSavegame(string name) => name != null && CityAtlases.ContainsKey(name);

        public int GetAtlasImageSize(string name) => TryGetAtlas(name, out var atlas) ? atlas.Main.width : -1;

        [BurstCompile]
        private unsafe struct WEPlaceholcerAtlasesUsageCount : IJobChunk
        {
            public FixedString32Bytes atlasToCheck;
            public ComponentTypeHandle<WETextDataMesh> m_textDataMeshHdl;
            public int* m_counter;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var dataMesh = chunk.GetNativeArray(ref m_textDataMeshHdl);
                for (int i = 0; i < dataMesh.Length; i++)
                {
                    if (dataMesh[i].TextType == WESimulationTextType.Image && dataMesh[i].Atlas == atlasToCheck) *m_counter += 1;
                }
            }

        }
        #endregion
    }
}