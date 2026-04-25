using Belzont.Utils;
using Game.UI.Localization;
using System.Collections.Generic;

namespace BelzontWE.Builtin
{
    [WEBuiltinFunction("Localization")]
    public static class WELocalizationFn
    {
        [WEFormula(typeof(string))]
        public static string Translate(string str) => LocalizedString.Id(str).Translate();

        [WEFormula(typeof(string))]
        public static string TranslateWithArgs(string str, Dictionary<string, string> vars)
        {
            var result = Translate(str);
            foreach (var entry in vars)
            {
                result = result.Replace("{" + entry.Key + "}", entry.Value);
            }
            return result;
        }
    }
}
