using Belzont.Utils;
using System;
using System.IO;
using System.Reflection;
using static Game.Rendering.Debug.RenderPrefabRenderer;

namespace BelzontWE.Bridge
{
    [Obsolete("Don't reference methods on this class directly. Always use reverse patch to access them, and don't use this mod DLL as hard dependency of your own mod.", true)]
    public static class TemplatesManagementBridge
    {
        public static bool RegisterCustomTemplates(Assembly mainAssembly, string rootFolderLayouts)
        {
            if (!Directory.Exists(rootFolderLayouts)) return false;
            var modData = ModManagementUtils.GetModDataFromMainAssembly(mainAssembly).asset;
            var modId = modData.identifier;
            var modName = modData.name;
            WETemplateManager.Instance.RegisterModTemplatesForLoading(modId, modName, rootFolderLayouts);
            return true;
        }

        public static void RegisterLoadableTemplatesFolder(Assembly mainAssembly, string rootFolder)
        {
            var modData = ModManagementUtils.GetModDataFromMainAssembly(mainAssembly).asset;
            WETemplateManager.Instance.RegisterLoadableTemplatesFolder(mainAssembly, new() { ModName = modData.name, Location = rootFolder });
        }
    }
}
