#define BURST
//#define VERBOSE 
using Unity.Entities;

namespace BelzontWE
{
    public class WEBuiltinFn : IK45_WE_Fn
    {
        public const string LETTERS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public const string NUMBERS = "0123456789";
        public readonly static string[] DIGITS_ORDER = { NUMBERS, NUMBERS, LETTERS[..10], NUMBERS, LETTERS, LETTERS, LETTERS };
        public static string GetPlateContentTst(Entity entity) => GetPlateContentTst(entity.Index);
        public static string GetPlateContentTst(byte refNum) => GetPlateContentTst((int)refNum);
        public static string GetPlateContentTst(ushort refNum) => GetPlateContentTst((int)refNum);
        public static string GetPlateContentTst(int refNum)
        {
            var output = "";
            var idx = refNum + 68050000;
            for (int i = 0; i < DIGITS_ORDER.Length; i++)
            {
                output = DIGITS_ORDER[i][idx % DIGITS_ORDER[i].Length] + output;
                idx /= DIGITS_ORDER[i].Length;
            }

            return output.PadRight(7, '0');
        }
    }

}