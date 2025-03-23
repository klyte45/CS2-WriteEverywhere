using System.Collections.Generic;
using System.Linq;
using Unity.Entities;

namespace BelzontWE.Builtin
{
    public static class WEParameterFn
    {
        public static string PrintVariables(Entity reference, Dictionary<string, string> variables) => string.Join(";", variables.Select(x => $"{x.Key}={x.Value}"));
    }
}