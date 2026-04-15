using NUnit.Framework;

namespace BelzontWE.Tests.Font.Sprites
{
    /// <summary>
    /// Tests for VT registration lifecycle integration in <see cref="BelzontWE.Sprites.WEAtlasesLibrary"/>.
    /// All tests require the game runtime (TextureStreamingSystem) and are marked [Ignore].
    /// </summary>
    [TestFixture]
    public class WEAtlasesLibraryVTLifecycleTests
    {
        // ── Local atlas lifecycle ──────────────────────────────────────────

        [Test, Ignore("Requires game runtime (TextureStreamingSystem + WEAtlasesLibrary)")]
        public void LocalAtlas_FromCache_IsVTRegisteredAfterLoad()
        {
            // Would verify: after LoadImagesFromLocalFolders with BC7 cache hit,
            // the resulting atlas has IsVTRegistered == true
        }

        [Test, Ignore("Requires game runtime (TextureStreamingSystem + WEAtlasesLibrary)")]
        public void LocalAtlas_FreshBuild_IsVTRegisteredAfterCacheWriteAndReload()
        {
            // Would verify: after fresh build → WriteBC7Cache → FromCacheFile,
            // the resulting atlas has IsVTRegistered == true
        }

        [Test, Ignore("Requires game runtime (TextureStreamingSystem + WEAtlasesLibrary)")]
        public void LocalAtlas_Reload_DeregistersOldBeforeRegisteringNew()
        {
            // Would verify: reload path disposes old (which deregisters VT),
            // then new atlas is VT-registered with fresh GUIDs
        }

        // ── Mod atlas lifecycle ────────────────────────────────────────────

        [Test, Ignore("Requires game runtime (TextureStreamingSystem + WEAtlasesLibrary)")]
        public void ModAtlas_FromCache_IsVTRegisteredAfterLoad()
        {
            // Would verify: mod atlas loaded from BC7 cache has IsVTRegistered == true
        }

        [Test, Ignore("Requires game runtime (TextureStreamingSystem + WEAtlasesLibrary)")]
        public void ModAtlas_FreshBuild_IsVTRegisteredAfterCacheWriteAndReload()
        {
            // Would verify: mod atlas fresh build → cache → reload → VT registered
        }

        [Test, Ignore("Requires game runtime (TextureStreamingSystem + WEAtlasesLibrary)")]
        public void ModAtlas_RegisterUnregisterCycle_NoVTLeak()
        {
            // Would verify: RegisterModAtlas → UnregisterModAtlas (disposes + deregisters)
            // → re-register → new atlas is VT-registered, old VT regions invalidated
        }

        // ── City atlas lifecycle ───────────────────────────────────────────

        [Test, Ignore("Requires game runtime (TextureStreamingSystem + WEAtlasesLibrary)")]
        public void CityAtlas_Deserialize_IsVTRegisteredAfterImageLoadAction()
        {
            // Would verify: after Deserialize + imageLoadAction execution,
            // city atlas has IsVTRegistered == true
        }

        [Test, Ignore("Requires game runtime (TextureStreamingSystem + WEAtlasesLibrary)")]
        public void CityAtlas_RemoveFromCity_DeregistersVT()
        {
            // Would verify: RemoveFromCity → Dispose → IsVTRegistered == false
        }

        // ── Full lifecycle ─────────────────────────────────────────────────

        [Test, Ignore("Requires game runtime (TextureStreamingSystem + WEAtlasesLibrary)")]
        public void FullLifecycle_LoadReloadDispose_NoOrphanedVTRegistrations()
        {
            // Would verify: load local atlases → reload (some changed, some gone)
            // → OnDestroy → no VT registrations remain (all invalidated)
        }

        [Test, Ignore("Requires game runtime (TextureStreamingSystem + WEAtlasesLibrary)")]
        public void OnDestroy_CleansUpAllThreeAtlasTypes()
        {
            // Would verify: after OnDestroy, all local/city/mod atlases are
            // disposed and VT-deregistered
        }
    }
}
