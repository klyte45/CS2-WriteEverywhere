# [0105] fix-bc7-rgba32-guard-cache-write

**Developed by:** 

## User Story

> Acting as **a player**, I want **the game to load without ArgumentException errors and have atlas cache files appear on disk**, so that I **the BC7 optimization works correctly on first load**.

---

## Background

Two bugs share the same root cause. (1) On savegame load: WETextureAtlas.Apply() calls CompressToBC7() on textures created via Texture2D.LoadImage() which can change format away from RGBA32 causing ArgumentException. Stack trace: Apply() <- imageLoadAction lambda <- OnUpdate(). (2) Cache files never written to disk: WriteBC7CacheAndReplaceTextures() in RegisterLocalAtlas is silently swallowed by its try-catch because CompressToBC7 throws the same RGBA32 exception for PNG-loaded textures from WEAtlasLoadingUtils.

There are aditional information from the Log. Look at `Logs\Mods_K45_WE.log` from AI Workspace for details.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] CompressToBC7 does not throw when source texture is not RGBA32 format
- [ ] CompressToBC7 creates a temporary RGBA32 copy before compressing non-RGBA32 textures
- [ ] Game loads previously saved game without ArgumentException in Apply()
- [ ] After LoadImagesFromLocalFoldersCoroutine runs, .cache.we.bc7 files appear in CACHED_VT_FOLDER
- [ ] Build compiles 0 errors, full test suite 0 failures

---

## Implementation Notes

1. In WEAtlasBC7Utils.CompressToBC7: before the unsafe block, check if source.format != TextureFormat.RGBA32.
2. If not RGBA32: create a temp Texture2D(w, h, RGBA32, false). Use MakeReadable to get source pixels then SetPixels on temp. Compress temp. Destroy(temp) after compress.
3. Do NOT throw for non-RGBA32 — convert silently.
4. Tests for CompressToBC7_NonRGBA32_ConvertsAndCompresses must be [Ignore] (Unity runtime required).

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|

---

## Related Tasks

### Depends on



### Is dependent for


