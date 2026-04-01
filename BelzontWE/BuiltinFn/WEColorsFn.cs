using UnityEngine;
using Color = UnityEngine.Color;
using ColorExtensions = Belzont.Utils.ColorExtensions;

namespace BelzontWE.Builtin
{
    [WEBuiltinFunction("Colors")]
    public static class WEColorsFn
    {
        [WEFormula(typeof(Color))]
        public static Color GetContrastColor(Color input) => ColorExtensions.ContrastColor(input);
        [WEFormula(typeof(Color))]
        public static Color CastColor(Color32 input) => input;
        [WEFormula(typeof(Color32))]
        public static Color32 CastColor32(Color input) => input;
    }
}