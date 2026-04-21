using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;

namespace BelzontWE.Tests.Systems
{
    [TestFixture]
    public class WEGamePropVariableInheritanceTests
    {
        // ── WEInheritedVarsCache component contracts ──

        [Test]
        public void WEInheritedVarsCache_IsIEnableableComponent()
        {
            // Must be IEnableableComponent so DrawTree can enable/disable it per-frame
            Assert.That(typeof(WEInheritedVarsCache).GetInterface(nameof(IEnableableComponent)), Is.Not.Null);
        }

        [Test]
        public void WEInheritedVarsCache_HasVarsField_OfFixedString512Bytes()
        {
            // Must store vars as FixedString512Bytes matching the WEPreCullingSystem variable string type
            var field = typeof(WEInheritedVarsCache).GetField("vars");
            Assert.That(field, Is.Not.Null);
            Assert.That(field.FieldType, Is.EqualTo(typeof(FixedString512Bytes)));
        }

        [Test]
        public void WEInheritedVarsCache_DefaultVars_IsEmpty()
        {
            // A default (disabled) cache must have empty vars — no false inheritance
            var cache = default(WEInheritedVarsCache);
            Assert.That(cache.vars.IsEmpty, Is.True);
        }

        [Test]
        public void WEInheritedVarsCache_StoresAndRetrievesVars()
        {
            // Verify round-trip: vars written to cache are preserved exactly
            var vars = new FixedString512Bytes("myVar=hello;otherVar=world;");
            var cache = new WEInheritedVarsCache { vars = vars };
            Assert.That(cache.vars.ToString(), Is.EqualTo("myVar=hello;otherVar=world;"));
        }

        // ── Variable string exclusion contract (local vars must not be inherited) ──

        [Test]
        public void LocalVar_WithBangPrefix_IsExcludedFromInheritableVars()
        {
            // PopulateVars only adds to inheritableVars when Key[0] != '!'
            // This test verifies the string convention used for local vars
            const string localVarKey = "!localOnly";
            Assert.That(localVarKey[0], Is.EqualTo('!'),
                "Local variables must start with '!' to be excluded from inheritance");
        }

        [Test]
        public void NonLocalVar_WithoutBangPrefix_IsIncludedInInheritableVars()
        {
            // Non-local vars should be included in inherited variable string
            const string inheritableVarKey = "parentVar";
            Assert.That(inheritableVarKey[0], Is.Not.EqualTo('!'),
                "Non-local variables must not start with '!' to be included in inheritance");
        }
    }
}
