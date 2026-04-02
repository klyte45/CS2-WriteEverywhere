using System;
using System.IO;
using System.Reflection;

namespace BelzontWE.Tests.Fixtures
{
    /// <summary>
    /// Provides access to the SourceSansPro-Regular TTF font (OFL licence) that is
    /// already embedded in BelzontWE.dll under BelzontWE.Resources.SourceSansPro-Regular.ttf.
    /// No extra file is shipped with the test project — we reuse the production assembly's
    /// embedded resource to keep everything in sync.
    /// </summary>
    public static class TestFontFixture
    {
        private const string ResourceName = "BelzontWE.Resources.SourceSansPro-Regular.ttf";

        /// <summary>
        /// Returns the raw bytes of the test TTF font.
        /// Throws <see cref="InvalidOperationException"/> if the resource cannot be found,
        /// which would indicate either a broken build or a resource name change.
        /// </summary>
        public static byte[] GetTestFontBytes()
        {
            Assembly assembly = typeof(WriteEverywhereCS2Mod).Assembly;
            using Stream? stream = assembly.GetManifestResourceStream(ResourceName);
            if (stream is null)
                throw new InvalidOperationException(
                    $"Embedded resource '{ResourceName}' not found in {assembly.FullName}. " +
                    "Ensure BelzontWE.csproj still declares it as EmbeddedResource.");

            using MemoryStream ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }

        /// <summary>
        /// Returns a readable <see cref="Stream"/> for the test TTF font.
        /// The caller is responsible for disposing the stream.
        /// </summary>
        public static Stream OpenTestFontStream()
        {
            Assembly assembly = typeof(WriteEverywhereCS2Mod).Assembly;
            Stream? stream = assembly.GetManifestResourceStream(ResourceName);
            if (stream is null)
                throw new InvalidOperationException(
                    $"Embedded resource '{ResourceName}' not found in {assembly.FullName}.");
            return stream;
        }
    }
}
