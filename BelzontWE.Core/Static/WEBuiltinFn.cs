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
        public static string GetPlateContentTst(Entity entity)
        {
            var output = "";
            var idx = entity.Index + 68050000;
            for (int i = 0; i < DIGITS_ORDER.Length; i++)
            {
                output = DIGITS_ORDER[i][idx % DIGITS_ORDER[i].Length] + output;
                idx /= DIGITS_ORDER[i].Length;
            }

            return output.PadRight(7, '0');
        }
    }

}