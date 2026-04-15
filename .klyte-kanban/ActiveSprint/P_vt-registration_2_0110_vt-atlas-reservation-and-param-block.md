**Start time:** 2026-04-15 05:47 -0300
# [0110] vt-atlas-reservation-and-param-block

**Developed by:** Claude Sonnet 4.6 (claude-sonnet-4-6@kwytco.com.br)
## User Story

> Acting as **the atlas system**, I want **to reserve rectangular regions in the game's VT atlas for each WE texture atlas**, so that I **the material can receive VT UV coordinates to sample from the global streaming atlas**.

---

## Background

Each WE atlas must reserve space in two VT stacks: Stack 0 (DefaultPVTStack, 4 layers: basecolor, maskmap, normalmap, +1) for main/normal/mask, and Stack 1 (ExtendedPVTStack, 1 layer) for emissive/control. TextureStreamingSystem.ReserveTextureRect(stackConfigIndex, width, height) returns VTAtlassingInfo with stackGlobalIndex and indexInStack. GetTextureParamBlock(atlassingInfo) returns VTTextureParamBlock with UV transform. Minimum atlas size is 512x512 (WE minimum is exactly 512x512, so no padding needed). See 01_VTArchitectureAndPipeline.md Phase 2 and 03_AtlasVTActionPlan.md R9.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] ReserveVTSpace successfully reserves space in both VT stacks
- [ ] VTAtlassingInfo and VTTextureParamBlock are correctly computed and stored
- [ ] Layer mapping matches the game's StackData.layerFormats order
- [ ] ExtendedPVTStack layer count is validated at runtime with graceful fallback
- [ ] IsVTRegistered flag is set after successful reservation
- [ ] Test: reserve → verify stackGlobalIndex and indexInStack are non-negative

---

## Implementation Notes

1. Add VT registration state fields to WETextureAtlas: IsVTRegistered, m_vtAtlasInfoStack0, m_vtAtlasInfoStack1, m_vtParamBlock0, m_vtParamBlock1
2. Method: ReserveVTSpace(TextureStreamingSystem tss) — calls tss.ReserveTextureRect for both stacks
3. Store returned VTAtlassingInfo and computed VTTextureParamBlock
4. Map layers: Stack 0 = main (layer 0) + normal (layer 1) + mask (layer 2); Stack 1 = emissive/control
5. Verify ExtendedPVTStack layer count at runtime — if only 1 layer, handle control vs emissive priority

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| ExtendedPVTStack may have only 1 layer, insufficient for both control and emissive | Medium | Check layerFormats.Length at runtime; if only 1 layer, keep control/emissive as direct texture bindings |
| ReserveTextureRect may fail if VT atlas is full | Low | Check return value; fall back to direct Texture2D binding if reservation fails |

---

## Related Tasks

### Depends on



### Is dependent for


