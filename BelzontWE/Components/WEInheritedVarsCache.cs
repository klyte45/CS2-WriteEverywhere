using Unity.Collections;
using Unity.Entities;

namespace BelzontWE
{
    public struct WEInheritedVarsCache : IComponentData
    {
        public FixedString512Bytes vars;
    }
}
