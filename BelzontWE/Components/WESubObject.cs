using Unity.Entities;

namespace BelzontWE
{
    public struct WESubObject : IBufferElementData, ICleanupBufferElementData
    {
        public Entity m_SubObject;
    }
}
