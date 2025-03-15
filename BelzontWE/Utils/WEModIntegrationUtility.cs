using System.Reflection;

namespace BelzontWE
{
    public static class WEModIntegrationUtility
    {
        public static string GetModIdentifier(Assembly modId) => modId.GetName().Name;
        public static string GetModAccessName(Assembly modId, string itemName) => $"{GetModIdentifier(modId)}:{itemName}";
    }
}