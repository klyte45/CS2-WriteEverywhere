using System.Collections.Generic;
using System.Linq;
using Unity.Entities;

namespace BelzontWE.Builtin
{
    public static class WEParameterFn
    {
        private const string REL_VAR_PREFIX = "!!r";
        public static string PrintVariables(Entity reference, Dictionary<string, string> variables) => string.Join(";", variables.Select(x => $"{x.Key}={x.Value}"));

        private static string GetRelVarAsString(int i, Dictionary<string, string> variables) => variables.TryGetValue(REL_VAR_PREFIX + i, out var key) && variables.TryGetValue(key, out var value) ? value : string.Empty;
        private static int GetRelVarAsInt(int i, Dictionary<string, string> variables) => int.TryParse(GetRelVarAsString(i, variables), out var intValue) ? intValue : 0;

        public static string RelVarStr1(Entity reference, Dictionary<string, string> variables) => GetRelVarAsString(1, variables);
        public static string RelVarStr2(Entity reference, Dictionary<string, string> variables) => GetRelVarAsString(2, variables);
        public static string RelVarStr3(Entity reference, Dictionary<string, string> variables) => GetRelVarAsString(3, variables);
        public static string RelVarStr4(Entity reference, Dictionary<string, string> variables) => GetRelVarAsString(4, variables);
        public static string RelVarStr5(Entity reference, Dictionary<string, string> variables) => GetRelVarAsString(5, variables);
        public static string RelVarStr6(Entity reference, Dictionary<string, string> variables) => GetRelVarAsString(6, variables);
        public static string RelVarStr7(Entity reference, Dictionary<string, string> variables) => GetRelVarAsString(7, variables);
        public static string RelVarStr8(Entity reference, Dictionary<string, string> variables) => GetRelVarAsString(8, variables);

        public static int RelVarInt1(Entity reference, Dictionary<string, string> variables) => GetRelVarAsInt(1, variables);
        public static int RelVarInt2(Entity reference, Dictionary<string, string> variables) => GetRelVarAsInt(2, variables);
        public static int RelVarInt3(Entity reference, Dictionary<string, string> variables) => GetRelVarAsInt(3, variables);
        public static int RelVarInt4(Entity reference, Dictionary<string, string> variables) => GetRelVarAsInt(4, variables);
        public static int RelVarInt5(Entity reference, Dictionary<string, string> variables) => GetRelVarAsInt(5, variables);
        public static int RelVarInt6(Entity reference, Dictionary<string, string> variables) => GetRelVarAsInt(6, variables);
        public static int RelVarInt7(Entity reference, Dictionary<string, string> variables) => GetRelVarAsInt(7, variables);
        public static int RelVarInt8(Entity reference, Dictionary<string, string> variables) => GetRelVarAsInt(8, variables);
    }
}