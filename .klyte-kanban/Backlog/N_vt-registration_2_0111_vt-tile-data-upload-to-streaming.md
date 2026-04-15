# [0111] vt-tile-data-upload-to-streaming

**Developed by:** 

## User Story

> Acting as **the atlas system**, I want **to upload preprocessed VT tile data into the game's VT streaming cache**, so that I **the GPU can stream tiles on-demand for WE atlas textures, reducing VRAM usage**.

---

## Background

After reserving VT space and preprocessing tile data, the actual BC7 tile bytes must be fed into the VT system. The game uses RegisterVTTextureData(guid, dataSize) to allocate a buffer, then copies tile data into it via GetTextureData(guid), followed by DoneLoading(guid). AddTextureToCache uploads the high-mip tile data for immediate visibility. Finally, InvalidateRegion forces the GPU to re-request tiles. See 01_VTArchitectureAndPipeline.md Phase 2 and 03_AtlasVTActionPlan.md R9 Path B step 3.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] Tile data is uploaded via RegisterVTTextureData/GetTextureData/DoneLoading pipeline
- [ ] AddTextureToCache registers high-mip tiles for immediate visibility
- [ ] InvalidateRegion triggers GPU to request fresh tiles
- [ ] All 5 atlas layers are uploaded to their respective VT stack layers
- [ ] No native memory leaks (NativeArray disposal verified)
- [ ] Test: upload known tile data → verify via VT system that tiles are available

---

## Implementation Notes

1. Create UploadTilesToVT(TextureStreamingSystem tss, VTAtlassingInfo atlasInfo, int layerIndex, byte[] tileData, Guid tileGuid)
2. Call tss.RegisterVTTextureData(tileGuid, tileData.Length)
3. Copy tileData into tss.GetTextureData(tileGuid) NativeArray
4. Call tss.DoneLoading(tileGuid)
5. Call tss.AddTextureToCache(atlasInfo.stackGlobalIndex, layerIndex, atlasInfo.indexInStack, width, height, tileGuid, 0) for high-mip visibility
6. Call tss.InvalidateRegion(atlasInfo.stackGlobalIndex, atlasInfo.indexInStack) to trigger GPU tile refresh
7. Perform upload for all 5 layers across both stacks

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| RegisterVTTextureData/GetTextureData API may have thread affinity requirements (main thread only) | Medium | Ensure all VT calls execute on main thread; use action queue if needed |
| Tile GUID management may conflict with game's own VT registrations | Low | Use deterministic GUIDs derived from atlas name + layer to avoid collisions; namespace with WE prefix |

---

## Related Tasks

### Depends on



### Is dependent for


