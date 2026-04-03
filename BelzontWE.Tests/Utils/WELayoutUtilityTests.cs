using NUnit.Framework;
using System.Collections.Generic;
using Unity.Entities;
using BelzontWE;
using BelzontWE.Utils;
using static BelzontWE.WELayoutUtility;

namespace BelzontWE.Tests.Utils
{
    // WELayoutUtility.CommonDataSetup is internal and made accessible via InternalsVisibleTo.
    // NOTE: NSubstitute cannot mock IECSReader because the generic methods have
    // 'unmanaged' constraints that Reflection.Emit cannot express in proxies.
    // A hand-written FakeECSReader is used instead — functionally equivalent.

    [TestFixture]
    public class WELayoutUtilityTests
    {
        // ── FakeECSReader that controls TryGetBuffer return value ─────────────

        private struct FakeComponent : IComponentData { }

        private class FakeECSReader : IECSReader
        {
            public bool TryGetBufferResult { get; set; } = false;
            public DynamicBuffer<WESubTextRef> BufferToReturn { get; set; } = default;

            public EntityManager RawManager => default;

            public bool TryGetComponent<T>(Entity entity, out T componentData)
                where T : unmanaged, IComponentData
            { componentData = default; return false; }

            public bool TryGetBuffer<T>(Entity entity, bool readOnly, out DynamicBuffer<T> bufferData)
                where T : unmanaged, IBufferElementData
            {
                if (typeof(T) == typeof(WESubTextRef) && TryGetBufferResult)
                {
                    bufferData = (DynamicBuffer<T>)(object)BufferToReturn;
                    return true;
                }
                bufferData = default;
                return false;
            }

            public bool HasComponent<T>(Entity entity)
                where T : unmanaged, IComponentData
                => false;
        }

        [TearDown]
        public void TearDown()
        {
            WELayoutUtility.Reader = null;
        }

        // ── Reader injection field ─────────────────────────────────────────────

        [Test]
        public void Reader_DefaultValue_IsNull()
        {
            Assert.IsNull(WELayoutUtility.Reader);
        }

        [Test]
        public void Reader_CanBeAssigned_AndRetrieved()
        {
            var fake = new FakeECSReader();
            WELayoutUtility.Reader = fake;
            Assert.AreSame(fake, WELayoutUtility.Reader);
        }

        // ── CommonDataSetup: child target entity selection ────────────────────

        private static Entity E(int index) => new Entity { Index = index, Version = 1 };

        private static WETextDataXmlTree MinimalTree()
            => new WETextDataXmlTree { self = new WETextDataXml { itemName = "test" } };

        [Test]
        public void CommonDataSetup_TargetIsTarget_ReturnsTargetEntity()
        {
            var parent = E(1); var target = E(2); var newEnt = E(3);
            var result = WELayoutUtility.CommonDataSetup(
                MinimalTree(), parent, target, ParentEntityMode.TARGET_IS_TARGET, newEnt,
                out _, out _, out _, out _);
            Assert.AreEqual(target, result);
        }

        [Test]
        public void CommonDataSetup_TargetIsSelf_ReturnsNewEntity()
        {
            var parent = E(1); var target = E(2); var newEnt = E(3);
            var result = WELayoutUtility.CommonDataSetup(
                MinimalTree(), parent, target, ParentEntityMode.TARGET_IS_SELF, newEnt,
                out _, out _, out _, out _);
            Assert.AreEqual(newEnt, result);
        }

        [Test]
        public void CommonDataSetup_TargetIsSelfForParent_ReturnsParentEntity()
        {
            var parent = E(1); var target = E(2); var newEnt = E(3);
            var result = WELayoutUtility.CommonDataSetup(
                MinimalTree(), parent, target, ParentEntityMode.TARGET_IS_SELF_FOR_PARENT, newEnt,
                out _, out _, out _, out _);
            Assert.AreEqual(parent, result);
        }

        [Test]
        public void CommonDataSetup_TargetIsParent_ReturnsParentEntity()
        {
            var parent = E(1); var target = E(2); var newEnt = E(3);
            var result = WELayoutUtility.CommonDataSetup(
                MinimalTree(), parent, target, ParentEntityMode.TARGET_IS_PARENT, newEnt,
                out _, out _, out _, out _);
            Assert.AreEqual(parent, result);
        }

        [Test]
        public void CommonDataSetup_TargetIsSelfParentHasTarget_ReturnsNewEntity()
        {
            var parent = E(1); var target = E(2); var newEnt = E(3);
            var result = WELayoutUtility.CommonDataSetup(
                MinimalTree(), parent, target, ParentEntityMode.TARGET_IS_SELF_PARENT_HAS_TARGET, newEnt,
                out _, out _, out _, out _);
            Assert.AreEqual(newEnt, result);
        }

        // ── CommonDataSetup: main.ParentEntity and main.TargetEntity ─────────

        [Test]
        public void CommonDataSetup_TargetIsTarget_SetsMainParentEntity()
        {
            var parent = E(10); var target = E(20); var newEnt = E(30);
            WELayoutUtility.CommonDataSetup(
                MinimalTree(), parent, target, ParentEntityMode.TARGET_IS_TARGET, newEnt,
                out var main, out _, out _, out _);
            Assert.AreEqual(parent, main.ParentEntity);
        }

        [Test]
        public void CommonDataSetup_TargetIsTarget_SetsMainTargetEntityToTarget()
        {
            var parent = E(10); var target = E(20); var newEnt = E(30);
            WELayoutUtility.CommonDataSetup(
                MinimalTree(), parent, target, ParentEntityMode.TARGET_IS_TARGET, newEnt,
                out var main, out _, out _, out _);
            Assert.AreEqual(target, main.TargetEntity);
        }

        [Test]
        public void CommonDataSetup_TargetIsSelfForParent_SetsMainTargetEntityToNewEntity()
        {
            // parentTarget = newEntity when TARGET_IS_SELF_FOR_PARENT
            var parent = E(10); var target = E(20); var newEnt = E(30);
            WELayoutUtility.CommonDataSetup(
                MinimalTree(), parent, target, ParentEntityMode.TARGET_IS_SELF_FOR_PARENT, newEnt,
                out var main, out _, out _, out _);
            Assert.AreEqual(newEnt, main.TargetEntity);
        }
    }
}
