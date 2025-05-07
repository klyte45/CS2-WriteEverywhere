using BelzontWE.Builtin;
using System;
using System.Globalization;

namespace BelzontWE.Bridge
{
    [Obsolete("Don't reference methods on this class directly. Always use reverse patch to access them, and don't use this mod DLL as hard dependency of your own mod.", true)]
    public static class LocalizationBridge
    {
        public static string FormatNumberWeStyle(float number, int totalDigits) => WENumberFormattingFn.DoIntReduction(number, totalDigits);
        public static CultureInfo GetWeCultureInfo() => WEModData.InstanceWE.FormatCulture;
    }
}
