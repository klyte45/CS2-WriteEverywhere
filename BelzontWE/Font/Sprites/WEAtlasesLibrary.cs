using Belzont.Interfaces;
using Belzont.Utils;
using BelzontWE.Commons.Utils.AssetPipeline;
using BelzontWE.Font;
using BelzontWE.Layout;
using Colossal.IO.AssetDatabase;
using Colossal.IO.AssetDatabase.VirtualTexturing;
using Colossal.OdinSerializer.Utilities;
using Colossal.Serialization.Entities;
using Game;
using Game.Common;
using Game.SceneFlow;
using Game.Tools;
using Game.UI;
using Game.UI.Localization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace BelzontWE.Sprites
{
    public partial class WEAtlasesLibrary : GameSystemBase, IDefaultSerializable
    {
        internal const string LOAD_FROM_MOD_NOTIFICATION_ID_PREFIX = "generatingAtlasesCacheMod";
        public static string IMAGES_FOLDER => Path.Combine(BasicIMod.ModSettingsRootFolder, "imageAtlases");
        public static string ATLAS_EXPORT_FOLDER => Path.Combine(BasicIMod.ModSettingsRootFolder, "exportedAtlases");
        public static string CACHED_VT_FOLDER => Path.Combine(BasicIMod.ModSettingsRootFolder, ".cache", "vtAtlases");
        public static string CACHED_VT_TILES_FOLDER => Path.Combine(BasicIMod.ModSettingsRootFolder, ".cache", "vtTiles");
        public static string CACHED_VT_TILES_CITY_FOLDER => Path.Combine(BasicIMod.ModSettingsRootFolder, ".cache", "vtTilesCity");
        private const string GEN_IMAGE_ATLAS_CACHE_NOTIFICATION_ID = "generatingAtlasesCache";
        private const string ERRORS_IMAGE_ATLAS_NOTIFICATION_ID = "errorLoadingAtlasesCache";
        private const string ERRORS_IMAGE_ATLAS_NOTIFICATION_MODULE_ID = "errorLoadingModuleAtlasesCache";

        public static WEAtlasesLibrary Instance { get; private set; }
        private readonly Queue<Action> actionQueue = new();
        private readonly Queue<WETextureAtlas> m_pendingVTRegistrations = new();
        private bool m_vtSystemReady;
        private readonly Dictionary<string, uint> m_localAtlasChecksums = new();
        private readonly Dictionary<string, uint> m_modAtlasChecksums = new();
        private EntityQuery m_atlasUsageQuery;
        private TextureStreamingSystem m_textureStreamingSystem;

        protected override void OnCreate()
        {
            Instance = this;
            KFileUtils.EnsureFolderCreation(IMAGES_FOLDER);
            KFileUtils.EnsureFolderCreation(CACHED_VT_FOLDER);
            KFileUtils.EnsureFolderCreation(CACHED_VT_TILES_FOLDER);
            KFileUtils.EnsureFolderCreation(CACHED_VT_TILES_CITY_FOLDER);
            WEAtlasVTUtils.CleanCityVTTileFileDirectory();
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
            m_textureStreamingSystem = World.GetOrCreateSystemManaged<TextureStreamingSystem>();
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
            foreach (var item in CityAtlases)
            {
                item.Value?.Dispose();
            }
            foreach (var item in ModAtlases)
            {
                item.Value?.Dispose();
            }
        }

        private const string INTERNAL_ATLAS_NAME = @"\/INTERNAL\/";

        private Dictionary<FixedString32Bytes, WETextureAtlas> LocalAtlases { get; } = [];
        private Dictionary<FixedString32Bytes, WETextureAtlas> CityAtlases { get; } = [];
        private Dictionary<string, WETextureAtlas> ModAtlases { get; } = [];

        private Dictionary<string, (AssetData info, Dictionary<string, (Action callback, Func<uint> checksumFactory, string modAccessName)> registrations)> RegisteredModsAtlases { get; } = [];

        /// <summary>
        /// Iterates all atlas dictionaries (local, city, mod) and calls
        /// <see cref="WETextureAtlas.NotifyRendering"/> on each VT-registered atlas.
        /// Called once per frame by <see cref="WEVTRequestSystem"/> instead of
        /// per-entity in the render callback.
        /// </summary>
        internal void NotifyAllVTAtlasesRendering()
        {
            foreach (var atlas in LocalAtlases.Values)
                atlas.NotifyRendering();
            foreach (var atlas in CityAtlases.Values)
                atlas.NotifyRendering();
            foreach (var atlas in ModAtlases.Values)
                atlas.NotifyRendering();
        }

        #region Getters

        public Dictionary<string, bool> ListAvailableAtlases() => LocalAtlases.Where(x => x.Key != INTERNAL_ATLAS_NAME && !CityAtlases.ContainsKey(x.Key) && x.Value.Count > 0).Select(x => (x.Key.ToString(), false)).Concat(CityAtlases.Select(x => (x.Key.ToString(), true))).ToDictionary(x => x.Item1, x => x.Item2);

        public string[] ListAvailableAtlasImages(string atlasName) => !atlasName.IsNullOrWhitespace() && (CityAtlases.TryGetValue(atlasName, out var arr) || LocalAtlases.TryGetValue(atlasName, out arr) || ModAtlases.TryGetValue(atlasName, out arr)) ? [.. arr.Keys.Select(x => x.ToString())] : [];

        internal IBasicRenderInformation GetFromLocalAtlases(WEImages image)
        {
            var sprite = GetFromAvailableAtlases(INTERNAL_ATLAS_NAME, image.ToString());
            if (sprite is null) return null;
            sprite.IsError = image != WEImages.FrameBorder;
            return sprite;
        }

        public bool TryGetAtlas(string atlasName, out WETextureAtlas atlas) => CityAtlases.TryGetValue(atlasName, out atlas) || LocalAtlases.TryGetValue(atlasName, out atlas) || ModAtlases.TryGetValue(atlasName, out atlas);

        public IBasicRenderInformation GetFromAvailableAtlases(string atlasName, FixedString32Bytes spriteName, bool fallbackOnInvalid = false)
        {
            IBasicRenderInformation fallbackBri = null;
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

        private bool ContainsSprite<T>(Dictionary<T, WETextureAtlas> dictionary, T atlasName, FixedString32Bytes spriteName, out IBasicRenderInformation cachedInfo, ref IBasicRenderInformation fallback)
        {
            cachedInfo = null;
            var isValidAtlas = dictionary.TryGetValue(atlasName, out var resultDicCache);
            var result = isValidAtlas && resultDicCache.TryGetValue(spriteName, out cachedInfo) && cachedInfo != null && cachedInfo.IsValid();
            if (!result && fallback == null && isValidAtlas && resultDicCache.TryGetValue("_FALLBACK", out var fallbackItem) && fallbackItem != null && fallbackItem.IsValid())
            {
                fallback = fallbackItem;
            }
            return result;
        }

        #endregion

        #region Loading

        private Coroutine localSpritesJobRunning;
        public void LoadImagesFromLocalFolders() => localSpritesJobRunning ??= GameManager.instance.StartCoroutine(LoadImagesFromLocalFoldersCoroutine());
        public IEnumerator LoadImagesFromLocalFoldersCoroutine()
        {
            if (modSpritesJobRunning != null) yield return 0;
            NotificationHelper.NotifyProgress(GEN_IMAGE_ATLAS_CACHE_NOTIFICATION_ID, 0);
            yield return 0;
            var errors = new Dictionary<string, string>();
            var folders = Directory.Exists(IMAGES_FOLDER) ? Directory.GetDirectories(IMAGES_FOLDER) : Array.Empty<string>();
            // Dispose atlases for folders that no longer exist on disk
            var folderNamesOnDisk = new HashSet<string>(folders.Select(d => Path.GetFileNameWithoutExtension(d)));
            var toRemove = LocalAtlases.Keys
                .Where(k => k != INTERNAL_ATLAS_NAME && !folderNamesOnDisk.Contains(k.ToString()))
                .ToList();
            foreach (var key in toRemove)
            {
                if (LocalAtlases.TryGetValue(key, out var old)) { var captured = old; actionQueue.Enqueue(() => captured?.Dispose()); }
                LocalAtlases.Remove(key);
                m_localAtlasChecksums.Remove(key.ToString());
            }
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
                var atlasName = Path.GetFileNameWithoutExtension(dir);
                var checksum = WEChecksumUtils.ComputeFolderChecksum(dir);
                // Smart reload: skip if checksum unchanged and atlas already loaded
                if (m_localAtlasChecksums.TryGetValue(atlasName, out var prevChecksum)
                    && prevChecksum == checksum
                    && LocalAtlases.ContainsKey(atlasName))
                {
                    continue;
                }
                var cacheFilePath = Path.Combine(CACHED_VT_FOLDER, $"{atlasName}.cache.we.bc7");
                var cachedFile = KAtlasCacheFile.ReadFrom(cacheFilePath);
                if (cachedFile != null && cachedFile.Checksum == checksum)
                {
                    try
                    {
                        LocalAtlases[atlasName] = WETextureAtlas.FromCacheFile(cachedFile);
                        LocalAtlases[atlasName].VTTileFolderName = atlasName;
                        RegisterAtlasForVT(LocalAtlases[atlasName]);
                        m_localAtlasChecksums[atlasName] = checksum;
                        continue;
                    }
                    catch (Exception ex)
                    {
                        LogUtils.DoWarnLog($"[WEAtlasesLibrary] Failed to load BC7 cache for atlas '{atlasName}': {ex.GetType().Name}: {ex.Message}");
                    }
                }
                var spritesToAdd = new List<WEImageInfo>();
                WEAtlasLoadingUtils.LoadAllImagesFromFolderRef(dir, spritesToAdd, (img, msg) => errors[img] = msg);
                var generatedAtlas = RegisterLocalAtlas(atlasName, spritesToAdd, GEN_IMAGE_ATLAS_CACHE_NOTIFICATION_ID, "generatingAtlasesCache.loadingFolders", argsNotif, loopCompleteSizeProgress: 70f / folders.Length, progressOffset: (i * 70f / folders.Length) + 25, sourceFolderPath: dir);
                if (generatedAtlas != null)
                {
                    m_localAtlasChecksums[atlasName] = checksum;
                }
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
                    if (currentSize >= WETextureAtlas.MAX_SIZE) break;
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
                foreach (var error in errors)
                {
                    LogUtils.DoWarnLog($"Error loading WE image '{error.Key}': {error.Value}");
                }
                NotificationHelper.NotifyWithCallback(ERRORS_IMAGE_ATLAS_NOTIFICATION_ID, Colossal.PSI.Common.ProgressState.Warning, () =>
                {
                    var dialog2 = new MessageDialog(
                        LocalizedString.Id(NotificationHelper.GetModDefaultNotificationTitle(ERRORS_IMAGE_ATLAS_NOTIFICATION_ID)),
                        LocalizedString.Id("K45::WE.ATLAS_MANAGER[errorDialogHeader]"),
                        LocalizedString.Value("Errors on local images:\n" + string.Join("\n", errors.Select(x => $"{x.Key}: {x.Value}"))),
                        true,
                        LocalizedString.Id("Common.OK"),
                        LocalizedString.Id(BasicIMod.ModData.FixLocaleId(BasicIMod.ModData.GetOptionLabelLocaleID(nameof(BasicModData.GoToLogFolder))))
                        );
                    GameManager.instance.userInterface.appBindings.ShowMessageDialog(dialog2, (x) =>
                    {
                        switch (x)
                        {
                            case 2:
                                BasicIMod.ModData.GoToLogFolder = true;
                                break;
                        }
                        NotificationHelper.RemoveNotification(ERRORS_IMAGE_ATLAS_NOTIFICATION_ID);
                    });
                });

            }
            if (BasicIMod.DebugMode) LogUtils.DoLog($"Loaded atlases: {string.Join(", ", LocalAtlases.Select(x => x.Key))}");

            NotificationHelper.NotifyProgress(GEN_IMAGE_ATLAS_CACHE_NOTIFICATION_ID, 100, textI18n: "generatingAtlasesCache.complete");
            WECustomMeshLibrary.Instance.ClearAllCache();
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
            m_modAtlasChecksums.Remove(key);
            if (RegisteredModsAtlases.TryGetValue(WEModIntegrationUtility.GetModIdentifier(modId), out var registrers) && registrers.registrations.ContainsKey(atlasName))
            {
                registrers.registrations.Remove(atlasName);
            }
        }

        internal void LoadImagesToAtlas(Assembly mainAssembly, string atlasName, string[] imagePaths, string modIdentifier, string displayName, string notifGroup, Dictionary<string, ILocElement> args)
        {
            var modId = WEModIntegrationUtility.GetModIdentifier(mainAssembly);
            EnqueueModAtlasLoader(mainAssembly, atlasName, modIdentifier, displayName, notifGroup, args, modId,
                (spritesToAdd, errors) => WEAtlasLoadingUtils.LoadAllImagesFromList(imagePaths, spritesToAdd, (img, msg) => errors.Add($"{img}: {msg}")),
                () => WEChecksumUtils.ComputeFileListChecksum(imagePaths));
        }

        internal void LoadImagesAsDynamicAtlas(Assembly mainAssembly, string atlasName,
            Func<(string Name, byte[] Main, byte[] ControlMask, byte[] MaskMap, byte[] Normal, byte[] Emissive, string XmlInfo)[]> producer,
            string modIdentifier, string displayName, string notifGroup, Dictionary<string, ILocElement> args)
        {
            var modId = WEModIntegrationUtility.GetModIdentifier(mainAssembly);
            EnqueueModAtlasLoader(mainAssembly, atlasName, modIdentifier, displayName, notifGroup, args, modId,
                (spritesToAdd, errors) => WEAtlasLoadingUtils.LoadAllImagesFromList(producer(), spritesToAdd, errors),
                () => WEChecksumUtils.ComputeBridgeMemoryChecksum(producer()));
        }
        internal void LoadImagesToAtlas(AssetData metadata, string atlasName, string[] imagePaths, string modIdentifier, string displayName, string notifGroup, Dictionary<string, ILocElement> args)
        {
            var modId = WEModIntegrationUtility.GetModIdentifier(metadata);
            EnqueueModAtlasLoader(metadata, atlasName, modIdentifier, displayName, notifGroup, args, modId,
                (spritesToAdd, errors) =>
                {
                    WEAtlasLoadingUtils.LoadAllImagesFromList(imagePaths, spritesToAdd, (img, msg) => errors.Add($"{img}: {msg}"));
                    LogUtils.DoLog($"Loaded images to atlas '{atlasName}' from paths: {string.Join(", ", imagePaths)}");
                },
                () => WEChecksumUtils.ComputeFileListChecksum(imagePaths));

            LogUtils.DoLog($"Enqueued images for atlas: '{atlasName}'");
        }

        private void EnqueueModAtlasLoader(object mainAssembly, string atlasName, string modIdentifier, string displayName, string notifGroup, Dictionary<string, ILocElement> args, string modId, Action<List<WEImageInfo>, List<string>> loaderEnqueue, Func<uint> checksumFactory)
        {
            if (mainAssembly is not Assembly and not AssetData)
            {
                throw new ArgumentException("mainAssembly must be of type Assembly or AssetData");
            }
            var modAccessName = mainAssembly is Assembly aKey
                ? WEModIntegrationUtility.GetModAccessName(aKey, atlasName)
                : WEModIntegrationUtility.GetModAccessName(mainAssembly as AssetData, atlasName);
            // The modAccessName format is "ModId:AtlasName". Colon is illegal in Windows filenames,
            // so we use it as a directory separator: vtAtlases/ModId/AtlasName.cache.we.bc7
            var cacheFilePath = Path.Combine(CACHED_VT_FOLDER, modAccessName.Replace(':', Path.DirectorySeparatorChar) + ".cache.we.bc7");

            actionQueue.Enqueue(() =>
            {
                void RegisterCallback()
                {
                    // Compute checksum before loading to enable smart cache hit
                    uint checksum = 0;
                    try { checksum = checksumFactory(); } catch { }

                    if (checksum != 0)
                    {
                        var cachedFile = KAtlasCacheFile.ReadFrom(cacheFilePath);
                        if (cachedFile != null && cachedFile.Checksum == checksum)
                        {
                            try
                            {
                                ModAtlases[modAccessName] = WETextureAtlas.FromCacheFile(cachedFile);
                                ModAtlases[modAccessName].VTTileFolderName = modAccessName;
                                RegisterAtlasForVT(ModAtlases[modAccessName]);
                                m_modAtlasChecksums[modAccessName] = checksum;
                                return;
                            }
                            catch (Exception ex)
                            {
                                LogUtils.DoWarnLog($"[WEAtlasesLibrary] Failed to load BC7 cache for mod atlas '{modAccessName}': {ex.GetType().Name}: {ex.Message}");
                            }
                        }
                    }

                    var spritesToAdd = new List<WEImageInfo>();
                    var errors = new List<string>();
                    loaderEnqueue(spritesToAdd, errors);
                    if (errors.Count > 0)
                    {
                        var paramsTitle = new Dictionary<string, ILocElement>()
                        {
                            ["atlasName"] = LocalizedString.Value(atlasName),
                            ["mod"] = LocalizedString.Value(displayName),
                        };
                        NotificationHelper.NotifyWithCallback($"{ERRORS_IMAGE_ATLAS_NOTIFICATION_MODULE_ID}.{modId}.{atlasName}", Colossal.PSI.Common.ProgressState.Warning, () =>
                        {
                            var dialog2 = new MessageDialog(
                                LocalizedString.Id(NotificationHelper.GetModDefaultNotificationTitle(ERRORS_IMAGE_ATLAS_NOTIFICATION_MODULE_ID), [.. paramsTitle.Select(x => (x.Key, x.Value))]),
                                LocalizedString.Id("K45::WE.ATLAS_MANAGER[errorDialogHeader]"),
                                LocalizedString.Value($"Errors on {atlasName} images from mod '{displayName}' ({modIdentifier}):\n" + string.Join("\n", errors)),
                                true,
                                LocalizedString.Id("Common.OK"),
                                LocalizedString.Id(BasicIMod.ModData.FixLocaleId(BasicIMod.ModData.GetOptionLabelLocaleID(nameof(BasicModData.GoToLogFolder))))
                                );
                            GameManager.instance.userInterface.appBindings.ShowMessageDialog(dialog2, (x) =>
                            {
                                switch (x)
                                {
                                    case 2:
                                        BasicIMod.ModData.GoToLogFolder = true;
                                        break;
                                }
                                NotificationHelper.RemoveNotification($"{ERRORS_IMAGE_ATLAS_NOTIFICATION_MODULE_ID}.{modId}.{atlasName}");
                            });
                        }, titleI18n: ERRORS_IMAGE_ATLAS_NOTIFICATION_MODULE_ID, argsTitle: paramsTitle, textI18n: ERRORS_IMAGE_ATLAS_NOTIFICATION_MODULE_ID);
                    }
                    else if (spritesToAdd.Count == 0)
                    {
                        throw new Exception($"There are no images to load. Check with the developer from the module for a fix");
                    }
                    if (spritesToAdd.Count == 0) return;
                    var atlas = RegisterAtlas(ModAtlases, modAccessName, spritesToAdd, notifGroup, "generatingAtlasesCacheMod.loading", args, args, LOAD_FROM_MOD_NOTIFICATION_ID_PREFIX, 100, 0);
                    if (atlas != null && checksum != 0)
                    {
                        try
                        {
                            atlas.WriteBC7CacheAndReplaceTextures(cacheFilePath, checksum);
                            // Dispose RGBA32 atlas and reload from BC7 cache to free RAM.
                            var reloadedCache = KAtlasCacheFile.ReadFrom(cacheFilePath);
                            if (reloadedCache != null && reloadedCache.Checksum == checksum)
                            {
                                ModAtlases[modAccessName].Dispose();
                                ModAtlases[modAccessName] = WETextureAtlas.FromCacheFile(reloadedCache);
                            }
                            ModAtlases[modAccessName].VTTileFolderName = modAccessName;
                            RegisterAtlasForVT(ModAtlases[modAccessName]);
                            m_modAtlasChecksums[modAccessName] = checksum;
                        }
                        catch (Exception ex)
                        {
                            LogUtils.DoWarnLog($"[WEAtlasesLibrary] Failed to write BC7 cache for mod atlas '{modAccessName}': {ex.GetType().Name}: {ex.Message}");
                        }
                    }
                }
                if (!RegisteredModsAtlases.ContainsKey(modId)) RegisteredModsAtlases[modId] = (mainAssembly is Assembly a ? ModManagementUtils.GetModDataFromMainAssembly(a) : mainAssembly as AssetData, []);
                RegisteredModsAtlases[modId].registrations[atlasName] = (RegisterCallback, checksumFactory, modAccessName);
                RegisteredModsAtlases[modId].registrations[atlasName].callback();
            });
        }

        private Coroutine modSpritesJobRunning;
        public void LoadImagesFromMods() => modSpritesJobRunning ??= GameManager.instance.StartCoroutine(LoadImagesFromModsCoroutine());
        public IEnumerator LoadImagesFromModsCoroutine()
        {
            ClearAtlasDict(ModAtlases);
            m_modAtlasChecksums.Clear();
            foreach (var mod in RegisteredModsAtlases.Values)
            {
                foreach (var registration in mod.registrations.Values)
                {
                    registration.callback();
                    yield return 0;
                }
            }
            modSpritesJobRunning = null;
            WETemplateManager.Instance.IncreaseSpritesAndLayoutsDataVersion();
            WECustomMeshLibrary.Instance.ClearAllCache();
        }


        private WETextureAtlas RegisterLocalAtlas(string atlasName, List<WEImageInfo> spritesToAdd, string notificationGroupId, string notificationI18n, Dictionary<string, ILocElement> argsNotif, Dictionary<string, ILocElement> argsTitle = null, string notificationTitlei18n = null, float loopCompleteSizeProgress = 100, float progressOffset = 0, string sourceFolderPath = null)
        {
            // Clean old VT tile files for this atlas (checksum mismatch means atlas content changed)
            WEAtlasVTUtils.CleanAtlasVTTileFolder(atlasName);
            var atlas = RegisterAtlas(LocalAtlases, atlasName, spritesToAdd, notificationGroupId, notificationI18n, argsNotif, argsTitle, notificationTitlei18n, loopCompleteSizeProgress, progressOffset);
            if (atlas != null && sourceFolderPath != null)
            {
                try
                {
                    var checksum = WEChecksumUtils.ComputeFolderChecksum(sourceFolderPath);
                    var cacheFilePath = Path.Combine(CACHED_VT_FOLDER, $"{atlasName}.cache.we.bc7");
                    atlas.WriteBC7CacheAndReplaceTextures(cacheFilePath, checksum);
                    // Dispose RGBA32 atlas and reload from BC7 cache to free RAM.
                    var cachedFile = KAtlasCacheFile.ReadFrom(cacheFilePath);
                    if (cachedFile != null && cachedFile.Checksum == checksum)
                    {
                        LocalAtlases[atlasName].Dispose();
                        LocalAtlases[atlasName] = WETextureAtlas.FromCacheFile(cachedFile);
                        atlas = LocalAtlases[atlasName];
                    }
                    atlas.VTTileFolderName = atlasName;
                    RegisterAtlasForVT(atlas);
                }
                catch (Exception ex)
                {
                    LogUtils.DoWarnLog($"[WEAtlasesLibrary] Failed to write BC7 cache for atlas '{atlasName}': {ex.GetType().Name}: {ex.Message}");
                }
            }
            return atlas;
        }

        private WETextureAtlas RegisterAtlas<T>(Dictionary<T, WETextureAtlas> targetDict, T atlasName, List<WEImageInfo> spritesToAdd, string notificationGroupId, string notificationI18n, Dictionary<string, ILocElement> argsNotif, Dictionary<string, ILocElement> argsTitle = null, string notificationTitlei18n = null, float loopCompleteSizeProgress = 100, float progressOffset = 0)
        {
            if (spritesToAdd.Count > 0)
            {
                targetDict[atlasName] = new(WETextureAtlas.MIN_SIZE);
                for (int j = 0; j < spritesToAdd.Count; j++)
                {
                    WEImageInfo entry = spritesToAdd[j];
                    while (targetDict[atlasName].Insert(entry) == 2)
                    {
                        var currentSize = targetDict[atlasName].Size;
                        if (currentSize >= WETextureAtlas.MAX_SIZE) break;
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
                return targetDict[atlasName];
            }
            return null;
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
           static IBasicRenderInformation m_bgTexture;
        public static IBasicRenderInformation GetWhiteTextureBRI()
        {
            m_bgTexture ??= WERenderingHelper.GenerateBri("\0whiteTexture\0", new WEImageInfo() { Main = Texture2D.whiteTexture });
            return m_bgTexture;
        }
        private static Material[] m_whiteBriMaterial;
        public static Material[] DefaultMaterialWhiteTexture()
        {
            if (m_whiteBriMaterial is null)
            {
                m_whiteBriMaterial = new Material[1];
                m_whiteBriMaterial[0] = WERenderingHelper.GenerateMaterial(m_bgTexture, WEShader.Default);
                m_whiteBriMaterial[0].mainTexture = Texture2D.whiteTexture;
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

        #region VT mapping

        /// <summary>
        /// Reserves VT space and uploads tiles for an atlas.
        /// If the game's VT system has not finished initializing, the atlas is
        /// queued and will be registered later in <see cref="OnUpdate"/>.
        /// Safe to call on atlases that don't have BC7 serialization data (no-op).
        /// </summary>
        private void RegisterAtlasForVT(WETextureAtlas atlas)
        {
            if (!WriteEverywhereCS2Mod.WeData.UseVT) return;
            if (atlas == null || atlas.IsVTRegistered) return;

            if (!m_vtSystemReady)
            {
                m_pendingVTRegistrations.Enqueue(atlas);
                return;
            }

            if (!atlas.ReserveVTSpace(m_textureStreamingSystem)) return;
            if (!atlas.UploadTilesToVT(m_textureStreamingSystem))
            {
                atlas.DeregisterFromVT(m_textureStreamingSystem);
                return;
            }
            // Textures are no longer needed after VT registration — VT streams from
            // .vtd tile files, and the resource interceptor reloads previews from cache.
            atlas.ReleaseTextures();
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
            if (!WriteEverywhereCS2Mod.WeData.UseVT) return;
            if (!m_vtSystemReady)
            {
                // Wait until the game's VT system has fully initialized:
                // VTDatabase must exist (Initialize() ran) and both material queues must be empty.
                if (m_textureStreamingSystem.VTDatabase != null
                    && m_textureStreamingSystem.VTMaterialsLeftToLoadCount == 0
                    && m_textureStreamingSystem.VTMaterialsDuplicatesToProcessCount == 0)
                {
                    m_vtSystemReady = true;
                    LogUtils.DoInfoLog($"[WEAtlasesLibrary] Game VT system ready (tileSize={m_textureStreamingSystem.tileSize}) — {m_pendingVTRegistrations.Count} deferred atlas registrations queued.");
                }
            }

            if (m_vtSystemReady && m_pendingVTRegistrations.Count > 0)
            {
                // Rate-limit: process at most VT_REGISTRATIONS_PER_FRAME atlases per frame
                // to avoid overwhelming the VT system (matches game's rate-limited loading pattern).
                const int VT_REGISTRATIONS_PER_FRAME = 2;
                int processed = 0;
                WEAtlasVTUtils.VTCrashLog($"[WEAtlasesLibrary] OnUpdate VT batch start: pending={m_pendingVTRegistrations.Count}");
                while (processed < VT_REGISTRATIONS_PER_FRAME && m_pendingVTRegistrations.TryDequeue(out var atlas))
                {
                    if (atlas == null || atlas.IsVTRegistered) continue;
                    WEAtlasVTUtils.VTCrashLog($"[WEAtlasesLibrary] Processing atlas {atlas.Width}x{atlas.Height} remaining={m_pendingVTRegistrations.Count}");
                    if (BasicIMod.VerboseMode) LogUtils.DoVerboseLog(
                        "[WEAtlasesLibrary] VT registering atlas {0}x{1} (remaining={2})",
                        atlas.Width, atlas.Height, m_pendingVTRegistrations.Count);
                    if (!atlas.ReserveVTSpace(m_textureStreamingSystem)) continue;
                    if (!atlas.UploadTilesToVT(m_textureStreamingSystem))
                    {
                        atlas.DeregisterFromVT(m_textureStreamingSystem);
                        continue;
                    }
                    atlas.ReleaseTextures();
                    processed++;
                }
                WEAtlasVTUtils.VTCrashLog($"[WEAtlasesLibrary] OnUpdate VT batch end: processed={processed} remaining={m_pendingVTRegistrations.Count}");
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
                    var capturedAtlas = atlas;
                    var capturedKey = key.ToString();
                    var originalAction = action;
                    actionQueue.Enqueue(() =>
                    {
                        originalAction();
                        capturedAtlas.VTTileFolderName = capturedKey;
                        capturedAtlas.IsCityAtlas = true;
                        RegisterAtlasForVT(capturedAtlas);
                    });
                }
                CityAtlases[key] = atlas;
            }
        }

        public void SetDefaults(Context context)
        {
            ClearAtlasDict(CityAtlases);
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

        public float[] GetAtlasImageSize(string name) => TryGetAtlas(name, out var atlas) ? [atlas.Width, atlas.Height] : [];

        internal record struct ModAtlasRegistry(string ModId, string ModName, string[] Atlases) { }
        internal ModAtlasRegistry[] ListModAtlases() => [.. RegisteredModsAtlases.Select(kv => new ModAtlasRegistry(WEModIntegrationUtility.GetModIdentifier(kv.Value.info), kv.Value.info.GetMeta().displayName, [.. ModAtlases.Keys.Where(y => y.StartsWith(kv.Key + ":"))]))];

#if BURST
        [Unity.Burst.BurstCompile]
#endif
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