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
        private const string WE_FOLDER_ROOT = "K45_WE";
        private const string WE_FOLDER_LAYOUTS = "layouts";
        private const string WE_FOLDER_ATLASES = "atlases";
        private const string WE_FOLDER_MESHES = "objMeshes";

        public static void ReloadAssetsSettings()
        {
            var itemsWithLayouts = AssetDatabase.global.GetAssets<PrefabAsset>().GroupBy(x => x.path).Select(x => x.First()).Where(x => (x.uri.Contains("://user/") || x.uri.Contains("://paradoxmods/")) && Directory.Exists(Path.Combine(Path.GetDirectoryName(x.path), WE_FOLDER_ROOT))).ToList();

            foreach (var item in itemsWithLayouts)
            {
                var sourceFolderLoc = Path.Combine(Path.GetDirectoryName(item.path), WE_FOLDER_ROOT);
                var meta = item.GetMeta();

                if (Directory.Exists(Path.Combine(sourceFolderLoc, WE_FOLDER_ATLASES)))
                {
                    var baseFolder = Path.Combine(sourceFolderLoc, WE_FOLDER_ATLASES);
                    var atlases = Directory.GetDirectories(baseFolder, "*", SearchOption.TopDirectoryOnly);
                    foreach (var atlasFolder in atlases)
                    {
                        AtlasDataSetup(item, atlasFolder, out string modIdentifier, out string displayName, out string targetAtlasName, out string notifGroup, out Dictionary<string, ILocElement> args);
                        WEAtlasesLibrary.Instance.LoadImagesToAtlas(item, targetAtlasName, Directory.GetFiles(atlasFolder, "*.png"), modIdentifier, displayName, notifGroup, args);
                    }
                }
                if (Directory.Exists(Path.Combine(sourceFolderLoc, WE_FOLDER_MESHES)))
                {
                    foreach (var obj in Directory.GetFiles(Path.Combine(sourceFolderLoc, WE_FOLDER_MESHES), "*.obj"))
                    {
                        WECustomMeshLibrary.Instance.LoadMeshToMod(item, Path.GetFileNameWithoutExtension(obj), obj);

                    }
                }
                if (Directory.Exists(Path.Combine(sourceFolderLoc, WE_FOLDER_LAYOUTS)))
                {
                    WETemplateManager.Instance.RegisterLoadableTemplatesFolder(item, new() { ModName = item.GetMeta().displayName, Location = Path.Combine(sourceFolderLoc, WE_FOLDER_LAYOUTS) });
                }


            }
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