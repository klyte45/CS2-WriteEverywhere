using Belzont.Utils;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using static BelzontWE.FontServer;

namespace BelzontWE.Bridge
{
    [Obsolete("Don't reference methods on this class directly. Always use reverse patch to access them, and don't use this mod DLL as hard dependency of your own mod.", true)]
    public static class FontManagementBridge
    {
        public static void RegisterModFonts(Assembly mainAssembly, string[] fonts)
        {
            var modData = ModManagementUtils.GetModDataFromMainAssembly(mainAssembly).asset;
            var modIdentifier = modData.identifier;
            Instance.RegisterModFonts(modIdentifier, fonts.Select(x => new ModFont()
            {
                Location = x,
                ModName = modData.name,
                Name = Path.GetFileNameWithoutExtension(x)

            }).ToArray());
        }
    }
}
