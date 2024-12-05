using Unity.Collections;
using Unity.Entities;

namespace BelzontWE
{
    public struct WETextDataSourceMod : IComponentData
    {
        public FixedString32Bytes modName;
    }
}