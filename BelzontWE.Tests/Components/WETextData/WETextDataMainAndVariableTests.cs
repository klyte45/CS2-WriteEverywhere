using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;

namespace BelzontWE.Tests.Components
{
    [TestFixture]
    public class WETextDataMainTests
    {
        // ── Property setters ──────────────────────────────────────────────

        [Test]
        public void ItemName_SetAndGet()
        {
            var m = new WETextDataMain();
            m.ItemName = new FixedString32Bytes("test-name");
            Assert.AreEqual(new FixedString32Bytes("test-name"), m.ItemName);
        }

        [Test]
        public void TargetEntity_SetAndGet()
        {
            var m = new WETextDataMain();
            var e = new Entity { Index = 42, Version = 1 };
            m.TargetEntity = e;
            Assert.AreEqual(e, m.TargetEntity);
        }

        [Test]
        public void ParentEntity_SetAndGet()
        {
            var m = new WETextDataMain();
            var e = new Entity { Index = 10, Version = 2 };
            m.ParentEntity = e;
            Assert.AreEqual(e, m.ParentEntity);
        }

        [Test]
        public void ItemName_OverwritePreviousValue()
        {
            var m = new WETextDataMain();
            m.ItemName = new FixedString32Bytes("first");
            m.ItemName = new FixedString32Bytes("second");
            Assert.AreEqual(new FixedString32Bytes("second"), m.ItemName);
        }

        // ── CreateDefault ─────────────────────────────────────────────────

        [Test]
        public void CreateDefault_ItemNameIsNewItem()
        {
            var target = new Entity { Index = 5, Version = 0 };
            var m = WETextDataMain.CreateDefault(target);
            Assert.AreEqual(new FixedString32Bytes("New item"), m.ItemName);
        }

        [Test]
        public void CreateDefault_TargetEntityMatchesPassed()
        {
            var target = new Entity { Index = 7, Version = 0 };
            var m = WETextDataMain.CreateDefault(target);
            Assert.AreEqual(target, m.TargetEntity);
        }

        [Test]
        public void CreateDefault_ParentEntityDefaultsToTarget()
        {
            var target = new Entity { Index = 9, Version = 0 };
            var m = WETextDataMain.CreateDefault(target);
            Assert.AreEqual(target, m.ParentEntity);
        }

        [Test]
        public void CreateDefault_ExplicitParentUsedWhenProvided()
        {
            var target = new Entity { Index = 9, Version = 0 };
            var parent = new Entity { Index = 3, Version = 0 };
            var m = WETextDataMain.CreateDefault(target, parent);
            Assert.AreEqual(parent, m.ParentEntity);
        }

        // ── SetNewParent — type-level tests (no EntityManager) ────────────

        [Test]
        public void SetNewParent_EntityNull_SetsParentAndReturnsTrue()
        {
            var target = new Entity { Index = 1, Version = 0 };
            var m = WETextDataMain.CreateDefault(target);
            // Entity.Null bypasses the condition: (e != Entity.Null) is false → falls through
            var result = m.SetNewParent(Entity.Null, default);
            Assert.IsTrue(result);
            Assert.AreEqual(Entity.Null, m.ParentEntity);
        }

        [Test]
        public void SetNewParent_SameAsTarget_SetsParentAndReturnsTrue()
        {
            var target = new Entity { Index = 5, Version = 0 };
            var m = WETextDataMain.CreateDefault(target);
            // e == TargetEntity → condition (e != TargetEntity) is false → falls through
            var result = m.SetNewParent(target, default);
            Assert.IsTrue(result);
            Assert.AreEqual(target, m.ParentEntity);
        }
    }

    [TestFixture]
    public class WETextDataVariableTests
    {
        // ── Initial state ─────────────────────────────────────────────────

        [Test]
        public void InitialState_KeyIsEmpty()
        {
            var v = new WETextDataVariable();
            Assert.AreEqual(new FixedString32Bytes(""), v.Key);
        }

        [Test]
        public void InitialState_ValueIsEmpty()
        {
            var v = new WETextDataVariable();
            Assert.AreEqual(new FixedString32Bytes(""), v.Value);
        }

        // ── Key/Value round-trip ──────────────────────────────────────────

        [Test]
        public void Key_SetAndGet()
        {
            var v = new WETextDataVariable();
            v.Key = new FixedString32Bytes("myKey");
            Assert.AreEqual(new FixedString32Bytes("myKey"), v.Key);
        }

        [Test]
        public void Value_SetAndGet()
        {
            var v = new WETextDataVariable();
            v.Value = new FixedString32Bytes("myValue");
            Assert.AreEqual(new FixedString32Bytes("myValue"), v.Value);
        }

        [Test]
        public void Key_DoesNotAffectValue()
        {
            var v = new WETextDataVariable();
            v.Value = new FixedString32Bytes("original");
            v.Key = new FixedString32Bytes("newKey");
            Assert.AreEqual(new FixedString32Bytes("original"), v.Value);
        }

        [Test]
        public void Value_DoesNotAffectKey()
        {
            var v = new WETextDataVariable();
            v.Key = new FixedString32Bytes("keyX");
            v.Value = new FixedString32Bytes("newValue");
            Assert.AreEqual(new FixedString32Bytes("keyX"), v.Key);
        }

        [Test]
        public void KeyAndValue_IndepedentRoundTrip()
        {
            var v = new WETextDataVariable
            {
                Key = new FixedString32Bytes("k"),
                Value = new FixedString32Bytes("v")
            };
            Assert.AreEqual(new FixedString32Bytes("k"), v.Key);
            Assert.AreEqual(new FixedString32Bytes("v"), v.Value);
        }
    }
}
