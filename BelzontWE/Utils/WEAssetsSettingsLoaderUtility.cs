using Belzont.Utils;
using BelzontWE.Sprites;
using Colossal.IO.AssetDatabase;
using Game.UI.Localization;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BelzontWE.Utils
{
    public static class WEAssetsSettingsLoaderUtility
    {
        public const string WE_FOLDER_ROOT = "K45_WE";
        public const string WE_FOLDER_LAYOUTS = "layouts";
        public const string WE_FOLDER_ATLASES = "atlases";
        public const string WE_FOLDER_MESHES = "objMeshes";

        private static readonly HashSet<string> loadedAssets = [];

        private static int Cooldown = 0;
        public static bool RanAtLeastOnce { get; private set; } = false;

        public static bool ReloadAssetsSettings()
        {
            if (Cooldown-- > 0)
            {
                return false;
            }
            var itemsWithLayouts = AssetDatabase.global.GetAssets<PrefabAsset>().GroupBy(x => x.path).Where(x=>!loadedAssets.Contains(x.Key)).Select(x => x.First()).Where(x =>
            {
                if (!(x.uri.Contains("://user/") || x.uri.Contains("://paradoxmods/"))) return false;
                var weRootFolder = Path.Combine(Path.GetDirectoryName(x.path), WE_FOLDER_ROOT);
                return Directory.Exists(weRootFolder);
            }).ToList();

            foreach (var item in itemsWithLayouts)
            {
                LogUtils.DoInfoLog($"Loading data for asset: {item.uniqueName}");
                var sourceFolderLoc = Path.Combine(Path.GetDirectoryName(item.path), WE_FOLDER_ROOT);
                var meta = item.GetMeta();
                loadedAssets.Add(item.path);

                if (Directory.Exists(Path.Combine(sourceFolderLoc, WE_FOLDER_ATLASES)))
                {
                    LogUtils.DoInfoLog($"Loading atlases for asset: {item.uniqueName}");
                    var baseFolder = Path.Combine(sourceFolderLoc, WE_FOLDER_ATLASES);
                    var atlases = Directory.GetDirectories(baseFolder, "*", SearchOption.TopDirectoryOnly);
                    foreach (var atlasFolder in atlases)
                    {
                        var atlasName = Path.GetFileName(atlasFolder);
                        AtlasDataSetup(item, atlasName, out string modIdentifier, out string displayName, out string targetAtlasName, out string notifGroup, out Dictionary<string, ILocElement> args);
                        WEAtlasesLibrary.Instance.LoadImagesToAtlas(item, atlasName, Directory.GetFiles(atlasFolder, "*.png"), modIdentifier, displayName, notifGroup, args);
                    }
                }
                if (Directory.Exists(Path.Combine(sourceFolderLoc, WE_FOLDER_MESHES)))
                {
                    LogUtils.DoInfoLog($"Loading meshes for asset: {item.uniqueName}");
                    foreach (var obj in Directory.GetFiles(Path.Combine(sourceFolderLoc, WE_FOLDER_MESHES), "*.obj"))
                    {
                        WECustomMeshLibrary.Instance.LoadMeshToMod(item, Path.GetFileNameWithoutExtension(obj), obj);

                    }
                }
                if (Directory.Exists(Path.Combine(sourceFolderLoc, WE_FOLDER_LAYOUTS)))
                {
                    LogUtils.DoInfoLog($"Loading layouts for asset: {item.uniqueName}");
                    WETemplateManager.Instance.RegisterAssetsLayoutsFolder(item, Path.Combine(sourceFolderLoc, WE_FOLDER_LAYOUTS));
                }

                LogUtils.DoInfoLog($"End loading data for asset: {item.uniqueName}");

            }
            RanAtLeastOnce = true;
            return true;
        }

        internal static void ResetCooldown()
        {
            Cooldown = 50;
        }

        private static void AtlasDataSetup(AssetData modData, string atlasName, out string modIdentifier, out string displayName, out string targetAtlasName, out string notifGroup, out Dictionary<string, ILocElement> args)
        {
            modIdentifier = modData.identifier;
            displayName = modData.GetMeta().displayName;
            targetAtlasName = WEModIntegrationUtility.GetModAccessName(modData, atlasName);
            notifGroup = $"{WEAtlasesLibrary.LOAD_FROM_MOD_NOTIFICATION_ID_PREFIX}:{targetAtlasName}";
            args = new()
            {
                ["atlasName"] = LocalizedString.Value(atlasName),
                ["mod"] = LocalizedString.Value(displayName),
            };
        }
    }
}