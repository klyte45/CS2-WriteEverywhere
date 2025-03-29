using System.Collections.Generic;

namespace BelzontWE
{
    public partial class WEStringsBank
    {
        private static WEStringsBank _instance;
        public static WEStringsBank Instance => _instance ??= new WEStringsBank();

        private readonly List<string> listStorage = new() { "" };
        private readonly Dictionary<string, int> dictStorage = new() { [""] = 0 };
        public string this[int idx] => idx > listStorage.Count || idx < 0 ? null : listStorage[idx];

        public int this[string str]
        {
            get
            {
                if (str == null) return -1;
                if (!dictStorage.TryGetValue(str, out var index))
                {
                    index = listStorage.Count;
                    listStorage.Add(str);
                    dictStorage[str] = index;
                }
                return index;
            }
        }
    }
}