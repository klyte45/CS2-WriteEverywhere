# [0113] vt-deregistration-on-atlas-dispose

**Developed by:** 

## User Story

> Acting as **the atlas system**, I want **to release all VT resources (atlas slots, tile data, material bindings) when an atlas is disposed or modified**, so that I **VT atlas space is not leaked, allowing runtime atlas reload/modification without exhausting the global VT atlas**.

---

## Background

When a user reloads images or modifies atlases at runtime, the old atlas must be fully deregistered from VT before the new one is registered. This involves: releasing the reserved VT atlas rect, unregistering tile data, unbinding materials, and invalidating the affected region. The game's SurfaceAsset.UnregisterFromVT provides the deregistration pattern. Without proper deregistration, VT atlas slots leak and eventually prevent new registrations. See 03_AtlasVTActionPlan.md R9 critical constraints ('No cleanup API observed' — must investigate SurfaceAsset.UnregisterFromVT for the pattern).

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] DeregisterFromVT releases VT atlas rect slots for both stacks
- [ ] Tile data GUIDs are unregistered from VT cache
- [ ] Materials are unbound from VT (ENABLE_VT disabled)
- [ ] VT region is invalidated after deregistration
- [ ] IsVTRegistered is reset to false
- [ ] No VT atlas slot leaks on repeated atlas reload cycles
- [ ] Test: register → deregister → re-register cycle succeeds without errors
- [ ] Test: dispose atlas → verify VT slot is available for new reservation

---

## Implementation Notes

1. Add DeregisterFromVT(TextureStreamingSystem tss) method to WETextureAtlas
2. Reverse the registration: release texture rect via tss API (investigate SurfaceAsset.UnregisterFromVT for exact method names)
3. Unregister tile data GUIDs from VT cache
4. Unbind materials — disable ENABLE_VT keyword, remove atlas params, restore fallback textures if material will be reused
5. Invalidate the freed region so GPU stops requesting tiles
6. Call DeregisterFromVT from WETextureAtlas.Dispose(), and before re-registration when an atlas is rebuilt
7. Reset IsVTRegistered and clear stored VTAtlassingInfo
8. Integrate with WEAtlasesLibrary: deregister before dispose in LoadImagesFromLocalFoldersCoroutine, UnregisterModAtlas, ClearAtlasDict

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| No public release/free API observed for VT atlas rects — deregistration may not be fully possible | Medium | Investigate SurfaceAsset.UnregisterFromVT decompilation; if no release API exists, track slots internally and reuse on re-registration |
| Deregistering while GPU is still requesting tiles may cause rendering artifacts | Low | Invalidate region first, wait one frame, then deregister tile data |

---

## Related Tasks

### Depends on



### Is dependent for


