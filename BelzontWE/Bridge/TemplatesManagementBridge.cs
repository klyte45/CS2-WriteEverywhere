using Belzont.Utils;
using System;
using System.IO;
using System.Reflection;
using Unity.Entities;

namespace BelzontWE.Bridge
{
    [Obsolete("Don't reference methods on this class directly. Always use reverse patch to access them, and don't use this mod DLL as hard dependency of your own mod.", true)]
    public static class TemplatesManagementBridge
    {
        public static bool RegisterCustomTemplates(Assembly mainAssembly, string rootFolderLayouts)
        {
            if (!Directory.Exists(rootFolderLayouts)) return false;
            WETemplateManager.Instance.RegisterModTemplatesForLoading(mainAssembly, rootFolderLayouts);
            return true;
        }

        public static void RegisterLoadableTemplatesFolder(Assembly mainAssembly, string rootFolder)
        {
            if (!Directory.Exists(rootFolder)) return;
            var modData = ModManagementUtils.GetModDataFromMainAssembly(mainAssembly).asset;
            WETemplateManager.Instance.RegisterLoadableTemplatesFolder(mainAssembly, new() { ModName = modData.mod.displayName, Location = rootFolder });
        }

        public static void ForceReloadLayouts() => World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<WETemplateManager>().MarkPrefabsDirty();
    }
}
