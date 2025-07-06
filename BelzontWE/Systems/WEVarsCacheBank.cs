using System.Collections.Generic;
using System.Linq;
using Unity.Collections;

namespace BelzontWE
{
    public class WEVarsCacheBank
    {
        private static WEVarsCacheBank _instance;
        public static WEVarsCacheBank Instance => _instance ??= new WEVarsCacheBank();

        private readonly List<Dictionary<string, string>> listStorage = new() { new Dictionary<string, string>() };
        private readonly Dictionary<FixedString512Bytes, int> dictStorage = new() { [""] = 0 };
        public Dictionary<string, string> this[int idx] => idx > listStorage.Count || idx < 0 ? null : listStorage[idx];

        public int this[FixedString512Bytes str]
        {
            get
            {
                if (!dictStorage.TryGetValue(str, out var index))
                {
                    index = listStorage.Count;
                    listStorage.Add(str.ToString().Split(WERendererSystem.VARIABLE_ITEM_SEPARATOR).Select(x => x.Split(WERendererSystem.VARIABLE_KV_SEPARATOR, 2))
                        .Where(x => x.Length == 2).GroupBy(x => x[0]).ToDictionary(x => x.Key, x => x.Last()[1]));
                    dictStorage[str] = index;
                }
                return index;
            }
        }
    }
}