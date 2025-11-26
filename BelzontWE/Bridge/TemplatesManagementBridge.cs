using Belzont.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.Entities;

namespace BelzontWE.Bridge
{
    [Obsolete("Don't reference methods on this class directly. Always use reverse patch to access them, and don't use this mod DLL as hard dependency of your own mod.", true)]
    public static class TemplatesManagementBridge
    {
        private static WETemplateManager templateManager;
        public static bool RegisterCustomTemplates(Assembly mainAssembly, string rootFolderLayouts)
        {
            if (!Directory.Exists(rootFolderLayouts)) return false;
            WETemplateManager.Instance.RegisterModTemplatesForLoading(mainAssembly, rootFolderLayouts);
            return true;
        }

        public static void RegisterLoadableTemplatesFolder(Assembly mainAssembly, string rootFolder)
        {
            if (!Directory.Exists(rootFolder)) return;
            var modData = ModManagementUtils.GetModDataFromMainAssembly(mainAssembly);
            WETemplateManager.Instance.RegisterLoadableTemplatesFolder(ModManagementUtils.GetModDataFromMainAssembly(mainAssembly), new() { ModName = modData.GetMeta().displayName, Location = rootFolder });
        }

        public static void ForceReloadLayouts() => (templateManager ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<WETemplateManager>()).MarkPrefabsDirty();

        public static Dictionary<string, string> GetMetadatasFromReplacement(Assembly mainAssembly, string layoutName)
        {
            templateManager ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<WETemplateManager>();
            return templateManager.GetMetadatasFromReplacement(mainAssembly, layoutName);
        }
    }
}
