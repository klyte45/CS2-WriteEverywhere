using Unity.Collections;
using Unity.Entities;

namespace BelzontWE
{
    public struct WETextDataDirtyFormulae : IComponentData
    {
        public FixedString512Bytes vars;
        internal Entity geometry;
    }
}