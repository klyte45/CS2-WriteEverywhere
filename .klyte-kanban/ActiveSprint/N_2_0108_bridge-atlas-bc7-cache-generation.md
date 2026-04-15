# [0108] bridge-atlas-bc7-cache-generation

**Developed by:** 

## User Story

> Acting as **a mod developer using ImageManagementBridge**, I want **my registered atlases to also benefit from BC7 caching**, so that I **my mod's images load faster on subsequent game starts without full re-encode**.

---

## Background

Only local image folder atlases write BC7 cache files. Atlases registered via ImageManagementBridge (RegisterImageAtlas with file paths, RegisterImageAtlasFromMemory with producer) go through EnqueueModAtlasLoader -> RegisterAtlas(ModAtlases) with no cache write. Both registration paths need WriteBC7CacheAndReplaceTextures after RegisterAtlas. Checksum strategy: file-based uses ComputeFileListChecksum(paths); memory-based uses ComputeBridgeMemoryChecksum(producer()). LoadImagesFromModsCoroutine should skip rebuild when cached file checksum matches (smart reload for mod atlases).

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] RegisterImageAtlas (file-based) writes a .cache.we.bc7 after atlas build
- [ ] RegisterImageAtlasFromMemory (producer-based) writes a .cache.we.bc7 after atlas build
- [ ] LoadImagesFromModsCoroutine skips rebuild for mod atlases with matching checksum
- [ ] m_modAtlasChecksums tracks checksums per mod atlas key
- [ ] Test: bridge cache lifecycle (Ignored - Unity required)
- [ ] Build compiles 0 errors, full test suite 0 failures

---

## Implementation Notes

1. In WEAtlasesLibrary.EnqueueModAtlasLoader: add a Func<uint> checksumFactory parameter. Call it after RegisterAtlas succeeds, then call atlas.WriteBC7CacheAndReplaceTextures(cacheFilePath, checksum) in a try-catch.
2. For LoadImagesToAtlas (file paths): pass checksumFactory = () => WEChecksumUtils.ComputeFileListChecksum(imagePaths).
3. For LoadImagesAsDynamicAtlas (producer): pass checksumFactory = () => WEChecksumUtils.ComputeBridgeMemoryChecksum(producer()).
4. Cache file path: Path.Combine(CACHED_VT_FOLDER, atlasName + '.cache.we.bc7') where atlasName is the full WEModIntegrationUtility key.
5. In LoadImagesFromModsCoroutine: before calling registerCallback, attempt ReadFrom cache, compare checksum; if match load from cache and skip rebuild.
6. Add m_modAtlasChecksums dictionary analogously to m_localAtlasChecksums.

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|

---

## Related Tasks

### Depends on

- [0105]
- [0106]
### Is dependent for

---
