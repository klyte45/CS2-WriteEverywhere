using UnityEngine;
using Color = UnityEngine.Color;
using ColorExtensions = Belzont.Utils.ColorExtensions;

namespace BelzontWE.Builtin
{
    public static class WEColorsFn
    {
        public static Color GetContrastColor(Color input) => ColorExtensions.ContrastColor(input);
        public static Color CastColor(Color32 input) => input;
        public static Color32 CastColor32(Color input) => input;
    }
}