using System.Collections.Generic;
using Unity.Entities;

namespace BelzontWE.Builtin
{
    [WEBuiltinFunction("Instancing")]
    public static class WEInstancingFn
    {
        [WEFormula(typeof(int))]
        public static int GetCurrentIdx(Entity e, Dictionary<string, string> vars) => vars.TryGetValue(WEConstants.INSTANCE_IDX_KEY, out var value) ? int.TryParse(value, out var valueInt) ? valueInt : -2 : -1;

        [WEFormula(typeof(int))]
        public static int GetCurrentCount(string str, Dictionary<string, string> vars) => vars.TryGetValue(WEConstants.INSTANCE_COUNT_TOTAL_KEY, out var value) ? int.TryParse(value, out var valueInt) ? valueInt : -2 : -1;
    }
}
