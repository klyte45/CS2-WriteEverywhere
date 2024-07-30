using Colossal.IO.AssetDatabase;
using System.IO;
using System.Reflection;

namespace BelzontWE
{
    public enum WEMemberSource
    {
        Game,
        Unity,
        CoUI,
        System,
        Mod,
        Unknown
    }
    public static class WEMemberSourceExtensions
    {
        public static WEMemberSource GetSource(Assembly assembly, out string modUrl, out string modName, out string dllName)
        {
            WEMemberSource source = WEMemberSource.Mod;
            dllName = assembly?.GetName()?.Name ?? "??????";
            modName = null;
            modUrl = null;
            if (dllName.StartsWith("Unity"))
            {
                source = WEMemberSource.Unity;
            }
            else if (dllName.ToLower().StartsWith("cohtml"))
            {
                source = WEMemberSource.CoUI;
            }
            else if (dllName.ToLower().StartsWith("System"))
            {
                source = WEMemberSource.System;
            }
            else if (assembly.Location.Contains($"Cities2_Data{Path.DirectorySeparatorChar}Managed"))
            {
                source = WEMemberSource.Game;
            }
            else
            {
                var thisFullName = assembly.FullName;
                ExecutableAsset modInfo = AssetDatabase.global.GetAsset(SearchFilter<ExecutableAsset>.ByCondition(x => x.definition?.FullName == thisFullName));
                if (modInfo == null)
                {
                    modUrl = modName = "??????";
                }
                else
                {
                    modName = modInfo.GetMeta().displayName;
                    modUrl = modInfo.GetMeta().remoteStorageSourceName;
                }
            }
            return source;
        }
    }
}