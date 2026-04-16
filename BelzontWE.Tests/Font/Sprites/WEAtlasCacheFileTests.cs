using BelzontWE.Font.Sprites;
using BelzontWE.Sprites;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using HeuristicMethod = MaxRectsBinPack.FreeRectChoiceHeuristic;

namespace BelzontWE.Tests.Font.Sprites
{
    [TestFixture]
    public class WEAtlasCacheFileTests
    {
        private string _tempDir = null!;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"WEAtlasCacheFile_{System.Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        // ── Constants ─────────────────────────────────────────────────────────

        [Test]
        public void MAGIC_IsExpectedValue()
            => Assert.That(WEAtlasCacheFile.MAGIC, Is.EqualTo(0x37434257u));

        [Test]
        public void FORMAT_VERSION_Is2()
            => Assert.That(WEAtlasCacheFile.FORMAT_VERSION, Is.EqualTo(2u));

        // ── Round-trip ────────────────────────────────────────────────────────

        [Test]
        public void WriteTo_ReadFrom_RoundTrip_AllFieldsMatch()
        {
            var cache = BuildSampleCache();
            var path = Path.Combine(_tempDir, "test.cache.we.bc7");

            cache.WriteTo(path);
            var loaded = WEAtlasCacheFile.ReadFrom(path);

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded!.Checksum, Is.EqualTo(cache.Checksum));
            Assert.That(loaded.Width, Is.EqualTo(cache.Width));
            Assert.That(loaded.Height, Is.EqualTo(cache.Height));
            Assert.That(loaded.Size, Is.EqualTo(cache.Size));
            Assert.That(loaded.Method, Is.EqualTo(cache.Method));
            Assert.That(loaded.Sprites.Count, Is.EqualTo(cache.Sprites.Count));
            Assert.That(loaded.Sprites[0].Name, Is.EqualTo(cache.Sprites[0].Name));
            Assert.That(loaded.Sprites[0].Flags, Is.EqualTo(cache.Sprites[0].Flags));
        }

        [Test]
        public void WriteTo_ReadFrom_LayerData_Preserved()
        {
            var layer = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
            var cache = BuildSampleCache(layerData: layer);
            var path = Path.Combine(_tempDir, "layer.cache.we.bc7");

            cache.WriteTo(path);
            var loaded = WEAtlasCacheFile.ReadFrom(path);

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded!.LayerBC7[0], Is.EqualTo(layer));
        }

        [Test]
        public void WriteTo_ReadFrom_NullLayer_RemainsNull()
        {
            var cache = BuildSampleCache(); // has null layers 1-4
            var path = Path.Combine(_tempDir, "nulllayer.cache.we.bc7");

            cache.WriteTo(path);
            var loaded = WEAtlasCacheFile.ReadFrom(path);

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded!.LayerBC7[1], Is.Null, "Emissive layer should be null");
        }

        // ── Magic validation ──────────────────────────────────────────────────

        [Test]
        public void ReadFrom_WrongMagic_ReturnsNull()
        {
            var path = Path.Combine(_tempDir, "corrupt.we.bc7");
            using (var w = new BinaryWriter(File.Create(path)))
            {
                w.Write(0xDEADBEEFu); // wrong magic
                w.Write(1u);          // version
            }

            var result = WEAtlasCacheFile.ReadFrom(path);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void ReadFrom_NonExistentFile_ReturnsNull()
        {
            var result = WEAtlasCacheFile.ReadFrom(Path.Combine(_tempDir, "doesnotexist.we.bc7"));
            Assert.That(result, Is.Null);
        }

        [Test]
        public void ReadFrom_TruncatedFile_ReturnsNull()
        {
            var path = Path.Combine(_tempDir, "truncated.we.bc7");
            File.WriteAllBytes(path, new byte[] { 0x57, 0x42, 0x43, 0x37 }); // just 4 bytes of magic

            var result = WEAtlasCacheFile.ReadFrom(path);
            Assert.That(result, Is.Null);
        }

        // ── WriteTo creates directory ─────────────────────────────────────────

        [Test]
        public void WriteTo_CreatesDirectoryIfAbsent()
        {
            var nested = Path.Combine(_tempDir, "sub", "dir");
            var path = Path.Combine(nested, "cache.we.bc7");
            Assert.That(Directory.Exists(nested), Is.False);

            BuildSampleCache().WriteTo(path);

            Assert.That(File.Exists(path), Is.True);
        }

        // ── RebuildRectsPack ──────────────────────────────────────────────────

        [Test]
        public void RebuildRectsPack_ReturnsPackWithOriginalDimensions()
        {
            var cache = BuildSampleCache();
            var path = Path.Combine(_tempDir, "repack.we.bc7");
            cache.WriteTo(path);
            var loaded = WEAtlasCacheFile.ReadFrom(path)!;

            var pack = loaded.RebuildRectsPack();

            Assert.That(pack.binWidth, Is.EqualTo(512));
            Assert.That(pack.binHeight, Is.EqualTo(512));
        }

        // ── Helper ────────────────────────────────────────────────────────────

        private static WEAtlasCacheFile BuildSampleCache(byte[]? layerData = null)
        {
            var pack = new MaxRectsBinPack(512, 512);
            var sprites = new List<WEAtlasCacheFile.CachedSprite>
            {
                new("sprite_a", new Rect(0, 0, 64, 64), WESpriteInfo.ExtraTexturesFlag.Emissive),
            };
            var layers = new byte[]?[]
            {
                layerData ?? new byte[WEAtlasBC7Utils.GetBC7SizeBytes(512, 512)],
                null, null, null, null
            };
            return new WEAtlasCacheFile(
                checksum: 0xABCD1234u,
                width: 512, height: 512, size: 19,
                method: HeuristicMethod.RectBestShortSideFit,
                rectsPack: pack,
                sprites: sprites.AsReadOnly(),
                layerBC7: layers);
        }
    }
}
