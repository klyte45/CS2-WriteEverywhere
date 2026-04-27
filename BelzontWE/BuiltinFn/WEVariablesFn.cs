using System.Collections.Generic;

namespace BelzontWE.Builtin
{
    [WEBuiltinFunction("Variables")]
    public class WEVariablesFn
    {
        [WEFormula(typeof(string))]
        public static string GetVariable(string varName, Dictionary<string, string> vars) => vars.TryGetValue(varName, out var value) ? value : $"<?{varName}?>";
        [WEFormula(typeof(int))]
        public static int GetVariableAsInt(string varName, Dictionary<string, string> vars) => int.TryParse(GetVariable(varName, vars), out int val) ? val : int.MinValue;
        [WEFormula(typeof(float))]
        public static float GetVariableAsFloat(string varName, Dictionary<string, string> vars) => float.TryParse(GetVariable(varName, vars), out var val) ? val : float.NaN;
    }

}
