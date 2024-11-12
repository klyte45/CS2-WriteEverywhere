using Color = UnityEngine.Color;
using ColorExtensions = Belzont.Utils.ColorExtensions;

namespace BelzontWE.Builtin
{
    public static class WEColorsFn
    {
        public static Color GetContrastColor(Color input) => ColorExtensions.ContrastColor(input);
    }
}