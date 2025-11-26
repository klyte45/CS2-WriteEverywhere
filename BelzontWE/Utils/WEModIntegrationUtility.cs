using Colossal.IO.AssetDatabase;
using System.Reflection;

namespace BelzontWE
{
    public static class WEModIntegrationUtility
    {
        public static string GetModIdentifier(Assembly modId) => modId.GetName().Name;
        public static string GetModAccessName(Assembly modId, string itemName) => $"{GetModIdentifier(modId)}:{itemName}";
        public static string GetModIdentifier(AssetData asset)
        {
            if (asset is ExecutableAsset ea)
            {
                return GetModIdentifier(ea.assembly);
            }
            var modId = asset.GetMeta();
            return modId.platformID == 0 ? "H+" + modId.GetHashCode().ToString("X16") : $"{modId.platformID}";
        }

        public static string GetModAccessName(AssetData asset, string itemName) => asset is ExecutableAsset ea ? GetModAccessName(ea.assembly, itemName) : $"PREF_{GetModIdentifier(asset)}:{itemName}";
    }
}