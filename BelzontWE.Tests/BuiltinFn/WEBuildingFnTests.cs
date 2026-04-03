using NUnit.Framework;
using System;
using Unity.Entities;
using BelzontWE.Builtin;

namespace BelzontWE.Tests.BuiltinFn
{
    [TestFixture]
    public class WEBuildingFnTests
    {
        // ── Default bindings are not null ─────────────────────────────────────

        [Test]
        public void GetBuildingRoad_DefaultBinding_IsNotNull()
        {
            Assert.IsNotNull(WEBuildingFn.GetBuildingRoad_binding);
        }

        [Test]
        public void GetBuildingRoadNumber_DefaultBinding_IsNotNull()
        {
            Assert.IsNotNull(WEBuildingFn.GetBuildingRoadNumber_binding);
        }

        [Test]
        public void GetBuildingMainRenter_DefaultBinding_IsNotNull()
        {
            Assert.IsNotNull(WEBuildingFn.GetBuildingMainRenter_binding);
        }

        // ── Null-fallback: binding = null ─────────────────────────────────────

        [Test]
        public void GetBuildingRoad_WhenBindingNull_ReturnsEntityNull()
        {
            var original = WEBuildingFn.GetBuildingRoad_binding;
            try
            {
                WEBuildingFn.GetBuildingRoad_binding = null;
                var result = WEBuildingFn.GetBuildingRoad(Entity.Null);
                Assert.AreEqual(Entity.Null, result);
            }
            finally { WEBuildingFn.GetBuildingRoad_binding = original; }
        }

        [Test]
        public void GetBuildingRoadNumber_WhenBindingNull_ReturnsNA()
        {
            var original = WEBuildingFn.GetBuildingRoadNumber_binding;
            try
            {
                WEBuildingFn.GetBuildingRoadNumber_binding = null;
                var result = WEBuildingFn.GetBuildingRoadNumber(Entity.Null);
                Assert.AreEqual("N/A", result);
            }
            finally { WEBuildingFn.GetBuildingRoadNumber_binding = original; }
        }

        [Test]
        public void GetBuildingMainRenter_WhenBindingNull_ReturnsEntityNull()
        {
            var original = WEBuildingFn.GetBuildingMainRenter_binding;
            try
            {
                WEBuildingFn.GetBuildingMainRenter_binding = null;
                var result = WEBuildingFn.GetBuildingMainRenter(Entity.Null);
                Assert.AreEqual(Entity.Null, result);
            }
            finally { WEBuildingFn.GetBuildingMainRenter_binding = original; }
        }

        // ── Custom binding injection ───────────────────────────────────────────

        [Test]
        public void GetBuildingRoad_WithCustomBinding_ReturnsExpectedEntity()
        {
            var expected = new Entity { Index = 42, Version = 1 };
            var original = WEBuildingFn.GetBuildingRoad_binding;
            try
            {
                WEBuildingFn.GetBuildingRoad_binding = _ => expected;
                var result = WEBuildingFn.GetBuildingRoad(Entity.Null);
                Assert.AreEqual(expected, result);
            }
            finally { WEBuildingFn.GetBuildingRoad_binding = original; }
        }

        [Test]
        public void GetBuildingRoadNumber_WithCustomBinding_ReturnsExpectedString()
        {
            var original = WEBuildingFn.GetBuildingRoadNumber_binding;
            try
            {
                WEBuildingFn.GetBuildingRoadNumber_binding = _ => "99B";
                var result = WEBuildingFn.GetBuildingRoadNumber(Entity.Null);
                Assert.AreEqual("99B", result);
            }
            finally { WEBuildingFn.GetBuildingRoadNumber_binding = original; }
        }

        [Test]
        public void GetBuildingMainRenter_WithCustomBinding_ReturnsExpectedEntity()
        {
            var expected = new Entity { Index = 7, Version = 3 };
            var original = WEBuildingFn.GetBuildingMainRenter_binding;
            try
            {
                WEBuildingFn.GetBuildingMainRenter_binding = _ => expected;
                var result = WEBuildingFn.GetBuildingMainRenter(Entity.Null);
                Assert.AreEqual(expected, result);
            }
            finally { WEBuildingFn.GetBuildingMainRenter_binding = original; }
        }

        // ── Binding receives correct entity argument ──────────────────────────

        [Test]
        public void GetBuildingRoad_BindingReceivesPassedEntity()
        {
            var passed = new Entity { Index = 100, Version = 2 };
            Entity captured = Entity.Null;
            var original = WEBuildingFn.GetBuildingRoad_binding;
            try
            {
                WEBuildingFn.GetBuildingRoad_binding = e => { captured = e; return Entity.Null; };
                WEBuildingFn.GetBuildingRoad(passed);
                Assert.AreEqual(passed, captured);
            }
            finally { WEBuildingFn.GetBuildingRoad_binding = original; }
        }

        [Test]
        public void GetBuildingRoadNumber_BindingReceivesPassedEntity()
        {
            var passed = new Entity { Index = 55, Version = 1 };
            Entity captured = Entity.Null;
            var original = WEBuildingFn.GetBuildingRoadNumber_binding;
            try
            {
                WEBuildingFn.GetBuildingRoadNumber_binding = e => { captured = e; return ""; };
                WEBuildingFn.GetBuildingRoadNumber(passed);
                Assert.AreEqual(passed, captured);
            }
            finally { WEBuildingFn.GetBuildingRoadNumber_binding = original; }
        }

        [Test]
        public void GetBuildingMainRenter_BindingReceivesPassedEntity()
        {
            var passed = new Entity { Index = 77, Version = 4 };
            Entity captured = Entity.Null;
            var original = WEBuildingFn.GetBuildingMainRenter_binding;
            try
            {
                WEBuildingFn.GetBuildingMainRenter_binding = e => { captured = e; return Entity.Null; };
                WEBuildingFn.GetBuildingMainRenter(passed);
                Assert.AreEqual(passed, captured);
            }
            finally { WEBuildingFn.GetBuildingMainRenter_binding = original; }
        }

        // ── Binding isolation: changing one does not affect others ────────────

        [Test]
        public void GetBuildingRoad_BindingIsolation_DoesNotAffectRoadNumber()
        {
            var originalRoad = WEBuildingFn.GetBuildingRoad_binding;
            var originalNum = WEBuildingFn.GetBuildingRoadNumber_binding;
            try
            {
                WEBuildingFn.GetBuildingRoad_binding = null;
                WEBuildingFn.GetBuildingRoadNumber_binding = _ => "7A";
                // road binding null → fallback; number binding custom → "7A"
                Assert.AreEqual(Entity.Null, WEBuildingFn.GetBuildingRoad(Entity.Null));
                Assert.AreEqual("7A", WEBuildingFn.GetBuildingRoadNumber(Entity.Null));
            }
            finally
            {
                WEBuildingFn.GetBuildingRoad_binding = originalRoad;
                WEBuildingFn.GetBuildingRoadNumber_binding = originalNum;
            }
        }

        [Test]
        public void GetBuildingRoad_BindingIsolation_DoesNotAffectMainRenter()
        {
            var originalRoad = WEBuildingFn.GetBuildingRoad_binding;
            var originalRenter = WEBuildingFn.GetBuildingMainRenter_binding;
            try
            {
                var renterEntity = new Entity { Index = 5, Version = 1 };
                WEBuildingFn.GetBuildingRoad_binding = null;
                WEBuildingFn.GetBuildingMainRenter_binding = _ => renterEntity;
                Assert.AreEqual(Entity.Null, WEBuildingFn.GetBuildingRoad(Entity.Null));
                Assert.AreEqual(renterEntity, WEBuildingFn.GetBuildingMainRenter(Entity.Null));
            }
            finally
            {
                WEBuildingFn.GetBuildingRoad_binding = originalRoad;
                WEBuildingFn.GetBuildingMainRenter_binding = originalRenter;
            }
        }

        [Test]
        public void GetBuildingRoadNumber_EmptyStringBinding_ReturnsEmpty()
        {
            var original = WEBuildingFn.GetBuildingRoadNumber_binding;
            try
            {
                WEBuildingFn.GetBuildingRoadNumber_binding = _ => string.Empty;
                var result = WEBuildingFn.GetBuildingRoadNumber(Entity.Null);
                Assert.AreEqual(string.Empty, result);
            }
            finally { WEBuildingFn.GetBuildingRoadNumber_binding = original; }
        }
    }
}
