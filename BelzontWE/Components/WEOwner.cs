using Unity.Entities;

namespace BelzontWE
{
    public struct WEOwner : IComponentData, ICleanupComponentData
    {
        public Entity m_weOwnerEntity;
    }
}
