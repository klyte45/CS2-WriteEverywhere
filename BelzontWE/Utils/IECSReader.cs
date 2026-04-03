using Colossal.Entities;
using Unity.Entities;

namespace BelzontWE.Utils
{
    /// <summary>
    /// Abstracts ECS read operations so callers can be tested with a test double
    /// instead of requiring a real Unity EntityManager.
    /// </summary>
    public interface IECSReader
    {
        bool TryGetComponent<T>(Entity entity, out T componentData) where T : unmanaged, IComponentData;
        bool TryGetBuffer<T>(Entity entity, bool readOnly, out DynamicBuffer<T> bufferData) where T : unmanaged, IBufferElementData;
        bool HasComponent<T>(Entity entity) where T : unmanaged, IComponentData;
        EntityManager RawManager { get; }
    }

    /// <summary>
    /// Production implementation that delegates to a real EntityManager.
    /// Construct with the EntityManager from World.DefaultGameObjectInjectionWorld.
    /// </summary>
    public sealed class EntityManagerECSReader : IECSReader
    {
        private readonly EntityManager _em;

        public EntityManagerECSReader(EntityManager em)
        {
            _em = em;
        }

        public EntityManager RawManager => _em;

        public bool TryGetComponent<T>(Entity entity, out T componentData) where T : unmanaged, IComponentData
            => _em.TryGetComponent(entity, out componentData);

        public bool TryGetBuffer<T>(Entity entity, bool readOnly, out DynamicBuffer<T> bufferData) where T : unmanaged, IBufferElementData
            => _em.TryGetBuffer(entity, readOnly, out bufferData);

        public bool HasComponent<T>(Entity entity) where T : unmanaged, IComponentData
            => _em.HasComponent<T>(entity);
    }
}
