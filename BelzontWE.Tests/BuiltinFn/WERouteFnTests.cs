using NUnit.Framework;
using System.Collections.Generic;
using Unity.Entities;
using BelzontWE.Builtin;

namespace BelzontWE.Tests.BuiltinFn
{
    [TestFixture]
    public class WERouteFnTests
    {
        // ── Default bindings are not null ─────────────────────────────────────

        [Test]
        public void GetTransportLineNumber_DefaultBinding_IsNotNull()
        {
            Assert.IsNotNull(WERouteFn.GetTransportLineNumber_binding);
        }

        [Test]
        public void GetWaypointStaticDestinationName_DefaultBinding_IsNotNull()
        {
            Assert.IsNotNull(WERouteFn.GetWaypointStaticDestinationName_binding);
        }

        [Test]
        public void GetWaypointStaticDestinationEntity_DefaultBinding_IsNotNull()
        {
            Assert.IsNotNull(WERouteFn.GetWaypointStaticDestinationEntity_binding);
        }

        // ── Null-fallback: binding = null ─────────────────────────────────────

        [Test]
        public void GetTransportLineNumber_WhenBindingNull_ReturnsFallback()
        {
            var original = WERouteFn.GetTransportLineNumber_binding;
            try
            {
                WERouteFn.GetTransportLineNumber_binding = null;
                var result = WERouteFn.GetTransportLineNumber(Entity.Null);
                Assert.AreEqual("<!>", result);
            }
            finally { WERouteFn.GetTransportLineNumber_binding = original; }
        }

        [Test]
        public void GetWaypointStaticDestinationName_WhenBindingNull_ReturnsFallback()
        {
            var original = WERouteFn.GetWaypointStaticDestinationName_binding;
            try
            {
                WERouteFn.GetWaypointStaticDestinationName_binding = null;
                var result = WERouteFn.GetWaypointStaticDestinationName(Entity.Null);
                Assert.AreEqual("???", result);
            }
            finally { WERouteFn.GetWaypointStaticDestinationName_binding = original; }
        }

        [Test]
        public void GetWaypointStaticDestinationEntity_WhenBindingNull_ReturnsDefault()
        {
            var original = WERouteFn.GetWaypointStaticDestinationEntity_binding;
            try
            {
                WERouteFn.GetWaypointStaticDestinationEntity_binding = null;
                var result = WERouteFn.GetWaypointStaticDestinationEntity(Entity.Null);
                Assert.AreEqual(default(Entity), result);
            }
            finally { WERouteFn.GetWaypointStaticDestinationEntity_binding = original; }
        }

        // ── Custom binding injection ───────────────────────────────────────────

        [Test]
        public void GetTransportLineNumber_WithCustomBinding_ReturnsExpected()
        {
            var original = WERouteFn.GetTransportLineNumber_binding;
            try
            {
                WERouteFn.GetTransportLineNumber_binding = _ => "42";
                var result = WERouteFn.GetTransportLineNumber(Entity.Null);
                Assert.AreEqual("42", result);
            }
            finally { WERouteFn.GetTransportLineNumber_binding = original; }
        }

        [Test]
        public void GetWaypointStaticDestinationName_WithCustomBinding_ReturnsExpected()
        {
            var original = WERouteFn.GetWaypointStaticDestinationName_binding;
            try
            {
                WERouteFn.GetWaypointStaticDestinationName_binding = _ => "Central Depot";
            var result = WERouteFn.GetWaypointStaticDestinationName(Entity.Null);
                Assert.AreEqual("Central Depot", result);
            }
            finally { WERouteFn.GetWaypointStaticDestinationName_binding = original; }
        }

        [Test]
        public void GetWaypointStaticDestinationEntity_WithCustomBinding_ReturnsExpected()
        {
            var expected = new Entity { Index = 99, Version = 2 };
            var original = WERouteFn.GetWaypointStaticDestinationEntity_binding;
            try
            {
                WERouteFn.GetWaypointStaticDestinationEntity_binding = _ => expected;
                var result = WERouteFn.GetWaypointStaticDestinationEntity(Entity.Null);
                Assert.AreEqual(expected, result);
            }
            finally { WERouteFn.GetWaypointStaticDestinationEntity_binding = original; }
        }

        // ── Binding receives correct entity argument ──────────────────────────

        [Test]
        public void GetTransportLineNumber_BindingReceivesPassedEntity()
        {
            var passed = new Entity { Index = 33, Version = 1 };
            Entity captured = Entity.Null;
            var original = WERouteFn.GetTransportLineNumber_binding;
            try
            {
                WERouteFn.GetTransportLineNumber_binding = e => { captured = e; return "0"; };
                WERouteFn.GetTransportLineNumber(passed);
                Assert.AreEqual(passed, captured);
            }
            finally { WERouteFn.GetTransportLineNumber_binding = original; }
        }

        [Test]
        public void GetWaypointStaticDestinationEntity_BindingReceivesPassedEntity()
        {
            var passed = new Entity { Index = 11, Version = 5 };
            Entity captured = Entity.Null;
            var original = WERouteFn.GetWaypointStaticDestinationEntity_binding;
            try
            {
                WERouteFn.GetWaypointStaticDestinationEntity_binding = e => { captured = e; return default; };
                WERouteFn.GetWaypointStaticDestinationEntity(passed);
                Assert.AreEqual(passed, captured);
            }
            finally { WERouteFn.GetWaypointStaticDestinationEntity_binding = original; }
        }

        // ── GetNthWaypoint pure early-exit paths ──────────────────────────────
        // NOTE: GetNthWaypoint references ConnectedRoute from the Game assembly.
        // JIT compiles the full method body and fails with FileNotFoundException
        // for Game.dll even on early-return paths. Tests are [Ignore]d.

        [Test, Ignore("JIT loads Game.dll types from method body — ConnectedRoute unavailable without game runtime")]
        public void GetNthWaypoint_EmptyVars_ReturnsDefault()
        {
            var vars = new Dictionary<string, string>();
            var result = WERouteFn.GetNthWaypoint(Entity.Null, vars);
            Assert.AreEqual(default(Entity), result);
        }

        [Test, Ignore("JIT loads Game.dll types from method body — ConnectedRoute unavailable without game runtime")]
        public void GetNthWaypoint_NonNumericIndex_ReturnsDefault()
        {
            var vars = new Dictionary<string, string> { ["!wp#"] = "abc" };
            var result = WERouteFn.GetNthWaypoint(Entity.Null, vars);
            Assert.AreEqual(default(Entity), result);
        }

        [Test, Ignore("JIT loads Game.dll types from method body — ConnectedRoute unavailable without game runtime")]
        public void GetNthWaypoint_TildeIndirectionMissingTarget_ReturnsDefault()
        {
            // "!wp#" = "~otherKey" but "otherKey" is not in vars → early return default
            var vars = new Dictionary<string, string> { ["!wp#"] = "~otherKey" };
            var result = WERouteFn.GetNthWaypoint(Entity.Null, vars);
            Assert.AreEqual(default(Entity), result);
        }

        [Test, Ignore("JIT loads Game.dll types from method body — ConnectedRoute unavailable without game runtime")]
        public void GetNthWaypoint_TildeIndirectionTargetNotNumeric_ReturnsDefault()
        {
            // "!wp#" = "~k", "k" = "xyz" → ushort.TryParse("xyz") fails → default
            var vars = new Dictionary<string, string> { ["!wp#"] = "~k", ["k"] = "xyz" };
            var result = WERouteFn.GetNthWaypoint(Entity.Null, vars);
            Assert.AreEqual(default(Entity), result);
        }
    }
}
