using NUnit.Framework;

namespace BelzontWE.Tests
{
    /// <summary>
    /// Minimal smoke tests that verify the test pipeline itself is wired up correctly.
    /// These tests have no game-DLL dependencies and must pass in every environment
    /// (local dev, CI, or any machine without the game installed).
    /// </summary>
    [TestFixture]
    public class PipelineSmokeTests
    {
        [Test]
        [Category("Smoke")]
        public void AlwaysPasses()
        {
            // If this test does not appear in the dotnet test output, the test
            // discovery pipeline is broken — check NUnit3TestAdapter and
            // Microsoft.NET.Test.Sdk package references in BelzontWE.Tests.csproj.
            Assert.Pass("Test pipeline is operational.");
        }

        [Test]
        [Category("Smoke")]
        public void TestFrameworkVersion_IsNUnit3()
        {
            string frameworkVersion = typeof(TestFixtureAttribute).Assembly.GetName().Version?.ToString() ?? "(unknown)";
            Assert.That(frameworkVersion, Does.StartWith("3."),
                $"Expected NUnit 3.x but found version {frameworkVersion}.");
        }
    }
}
