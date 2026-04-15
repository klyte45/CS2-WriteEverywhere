using NUnit.Framework;

namespace BelzontWE.Tests.Font.Sprites
{
    /// <summary>
    /// Tests for the smart checksum-based reload (T8).
    /// Tests that require game systems are ignored; pure .NET tests run normally.
    /// </summary>
    [TestFixture]
    public class WEAtlasesLibrarySmartReloadTests
    {
        [Test]
        [Ignore("Requires Unity game systems (WEAtlasesLibrary.Instance)")]
        public void Reload_UnchangedFolder_DoesNotDisposeOrRebuild()
        {
            // Arrange: load an atlas, record initial instance reference
            // Act: call LoadImagesFromLocalFoldersCoroutine() a second time
            // Assert: LocalAtlases[name] is the same atlas instance (no Dispose called)
        }

        [Test]
        [Ignore("Requires Unity game systems (WEAtlasesLibrary.Instance)")]
        public void Reload_ChangedFolder_RebuildsAtlas()
        {
            // Arrange: load atlas, then add a file to the folder
            // Act: reload
            // Assert: atlas instance is a new object (rebuilt)
        }

        [Test]
        [Ignore("Requires Unity game systems (WEAtlasesLibrary.Instance)")]
        public void Reload_NewFolder_CreatesAtlas()
        {
            // Arrange: load with N folders
            // Act: create a new folder, reload
            // Assert: LocalAtlases.ContainsKey(newAtlasName) == true
        }

        [Test]
        [Ignore("Requires Unity game systems (WEAtlasesLibrary.Instance)")]
        public void Reload_DeletedFolder_DisposesAtlas()
        {
            // Arrange: load atlas for folder, delete folder from disk
            // Act: reload
            // Assert: LocalAtlases.ContainsKey(deletedName) == false
        }
    }
}
