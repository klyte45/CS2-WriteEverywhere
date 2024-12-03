using Belzont.Utils;
using System;
using System.IO;
using System.Reflection;
using static BelzontWE.FontServer;

namespace BelzontWE.Bridge
{
    [Obsolete("Don't reference methods on this class directly. Always use reverse patch to access them, and don't use this mod DLL as hard dependency of your own mod.", true)]
    public static class FontManagementBridge
    {
        public static void RegisterModFonts(Assembly mainAssembly, string rootFolder)
        {
            if (!Directory.Exists(rootFolder)) return;
            var modData = ModManagementUtils.GetModDataFromMainAssembly(mainAssembly).asset;
            Instance.RegisterModFonts(mainAssembly, new() { ModName = modData.mod.displayName, Location = rootFolder });
        }
    }
}
