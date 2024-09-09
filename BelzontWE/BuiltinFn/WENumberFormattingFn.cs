using System;
using System.Text.RegularExpressions;

namespace BelzontWE.Builtin
{
    public class WENumberFormattingFn
    {
        public static string To4DigitsValue(float value) => DoIntReduction(value, 4);
        public static string To3DigitsValue(float value) => DoIntReduction(value, 3);

        private static readonly string[] orders = new[] { "", "k", "M", "G", "T", "P", "E" };

        private static string DoIntReduction(float src, int maxDigits)
        {
            var order = 0;
            var floatReduced = DoIntReduction(src, maxDigits, ref order);
            string result;
            if (order == 0) result = floatReduced.ToString("F3", WEModData.InstanceWE.FormatCulture)[..(maxDigits + 1)];
            else if (order > orders.Length) return "∞";
            else result = floatReduced.ToString("F3", WEModData.InstanceWE.FormatCulture)[..maxDigits];
            if (Regex.IsMatch(result, "^[0-9]$")) result = result[..-1];
            if (floatReduced < 0) result = result[..-1];
            return result + orders[order];
        }
        private static float DoIntReduction(float src, int maxDigits, ref int order)
        {
            if (Math.Abs(src) >= 10 * maxDigits)
            {
                order++;
                return DoIntReduction(src * .001f, maxDigits, ref order);
            }
            return src;
        }
    }
}
//&BelzontWE.Builtin.WEBuildingFn;GetBuildingMainRenter/Game.Companies.CompanyData;m_Brand/Game.Prefabs.BrandData;m_ColorSet.m_Channel0
//&Color32;get_cyan

//&WEBuildingFn;GetBuildingMainRenter/Game.Companies.CompanyData;m_Brand/Game.Prefabs.BrandData;m_ColorSet.m_Channel0