using Colossal.IO.AssetDatabase;
using System.Collections.Generic;
using Unity.Entities;

namespace BelzontWE.Builtin
{
    public static class WEModuleFn
    {
        private static readonly Dictionary<string, bool> moduleStates = [];

        private static bool IsModuleEnabled(string module)
        {
            if(!moduleStates.TryGetValue(module, out var isEnabled))
            {
                isEnabled = AssetDatabase.global.GetAsset(SearchFilter<ExecutableAsset>.ByCondition(asset => asset.isMod && asset.isLoaded && asset.name.Equals(module)))?.assembly != null;
                moduleStates[module] = isEnabled;
            }
            return isEnabled;
        }
        public static int IsModuleEnabled(Entity _, Dictionary<string, string> vars) => vars.TryGetValue("!module", out var module) && IsModuleEnabled(module) ? 1 : 0;
    }
}