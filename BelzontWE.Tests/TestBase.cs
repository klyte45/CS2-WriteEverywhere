using NUnit.Framework;

namespace BelzontWE.Tests
{
    /// <summary>
    /// Base class for all BelzontWE test fixtures.
    /// Provides shared SetUp / TearDown hooks so every test starts from a clean state.
    /// Inherit from this class when your test needs the shared setup; otherwise use
    /// a plain [TestFixture] class directly.
    /// </summary>
    public abstract class TestBase
    {
        [SetUp]
        public virtual void SetUp()
        {
            // Shared pre-test initialisation goes here.
            // Subclasses should call base.SetUp() first when overriding.
        }

        [TearDown]
        public virtual void TearDown()
        {
            // Shared post-test cleanup goes here.
            // Subclasses should call base.TearDown() last when overriding.
        }
    }
}
