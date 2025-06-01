using Belzont.Interfaces;
using Belzont.Serialization;
using Belzont.Utils;
using BelzontWE.Font;
using BelzontWE.Font.Utility;
using BelzontWE.Layout;
using Colossal.IO.AssetDatabase;
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
using System.Reflection;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Game.Modding.ModManager;

namespace BelzontWE.Sprites
{
    public partial class WEAtlasesLibrary : GameSystemBase, IBelzontSerializableSingleton<WEAtlasesLibrary>
    {
        internal const string LOAD_FROM_MOD_NOTIFICATION_ID_PREFIX = "generatingAtlasesCacheMod";
        public static string IMAGES_FOLDER => Path.Combine(BasicIMod.ModSettingsRootFolder, "imageAtlases");
        public static string ATLAS_EXPORT_FOLDER => Path.Combine(BasicIMod.ModSettingsRootFolder, "exportedAtlases");
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
        private Dictionary<string, WETextureAtlas> ModAtlases { get; } = new();

        private Dictionary<string, (ModInfo info, Dictionary<string, Action> registerCallback)> RegisteredModsAtlases { get; } = new();

        #region Getters

        public Dictionary<string, bool> ListAvailableAtlases() => LocalAtlases.Where(x => x.Key != INTERNAL_ATLAS_NAME && !CityAtlases.ContainsKey(x.Key) && x.Value.Count > 0).Select(x => (x.Key.ToString(), false)).Concat(CityAtlases.Select(x => (x.Key.ToString(), true))).ToDictionary(x => x.Item1, x => x.Item2);

        public string[] ListAvailableAtlasImages(string atlasName) => !atlasName.IsNullOrWhitespace() && (CityAtlases.TryGetValue(atlasName, out var arr) || LocalAtlases.TryGetValue(atlasName, out arr) || ModAtlases.TryGetValue(atlasName, out arr)) ? arr.Keys.Select(x => x.ToString()).ToArray() : new string[0];

        internal BasicRenderInformation GetFromLocalAtlases(WEImages image)
        {
            var sprite = GetFromAvailableAtlases(INTERNAL_ATLAS_NAME, image.ToString());
            sprite.m_isError = image != WEImages.FrameBorder;
            return sprite;
        }

        public bool TryGetAtlas(string atlasName, out WETextureAtlas atlas) => CityAtlases.TryGetValue(atlasName, out atlas) || LocalAtlases.TryGetValue(atlasName, out atlas) || ModAtlases.TryGetValue(atlasName, out atlas);

        public BasicRenderInformation GetFromAvailableAtlases(string atlasName, FixedString32Bytes spriteName, bool fallbackOnInvalid = false)
        {
            BasicRenderInformation fallbackBri = null;
            return spriteName.Trim().Length == 0 || atlasName.IsNullOrWhitespace()
                        ? fallbackOnInvalid ? GetFromLocalAtlases(WEImages.FrameParamsInvalidImage)
                            : null
                        : atlasName.Contains(":")
                            ? ContainsSprite(ModAtlases, atlasName, spriteName, out var cachedInfo, ref fallbackBri) ? cachedInfo
                                : fallbackBri
                                ?? (fallbackOnInvalid ? GetFromLocalAtlases(WEImages.FrameParamsInvalidImage) : null)
                        : ContainsSprite(CityAtlases, atlasName, spriteName, out cachedInfo, ref fallbackBri) ? cachedInfo
                        : ContainsSprite(LocalAtlases, atlasName, spriteName, out cachedInfo, ref fallbackBri) ? cachedInfo
                        : fallbackBri
                        ?? (fallbackOnInvalid ? GetFromLocalAtlases(WEImages.FrameParamsInvalidImage) : null);
        }

        private bool ContainsSprite<T>(Dictionary<T, WETextureAtlas> dictionary, T atlasName, FixedString32Bytes spriteName, out BasicRenderInformation cachedInfo, ref BasicRenderInformation fallback)
        {
            cachedInfo = null;
            var isValidAtlas = dictionary.TryGetValue(atlasName, out var resultDicCache);
            var result = isValidAtlas && resultDicCache.TryGetValue(spriteName, out cachedInfo) && cachedInfo != null && cachedInfo.Main;
            if (!result && fallback == null && isValidAtlas && resultDicCache.TryGetValue("_FALLBACK", out var fallbackItem) && fallbackItem != null && fallbackItem.Main)
            {
                fallback = fallbackItem;
            }
            return result;
        }

        public BasicRenderInformation GetSlideFromAvailable(string atlasName, Func<int, int> idxFunc, bool fallbackOnInvalid = false)
            => !CityAtlases.TryGetValue(atlasName, out var atlas) && !LocalAtlases.TryGetValue(atlasName, out atlas)
                ? fallbackOnInvalid
                    ? GetFromLocalAtlases(WEImages.FrameParamsInvalidFolder)
                    : null
                : GameManager.instance.gameMode == GameMode.Editor
                    ? GetFromLocalAtlases(WEImages.FrameBorder)
                    : GetFromAvailableAtlases(atlasName, atlas.Keys.ElementAt(idxFunc(atlas.Count - 1) + 1), fallbackOnInvalid);

        #endregion

        #region Loading

        private Coroutine localSpritesJobRunning;
        public void LoadImagesFromLocalFolders() => localSpritesJobRunning ??= GameManager.instance.StartCoroutine(LoadImagesFromLocalFoldersCoroutine());
        public IEnumerator LoadImagesFromLocalFoldersCoroutine()
        {
            if (modSpritesJobRunning != null) yield return 0;
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
                WEAtlasLoadingUtils.LoadAllImagesFromFolderRef(dir, spritesToAdd, errors);
                RegisterLocalAtlas(Path.GetFileNameWithoutExtension(dir), spritesToAdd, GEN_IMAGE_ATLAS_CACHE_NOTIFICATION_ID, "generatingAtlasesCache.loadingFolders", argsNotif, loopCompleteSizeProgress: 70f / folders.Length, progressOffset: (i * 70f / folders.Length) + 25);
            }
            NotificationHelper.NotifyProgress(GEN_IMAGE_ATLAS_CACHE_NOTIFICATION_ID, 95, textI18n: "generatingAtlasesCache.loadingInternalAtlas");
            yield return 0;
            LocalAtlases[INTERNAL_ATLAS_NAME] = new(21);
            foreach (var img in Enum.GetValues(typeof(WEImages)).Cast<WEImages>())
            {
                var Texture = KResourceLoader.LoadTextureMod(img.ToString());
                while (LocalAtlases[INTERNAL_ATLAS_NAME].Insert(img.ToString(), Texture) == 2)
                {
                    var currentSize = LocalAtlases[INTERNAL_ATLAS_NAME].Size;
                    if (currentSize >= 28) break;
                    var newAtlas = new WETextureAtlas(currentSize + 1);
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
            localSpritesJobRunning = null;
        }
        internal void UnregisterModAtlas(Assembly modId, string atlasName)
        {
            var key = WEModIntegrationUtility.GetModAccessName(modId, atlasName);
            if (ModAtlases.ContainsKey(key))
            {
                var item = ModAtlases[key];
                actionQueue.Enqueue(() => item?.Dispose());
                ModAtlases.Remove(key);
            }
            if (RegisteredModsAtlases.TryGetValue(WEModIntegrationUtility.GetModIdentifier(modId), out var registrers) && registrers.registerCallback.ContainsKey(atlasName))
            {
                registrers.registerCallback.Remove(atlasName);
            }
        }

        internal void LoadImagesToAtlas(Assembly mainAssembly, string atlasName, string[] imagePaths, string modIdentifier, string displayName, string notifGroup, Dictionary<string, ILocElement> args)
        {
            var modId = WEModIntegrationUtility.GetModIdentifier(mainAssembly);
            EnqueueModAtlasLoader(mainAssembly, atlasName, modIdentifier, displayName, notifGroup, args, modId, (spritesToAdd, errors) => WEAtlasLoadingUtils.LoadAllImagesFromList(imagePaths, spritesToAdd, errors));
        }

        internal void LoadImagesAsDynamicAtlas(Assembly mainAssembly, string atlasName,
            Func<(string Name, byte[] Main, byte[] ControlMask, byte[] MaskMap, byte[] Normal, byte[] Emissive, string XmlInfo)[]> producer,
            string modIdentifier, string displayName, string notifGroup, Dictionary<string, ILocElement> args)
        {
            var modId = WEModIntegrationUtility.GetModIdentifier(mainAssembly);
            EnqueueModAtlasLoader(mainAssembly, atlasName, modIdentifier, displayName, notifGroup, args, modId, (spritesToAdd, errors) => WEAtlasLoadingUtils.LoadAllImagesFromList(producer(), spritesToAdd, errors));
        }

        private void EnqueueModAtlasLoader(Assembly mainAssembly, string atlasName, string modIdentifier, string displayName, string notifGroup, Dictionary<string, ILocElement> args, string modId, Action<List<WEImageInfo>, List<string>> loaderEnqueue)
        {
            actionQueue.Enqueue(() =>
            {
                void RegisterCallback()
                {
                    var spritesToAdd = new List<WEImageInfo>();
                    var errors = new List<string>();
                    loaderEnqueue(spritesToAdd, errors);
                    if (errors.Count > 0)
                    {
                        throw new Exception($"Some error were found when trying to create atlas '{atlasName}' for mod identified by '{modIdentifier}' ({displayName}), aborting:\n- {string.Join("\n- ", errors)}");
                    }
                    if (spritesToAdd.Count == 0)
                    {
                        throw new Exception($"There are no images to load. Check with the developer from the module for a fix");
                    }
                    RegisterAtlas(ModAtlases, WEModIntegrationUtility.GetModAccessName(mainAssembly, atlasName), spritesToAdd, notifGroup, "generatingAtlasesCacheMod.loading", args, args, LOAD_FROM_MOD_NOTIFICATION_ID_PREFIX, 100, 0);
                }
                if (!RegisteredModsAtlases.ContainsKey(modId)) RegisteredModsAtlases[modId] = (ModManagementUtils.GetModDataFromMainAssembly(mainAssembly), new());
                RegisteredModsAtlases[modId].registerCallback[atlasName] = RegisterCallback;
                RegisteredModsAtlases[modId].registerCallback[atlasName]();
            });
        }

        private Coroutine modSpritesJobRunning;
        public void LoadImagesFromMods() => modSpritesJobRunning ??= GameManager.instance.StartCoroutine(LoadImagesFromModsCoroutine());
        public IEnumerator LoadImagesFromModsCoroutine()
        {
            ClearAtlasDict(ModAtlases);
            foreach (var mod in RegisteredModsAtlases.Values)
            {
                foreach (var atlasCallback in mod.registerCallback.Values)
                {
                    atlasCallback();
                    yield return 0;
                }
            }
            modSpritesJobRunning = null;
            WETemplateManager.Instance.IncreaseSpritesAndLayoutsDataVersion();
        }


        private void RegisterLocalAtlas(string atlasName, List<WEImageInfo> spritesToAdd, string notificationGroupId, string notificationI18n, Dictionary<string, ILocElement> argsNotif, Dictionary<string, ILocElement> argsTitle = null, string notificationTitlei18n = null, float loopCompleteSizeProgress = 100, float progressOffset = 0)
        {
            RegisterAtlas(LocalAtlases, atlasName, spritesToAdd, notificationGroupId, notificationI18n, argsNotif, argsTitle, notificationTitlei18n, loopCompleteSizeProgress, progressOffset);
        }

        private void RegisterAtlas<T>(Dictionary<T, WETextureAtlas> targetDict, T atlasName, List<WEImageInfo> spritesToAdd, string notificationGroupId, string notificationI18n, Dictionary<string, ILocElement> argsNotif, Dictionary<string, ILocElement> argsTitle = null, string notificationTitlei18n = null, float loopCompleteSizeProgress = 100, float progressOffset = 0)
        {
            if (spritesToAdd.Count > 0)
            {
                targetDict[atlasName] = new(18);
                for (int j = 0; j < spritesToAdd.Count; j++)
                {
                    WEImageInfo entry = spritesToAdd[j];
                    while (targetDict[atlasName].Insert(entry) == 2)
                    {
                        var currentSize = targetDict[atlasName].Size;
                        if (currentSize >= 28) break;
                        var newAtlas = new WETextureAtlas(currentSize + 1);
                        newAtlas.InsertAll(targetDict[atlasName]);
                        targetDict[atlasName].Dispose();
                        targetDict[atlasName] = newAtlas;
                    }
                    entry.Dispose();

                    if (j % 3 == 2)
                    {
                        NotificationHelper.NotifyProgress(notificationGroupId, Mathf.RoundToInt(progressOffset + (loopCompleteSizeProgress * ((j + 1f) / spritesToAdd.Count))), argsTitle: argsTitle, textI18n: notificationI18n, argsText: argsNotif, titleI18n: notificationTitlei18n);
                    }
                }
                NotificationHelper.NotifyProgress(notificationGroupId, Mathf.RoundToInt(progressOffset + loopCompleteSizeProgress), argsTitle: argsTitle, textI18n: notificationI18n, argsText: argsNotif, titleI18n: notificationTitlei18n);
                targetDict[atlasName].Apply();
                if (BasicIMod.TraceMode && atlasName is string s) targetDict[atlasName]._SaveDebug(s);
            }
        }

        private void ClearAtlasDict<T>(Dictionary<T, WETextureAtlas> atlasDict)
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
        public WETextureAtlas GetOrCreateAtCity(FixedString32Bytes atlasName)
            => CityAtlases.TryGetValue(atlasName, out var atlas) ? atlas : (CityAtlases[atlasName] = new WETextureAtlas());

        public bool CopyToCity(FixedString32Bytes atlasName, FixedString32Bytes newName)
        {
            if (!LocalAtlases.TryGetValue(atlasName, out var atlas) || CityAtlases.ContainsKey(newName))
            {
                return false;
            }
            CityAtlases[newName] = new WETextureAtlas(atlas.Size, willSerialize: true);
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
            => CityAtlases.TryGetValue(atlasName, out var atlas) ? ExportAtlas(folderName, atlas) : null;

        private static string ExportAtlas(string folderName, WETextureAtlas atlas)
        {
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
#if DEBUG
        internal
#else
        private
#endif
           static BasicRenderInformation m_bgTexture;
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

        private static Material m_semiWhiteBriMaterial;
        public static Material DefaultMaterialSemiTransparent()
        {
            if (!m_semiWhiteBriMaterial)
            {
                m_semiWhiteBriMaterial = WERenderingHelper.GenerateMaterial(m_bgTexture, WEShader.Glass);
                m_semiWhiteBriMaterial.SetTexture(FontAtlas._BaseColorMap, Texture2D.whiteTexture);
                m_semiWhiteBriMaterial.SetColor("_BaseColor", new Color(1, 1, 1, .15f));
                m_semiWhiteBriMaterial.SetFloat("_Metallic", 0);
                m_semiWhiteBriMaterial.SetFloat("_Smoothness", 1);
                m_semiWhiteBriMaterial.SetFloat(WERenderingHelper.IOR, 1);
                m_semiWhiteBriMaterial.SetColor(WERenderingHelper.Transmittance, Color.clear);
                m_semiWhiteBriMaterial.SetFloat("_NormalStrength", 0);
                m_semiWhiteBriMaterial.SetFloat("_Thickness", 0);
                m_semiWhiteBriMaterial.SetVector("colossal_TextureArea", new float4(Vector2.zero, Vector2.one));
            }
            return m_semiWhiteBriMaterial;
        }
        #endregion



        internal string ExportModAtlas(string atlasFullName, string folder)
            => ModAtlases.TryGetValue(atlasFullName, out var atlas) ? ExportAtlas(folder, atlas) : null;


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
        public bool AtlasExists(string name) => name != null && (CityAtlases.ContainsKey(name) || LocalAtlases.ContainsKey(name) || ModAtlases.ContainsKey(name));

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

        public float[] GetAtlasImageSize(string name) => TryGetAtlas(name, out var atlas) ? new float[] { atlas.Width, atlas.Height } : new float[0];

        internal record struct ModAtlasRegistry(string ModId, string ModName, string[] Atlases) { }
        internal ModAtlasRegistry[] ListModAtlases() => RegisteredModsAtlases
            .Select(x => new ModAtlasRegistry(WEModIntegrationUtility.GetModIdentifier(x.Value.info.asset.assembly), x.Value.info.asset.mod.displayName, ModAtlases.Keys.Where(y => y.StartsWith(x.Key + ":")).ToArray())).ToArray();


        [BurstCompile]
        private unsafe struct WEPlaceholcerAtlasesUsageCount : IJobChunk
        {
            public FixedString512Bytes atlasToCheck;
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