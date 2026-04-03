using BelzontWE.Utils;
using Unity.Entities;

namespace BelzontWE.Tests.Utils
{
    /// <summary>
    /// Minimal IECSReader test double.
    /// Safe for no-formulae paths where EntityManager is never dereferenced.
    /// </summary>
    internal class NullECSReader : IECSReader
    {
        public EntityManager RawManager => default;

        public bool TryGetComponent<T>(Entity entity, out T componentData)
            where T : unmanaged, IComponentData
        {
            componentData = default;
            return false;
        }

        public bool TryGetBuffer<T>(Entity entity, bool readOnly, out DynamicBuffer<T> bufferData)
            where T : unmanaged, IBufferElementData
        {
            bufferData = default;
            return false;
        }

        public bool HasComponent<T>(Entity entity) where T : unmanaged, IComponentData => false;
    }
}
