using NUnit.Framework;
using Unity.Collections;

namespace BelzontWE.Tests.Systems
{
    [TestFixture]
    public class WEGamePropCleanupSystemTests
    {
        // ── WEChild + WEOwner contract: serialization split ensures stale detection ──

        [Test]
        public void WEChild_IsSerialized_ForStaleDetection()
        {
            // WEChild must implement IEmptySerializable so it persists across save/load
            // — required to detect post-load stale orphans (WEChild present, WEOwner absent).
            Assert.That(typeof(WEChild).GetInterface("IEmptySerializable"), Is.Not.Null);
        }

        [Test]
        public void WEOwner_IsNotSerialized_SoLoadedOrphansHaveNoOwner()
        {
            // WEOwner must NOT serialize — after load, spawned props have WEChild but no WEOwner.
            Assert.That(typeof(WEOwner).GetInterface("IEmptySerializable"), Is.Null);
            Assert.That(typeof(WEOwner).GetInterface("ISerializable"), Is.Null);
        }

        // ── WEGamePropLastSpawned ─────────────────────────────────────────

        [Test]
        public void WEGamePropLastSpawned_DefaultValue_IsEmpty()
        {
            var c = new WEGamePropLastSpawned();
            Assert.That(c.LastSpawnedPrefabName.Length, Is.EqualTo(0));
        }

        [Test]
        public void WEGamePropLastSpawned_CanStorePrefabName()
        {
            var c = new WEGamePropLastSpawned { LastSpawnedPrefabName = new FixedString128Bytes("my_prefab") };
            Assert.That(c.LastSpawnedPrefabName.ToString(), Is.EqualTo("my_prefab"));
        }
    }
}
