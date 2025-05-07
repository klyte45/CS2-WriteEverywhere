using System;
using System.Text.RegularExpressions;
using Unity.Mathematics;

namespace BelzontWE.Builtin
{
    public class WENumberFormattingFn
    {
        public static string To4DigitsValue(float value) => DoIntReduction(value, 4);
        public static string To3DigitsValue(float value) => DoIntReduction(value, 3);
        public static string To4DigitsValue(int value) => DoIntReduction(value, 4);
        public static string To3DigitsValue(int value) => DoIntReduction(value, 3);
        public static string To4DigitsValue(long value) => DoIntReduction(value, 4);
        public static string To3DigitsValue(long value) => DoIntReduction(value, 3);
        public static string To4DigitsValue(short value) => DoIntReduction(value, 4);
        public static string To3DigitsValue(short value) => DoIntReduction(value, 3);

        private static readonly string[] orders = new[] { "", "k", "M", "G", "T", "P", "E" };

        internal static string DoIntReduction(float src, int maxDigits)
        {
            var order = 0;
            var floatReduced = DoIntReduction(src, maxDigits, ref order);
            string result;
            if (order == 0) result = floatReduced.ToString("F3", WEModData.InstanceWE.FormatCulture)[..(maxDigits + 1)];
            else if (order > orders.Length) return "∞";
            else result = floatReduced.ToString("F3", WEModData.InstanceWE.FormatCulture)[..(maxDigits + 1)];
            if (Regex.IsMatch(result, "^[0-9]$")) result = result[..-1];
            if (floatReduced < 0) result = result[..-1];
            return result + orders[order];
        }
        private static float DoIntReduction(float src, int maxDigits, ref int order)
        {
            if (Math.Abs(src) >= math.pow(10, maxDigits))
            {
                order++;
                return DoIntReduction(src * .001f, maxDigits, ref order);
            }
            return src;
        }
    }
}