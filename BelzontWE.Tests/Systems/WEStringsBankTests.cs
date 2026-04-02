using NUnit.Framework;

namespace BelzontWE.Tests.Systems
{
    /// <summary>
    /// Tests for WEStringsBank: a bidirectional string/index mapping with a singleton instance.
    /// Tests construct fresh instances to avoid shared-state coupling between tests.
    /// </summary>
    [TestFixture]
    public class WEStringsBankTests
    {
        private WEStringsBank _bank = null!;

        [SetUp]
        public void SetUp() => _bank = new WEStringsBank();

        // ── Initial state ────────────────────────────────────────────────────

        [Test]
        public void InitialState_EmptyString_IsAtIndex_Zero()
            => Assert.That(_bank[0], Is.EqualTo(string.Empty));

        [Test]
        public void InitialState_EmptyStringIndex_IsZero()
            => Assert.That(_bank[string.Empty], Is.EqualTo(0));

        [Test]
        public void InitialState_IndexOne_ReturnsNull()
            => Assert.That(_bank[1], Is.Null);

        [Test]
        public void InitialState_NegativeIndex_ReturnsNull()
            => Assert.That(_bank[-1], Is.Null);

        // ── Null lookup ───────────────────────────────────────────────────────

        [Test]
        public void NullKey_ReturnsMinusOne()
            => Assert.That(_bank[null!], Is.EqualTo(-1));

        // ── New string insertion ──────────────────────────────────────────────

        [Test]
        public void NewString_ReceivesIndex_One()
        {
            var idx = _bank["hello"];
            Assert.That(idx, Is.EqualTo(1));
        }

        [Test]
        public void InsertedString_CanBeRetrievedByIndex()
        {
            var idx = _bank["hello"];
            Assert.That(_bank[idx], Is.EqualTo("hello"));
        }

        [Test]
        public void SecondNewString_ReceivesIndex_Two()
        {
            _ = _bank["first"];
            var idx = _bank["second"];
            Assert.That(idx, Is.EqualTo(2));
        }

        [Test]
        public void DuplicateString_ReturnsSameIndex()
        {
            var idx1 = _bank["same"];
            var idx2 = _bank["same"];
            Assert.That(idx2, Is.EqualTo(idx1));
        }

        [Test]
        public void DuplicateString_DoesNotGrowCollection()
        {
            _ = _bank["a"];
            _ = _bank["a"];
            // After adding "a" once, next new string should get index 2, not 3
            var idx = _bank["b"];
            Assert.That(idx, Is.EqualTo(2));
        }

        // ── Index bounds ──────────────────────────────────────────────────────

        [Test]
        public void IndexEqualToCount_ReturnsNull()
        {
            // Count is 1 initially; index 1 is out of range
            Assert.That(_bank[1], Is.Null);
        }

        [Test]
        public void IndexAboveCount_ReturnsNull()
        {
            _ = _bank["a"]; // index 1
            Assert.That(_bank[100], Is.Null);
        }

        [Test]
        public void LargeNegativeIndex_ReturnsNull()
            => Assert.That(_bank[int.MinValue], Is.Null);

        // ── Singleton contract ────────────────────────────────────────────────

        [Test]
        public void Instance_ReturnsSameObject_OnMultipleCalls()
        {
            var a = WEStringsBank.Instance;
            var b = WEStringsBank.Instance;
            Assert.That(a, Is.SameAs(b));
        }

        [Test]
        public void Instance_IsNotNull()
            => Assert.That(WEStringsBank.Instance, Is.Not.Null);
    }
}
