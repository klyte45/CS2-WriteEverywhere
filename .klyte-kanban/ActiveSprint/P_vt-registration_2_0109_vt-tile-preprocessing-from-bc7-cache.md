**Start time:** 2026-04-15 05:32 -0300
# [0109] vt-tile-preprocessing-from-bc7-cache

**Developed by:** Claude Sonnet 4.6 (claude-sonnet-4-6@kwytco.com.br)
## User Story

> Acting as **the atlas system**, I want **to convert raw BC7 atlas data into VT-compatible tiled format using AtlassingUtils.PreProcessData**, so that I **the tile data is ready for registration into the game's VT streaming system**.

---

## Background

The game's VT system requires textures in a tiled, padded BC7 format. AtlassingUtils.PreProcessData (Colossal.IO.AssetDatabase, managed/Burst, runtime-callable) takes raw BC7 byte buffers and reorganizes them into 512px tiles with 8px overlap padding. The BC7 data already produced by the Phase 1 disk cache is directly compatible as input. Each of the 5 atlas layers must be preprocessed independently, with correct GraphicsFormat (BC7_SRGB for main/emissive, BC7_UNorm for normal/mask/control). See 07_GameBC7ImportPipelineResearch.md Section 6 and 01_VTArchitectureAndPipeline.md for reference.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] WEAtlasVTUtils.PreprocessForVT wraps AtlassingUtils.PreProcessData correctly
- [ ] All 5 layer formats are correctly mapped (sRGB vs UNorm)
- [ ] Output tile data follows the game's expected tile layout (512px tiles, 8px padding)
- [ ] NativeArray memory is properly managed (no leaks)
- [ ] Unit test: known BC7 input produces expected tile count and byte sizes

---

## Implementation Notes

1. Create a new utility class WEAtlasVTUtils with a method PreprocessForVT(byte[] bc7Data, int width, int height, GraphicsFormat format) that wraps AtlassingUtils.PreProcessData
2. LayerInfo constructor takes (tileSize=512, format) — blockWidth=4, blockHeight=4, blockSize=16 for BC7
3. Process all 5 layers: main (BC7_SRGB), emissive (BC7_SRGB), control (BC7_UNorm), mask (BC7_UNorm), normal (BC7_UNorm)
4. Return the preprocessed NativeArray<byte> per layer for consumption by the registration task
5. Handle NativeArray lifecycle carefully — caller must Dispose after upload

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| AtlassingUtils.PreProcessData may have undocumented requirements on input alignment or mipmap presence | Medium | Test with various atlas sizes (512x512 through 4096x4096); compare output layout with game's own VTTextureAsset output |

---

## Related Tasks

### Depends on



### Is dependent for


