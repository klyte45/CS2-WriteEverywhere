using Belzont.Utils;
using BelzontWE.Utils;
using Colossal.IO.AssetDatabase;
using Game.UI.Menu;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BelzontWE
{
    public class PdxAssetUploadHandleOverrides : Redirector, IRedirectableWorldless
    {
        public void Awake()
        {
            AddRedirect(typeof(PdxAssetUploadHandle).GetMethod("CopyPreview", RedirectorUtils.allFlags), GetType().GetMethod(nameof(BeforeCopyPreview), RedirectorUtils.allFlags));
        }

        private static void BeforeCopyPreview(PdxAssetUploadHandle __instance)
        {
            var targetRootFolder = __instance.GetAbsoluteContentPath();
            LogUtils.DoInfoLog($"Package folder is: {__instance.mainAsset.path}");
            var weFolder = Path.Combine(Path.GetDirectoryName(__instance.mainAsset.path), WEAssetsSettingsLoaderUtility.WE_FOLDER_ROOT);
            if (Directory.Exists(weFolder))
            {
                var layoutsSrc = Path.Combine(weFolder, WEAssetsSettingsLoaderUtility.WE_FOLDER_LAYOUTS);
                var atlasesSrc = Path.Combine(weFolder, WEAssetsSettingsLoaderUtility.WE_FOLDER_ATLASES);
                var meshesSrc = Path.Combine(weFolder, WEAssetsSettingsLoaderUtility.WE_FOLDER_MESHES);

                var weRootTargetFolder = Path.Combine(targetRootFolder, WEAssetsSettingsLoaderUtility.WE_FOLDER_ROOT);
                var totalFilesCopied = 0;

                var targetLayoutsFolder = Path.Combine(weRootTargetFolder, WEAssetsSettingsLoaderUtility.WE_FOLDER_LAYOUTS);
                totalFilesCopied += CopyValidFiles(layoutsSrc, $"*.{WETemplateManager.SIMPLE_LAYOUT_EXTENSION}", targetLayoutsFolder);
                totalFilesCopied += CopyValidFiles(layoutsSrc, $"*.{WETemplateManager.PREFAB_LAYOUT_EXTENSION}", targetLayoutsFolder);

                var targetAtlasesFolder = Path.Combine(weRootTargetFolder, WEAssetsSettingsLoaderUtility.WE_FOLDER_ATLASES);
                totalFilesCopied += CopyValidFiles(atlasesSrc, "*.png", targetAtlasesFolder);

                var targetMeshesFolder = Path.Combine(weRootTargetFolder, WEAssetsSettingsLoaderUtility.WE_FOLDER_MESHES);
                totalFilesCopied += CopyValidFiles(meshesSrc, "*.obj", targetMeshesFolder);

                LogUtils.DoInfoLog($"Total WE related files copied: {totalFilesCopied}");
                if (totalFilesCopied > 0)
                {
                    //           __instance.tags.Add("WriteEverywhere"); // Someday...
                }
            }
        }

        private static int CopyValidFiles(string srcDir, string searchPattern, string targetFolder)
        {
            var filesCopied = 0;
            if (!Directory.Exists(srcDir))
            {
                return filesCopied;
            }

            foreach (var file in Directory.GetFiles(srcDir, searchPattern, SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(srcDir, file);
                var targetPath = Path.Combine(targetFolder, relativePath);
                KFileUtils.EnsureFolderCreation(Path.GetDirectoryName(targetPath));
                File.Copy(file, targetPath, true);
                LogUtils.DoInfoLog($"Copied file: {file} to {targetPath}");
                filesCopied++;
            }

            return filesCopied;
        }
    }
}