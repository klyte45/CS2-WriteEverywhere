using NUnit.Framework;

namespace BelzontWE.Tests.Systems
{
    /// <summary>
    /// Tests for WEVarsCacheBank.
    /// NOTE: The bank's indexer uses FixedString512Bytes (Unity.Collections) as dictionary key.
    /// FixedString512Bytes has default-interface-method implementations that are incompatible
    /// with .NET Framework 4.8 runtime — instantiating the class causes TypeLoadException
    /// outside of the game process.  Tests that require bank instantiation are therefore
    /// permanently Ignored until the test project moves to a .NET 5+ TFM or the class is
    /// refactored to use a non-Unity key type.
    /// </summary>
    [TestFixture]
    public class WEVarsCacheBankTests
    {
        [Test]
        [Ignore("WEVarsCacheBank requires Unity.Collections runtime — TypeLoadException on net48 test runner")]
        public void Instance_IsNotNull()
            => Assert.That(WEVarsCacheBank.Instance, Is.Not.Null);

        [Test]
        [Ignore("WEVarsCacheBank requires Unity.Collections runtime — TypeLoadException on net48 test runner")]
        public void Instance_ReturnsSameObject_OnMultipleCalls()
        {
            var a = WEVarsCacheBank.Instance;
            var b = WEVarsCacheBank.Instance;
            Assert.That(a, Is.SameAs(b));
        }
    }
}
