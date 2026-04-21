using Unity.Collections;
using Unity.Entities;

namespace BelzontWE
{
    public struct WEInheritedVarsCache : IComponentData, IEnableableComponent
    {
        public FixedString512Bytes vars;
    }
}
