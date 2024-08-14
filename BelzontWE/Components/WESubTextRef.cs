using Unity.Entities;

namespace BelzontWE
{
    public struct WESubTextRef : IBufferElementData
    {
        public const uint CURRENT_VERSION = 0;
        public Entity m_weTextData;

    }

}