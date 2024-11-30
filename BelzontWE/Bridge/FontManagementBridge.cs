using Belzont.Utils;
using System;
using System.Reflection;
using static BelzontWE.FontServer;

namespace BelzontWE.Bridge
{
    [Obsolete("Don't reference methods on this class directly. Always use reverse patch to access them, and don't use this mod DLL as hard dependency of your own mod.", true)]
    public static class FontManagementBridge
    {
        public static void RegisterModFonts(Assembly mainAssembly, string rootFolder)
        {
            var modData = ModManagementUtils.GetModDataFromMainAssembly(mainAssembly).asset;
            Instance.RegisterModFonts(mainAssembly, new() { ModName = modData.name, Location = rootFolder });
        }
    }
}
