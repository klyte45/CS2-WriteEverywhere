using NUnit.Framework;
using System.Reflection;
using Unity.Entities;
using BelzontWE.Utils;

namespace BelzontWE.Tests.Utils
{
    // IECSReader is a purely structural infra seam — no ECS world required.
    // Tests verify: interface contract, EntityManagerECSReader shape, and that
    // a custom test-double can implement the interface cleanly.

    [TestFixture]
    public class IECSReaderTests
    {
        // ── IECSReader interface shape ─────────────────────────────────────────

        [Test]
        public void IECSReader_IsInterface()
        {
            Assert.IsTrue(typeof(IECSReader).IsInterface);
        }

        [Test]
        public void IECSReader_HasTryGetComponentMethod()
        {
            var methods = typeof(IECSReader).GetMethods();
            var hasMethod = System.Array.Exists(methods, m => m.Name == "TryGetComponent");
            Assert.IsTrue(hasMethod);
        }

        [Test]
        public void IECSReader_HasTryGetBufferMethod()
        {
            var methods = typeof(IECSReader).GetMethods();
            var hasMethod = System.Array.Exists(methods, m => m.Name == "TryGetBuffer");
            Assert.IsTrue(hasMethod);
        }

        [Test]
        public void IECSReader_HasHasComponentMethod()
        {
            var methods = typeof(IECSReader).GetMethods();
            var hasMethod = System.Array.Exists(methods, m => m.Name == "HasComponent");
            Assert.IsTrue(hasMethod);
        }

        [Test]
        public void IECSReader_HasRawManagerProperty()
        {
            var prop = typeof(IECSReader).GetProperty("RawManager");
            Assert.IsNotNull(prop);
            Assert.AreEqual(typeof(EntityManager), prop.PropertyType);
        }

        // ── EntityManagerECSReader shape ──────────────────────────────────────

        [Test]
        public void EntityManagerECSReader_ImplementsIECSReader()
        {
            Assert.IsTrue(typeof(IECSReader).IsAssignableFrom(typeof(EntityManagerECSReader)));
        }

        [Test]
        public void EntityManagerECSReader_IsNotAbstract()
        {
            Assert.IsFalse(typeof(EntityManagerECSReader).IsAbstract);
        }

        [Test]
        public void EntityManagerECSReader_HasPublicConstructorAcceptingEntityManager()
        {
            var ctor = typeof(EntityManagerECSReader).GetConstructor(
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(EntityManager) },
                null);
            Assert.IsNotNull(ctor);
        }

        [Test]
        public void EntityManagerECSReader_RawManagerPropertyIsReadable()
        {
            var prop = typeof(EntityManagerECSReader).GetProperty("RawManager",
                BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(prop);
            Assert.IsTrue(prop.CanRead);
        }

        // ── Test-double can implement the interface ────────────────────────────

        private struct DummyComponent : IComponentData { }

        private class FakeECSReader : IECSReader
        {
            public EntityManager RawManager => default;
            public bool TryGetComponent<T>(Entity entity, out T componentData)
                where T : unmanaged, IComponentData
            { componentData = default; return false; }
            public bool TryGetBuffer<T>(Entity entity, bool readOnly, out DynamicBuffer<T> bufferData)
                where T : unmanaged, IBufferElementData
            { bufferData = default; return false; }
            public bool HasComponent<T>(Entity entity)
                where T : unmanaged, IComponentData
                => false;
        }

        [Test]
        public void FakeECSReader_ImplementsInterface_CanBeAssigned()
        {
            IECSReader reader = new FakeECSReader();
            Assert.IsNotNull(reader);
        }

        [Test]
        public void FakeECSReader_TryGetComponent_ReturnsFalse()
        {
            IECSReader reader = new FakeECSReader();
            var result = reader.TryGetComponent<DummyComponent>(Entity.Null, out _);
            Assert.IsFalse(result);
        }

        [Test]
        public void FakeECSReader_HasComponent_ReturnsFalse()
        {
            IECSReader reader = new FakeECSReader();
            var result = reader.HasComponent<DummyComponent>(Entity.Null);
            Assert.IsFalse(result);
        }
    }
}
