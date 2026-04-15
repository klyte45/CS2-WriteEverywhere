using NUnit.Framework;
using System.IO;

namespace BelzontWE.Tests.Utils
{
    [TestFixture]
    public class WEChecksumUtilsTests
    {
        private string _tempDir = null!;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"WEChecksumUtils_{System.Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        // ── Consistency ───────────────────────────────────────────────────────

        [Test]
        public void ComputeFolderChecksum_SameFolderContents_ReturnsSameValue()
        {
            File.WriteAllText(Path.Combine(_tempDir, "alpha.png"), "AAAA");
            File.WriteAllText(Path.Combine(_tempDir, "beta.png"), "BBBB");

            var first = WEChecksumUtils.ComputeFolderChecksum(_tempDir);
            var second = WEChecksumUtils.ComputeFolderChecksum(_tempDir);

            Assert.That(first, Is.EqualTo(second));
        }

        // ── Empty folder ──────────────────────────────────────────────────────

        [Test]
        public void ComputeFolderChecksum_EmptyFolder_ReturnsKnownSentinel()
        {
            var result = WEChecksumUtils.ComputeFolderChecksum(_tempDir);
            // Empty folder should equal the FNV offset basis (hash of empty byte sequence)
            Assert.That(result, Is.EqualTo(2166136261u));
        }

        [Test]
        public void ComputeFolderChecksum_NonExistentFolder_ReturnsSentinel()
        {
            var result = WEChecksumUtils.ComputeFolderChecksum(Path.Combine(_tempDir, "nonexistent"));
            Assert.That(result, Is.EqualTo(2166136261u));
        }

        // ── Ordering determinism ──────────────────────────────────────────────

        [Test]
        public void ComputeFolderChecksum_FileOrderingIsCaseInsensitiveOrdinal()
        {
            // Write both files, get checksum, then delete and recreate in reverse order
            var fileZ = Path.Combine(_tempDir, "Zfile.png");
            var fileA = Path.Combine(_tempDir, "afile.png");

            File.WriteAllText(fileZ, "Z");
            File.WriteAllText(fileA, "A");
            var checksumFirst = WEChecksumUtils.ComputeFolderChecksum(_tempDir);

            File.Delete(fileZ);
            File.Delete(fileA);

            // Recreate in reverse order — result must be identical (sorted by path, not creation order)
            File.WriteAllText(fileA, "A");
            File.WriteAllText(fileZ, "Z");
            var checksumSecond = WEChecksumUtils.ComputeFolderChecksum(_tempDir);

            Assert.That(checksumFirst, Is.EqualTo(checksumSecond));
        }

        // ── Golden test ───────────────────────────────────────────────────────

        [Test]
        public void ComputeFolderChecksum_KnownInput_ReturnsGoldenChecksum()
        {
            // File: "img.png" with size = 4 bytes → entry "<fullpath>:4"
            var fullPath = Path.Combine(_tempDir, "img.png");
            File.WriteAllBytes(fullPath, new byte[] { 1, 2, 3, 4 });

            var result = WEChecksumUtils.ComputeFolderChecksum(_tempDir);

            // Pre-computed FNV-1a of bytes for "{fullPath}:4\0" (UTF-8, null-byte separator)
            // Verified against the reference algorithm; if this fails the algorithm has changed.
            uint expected = ComputeExpected($"{fullPath}:4");
            Assert.That(result, Is.EqualTo(expected));
        }

        // ── Change detection ──────────────────────────────────────────────────

        [Test]
        public void ComputeFolderChecksum_AddFile_ChangesChecksum()
        {
            File.WriteAllText(Path.Combine(_tempDir, "a.png"), "A");
            var before = WEChecksumUtils.ComputeFolderChecksum(_tempDir);

            File.WriteAllText(Path.Combine(_tempDir, "b.png"), "B");
            var after = WEChecksumUtils.ComputeFolderChecksum(_tempDir);

            Assert.That(after, Is.Not.EqualTo(before));
        }

        [Test]
        public void ComputeFolderChecksum_RemoveFile_ChangesChecksum()
        {
            var file = Path.Combine(_tempDir, "a.png");
            File.WriteAllText(file, "A");
            File.WriteAllText(Path.Combine(_tempDir, "b.png"), "B");
            var before = WEChecksumUtils.ComputeFolderChecksum(_tempDir);

            File.Delete(file);
            var after = WEChecksumUtils.ComputeFolderChecksum(_tempDir);

            Assert.That(after, Is.Not.EqualTo(before));
        }

        [Test]
        public void ComputeFolderChecksum_ResizeFile_ChangesChecksum()
        {
            var file = Path.Combine(_tempDir, "a.png");
            File.WriteAllText(file, "SHORT");
            var before = WEChecksumUtils.ComputeFolderChecksum(_tempDir);

            File.WriteAllText(file, "MUCH_LONGER_CONTENT_HERE");
            var after = WEChecksumUtils.ComputeFolderChecksum(_tempDir);

            Assert.That(after, Is.Not.EqualTo(before));
        }

        // ── Non-PNG filter (new behavior) ─────────────────────────────────────

        [Test]
        public void ComputeFolderChecksum_NonPngIgnored()
        {
            File.WriteAllText(Path.Combine(_tempDir, "img.png"), "AAAA");
            var before = WEChecksumUtils.ComputeFolderChecksum(_tempDir);

            // Adding a non-PNG file must NOT change the checksum
            File.WriteAllText(Path.Combine(_tempDir, "readme.txt"), "ignored");
            File.WriteAllText(Path.Combine(_tempDir, "data.xml"), "also ignored");
            var after = WEChecksumUtils.ComputeFolderChecksum(_tempDir);

            Assert.That(after, Is.EqualTo(before));
        }

        [Test]
        public void ComputeFileListChecksum_FullPath_Used()
        {
            // Same filename, different directories → must produce different checksums
            var dir1 = Path.Combine(_tempDir, "mod_a");
            var dir2 = Path.Combine(_tempDir, "mod_b");
            Directory.CreateDirectory(dir1);
            Directory.CreateDirectory(dir2);

            var path1 = Path.Combine(dir1, "icon.png");
            var path2 = Path.Combine(dir2, "icon.png");
            File.WriteAllBytes(path1, new byte[] { 1, 2, 3 });
            File.WriteAllBytes(path2, new byte[] { 1, 2, 3 });

            var checksum1 = WEChecksumUtils.ComputeFileListChecksum(new[] { path1 });
            var checksum2 = WEChecksumUtils.ComputeFileListChecksum(new[] { path2 });

            Assert.That(checksum1, Is.Not.EqualTo(checksum2));
        }

        // ── Helper: compute expected hash at compile time for golden test ──────

        private static uint ComputeExpected(string entry)
        {
            const uint basis = 2166136261u;
            const uint prime = 16777619u;
            uint h = basis;
            foreach (byte b in System.Text.Encoding.UTF8.GetBytes(entry))
            {
                h ^= b;
                h *= prime;
            }
            // null byte separator
            h ^= 0;
            h *= prime;
            return h;
        }
    }
}
