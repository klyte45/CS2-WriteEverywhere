**Start time:** 2026-03-31 22:59 -0300
# [0001] Font Atlas Copy-on-Expand

**Developed by:** Agent-Claude-Sonnet-4.6 <agent@example.com>
## Reference

Source: RefsLibrary/20260330_CodeStructureAnalysis/03_FontProcessing/02_ImprovementAnalysis.md — Area 1

## User Story

> Acting as **a player with a large city using Write Everywhere text entities across many Unicode character sets**, I want **the font atlas to expand without discarding cached glyph data**, so that I **I never see text flash back to a loading placeholder and the game does not stall during atlas resize events**.

---

## Background

When the font atlas texture is full and needs to grow, the current implementation destroys the old texture, creates a new larger one, resets all glyph AtlasGenerated flags, and invalidates the entire text render cache (m_textCache). All glyphs must be re-rasterized from scratch into the new texture, causing a visible frame spike and 1–2 frames where all WE text shows LOADING_PLACEHOLDER.

In cities with 500+ WE text entities using diverse Unicode characters, the initial atlas (1024×1024) fills up quickly, making this a recurring event.

---

## Definition of Ready (DoR)

- [ ] FontAtlas.cs is identified and its Reset() / expansion code path is located
- [ ] FontSystem atlas expansion call site is identified
- [ ] The skyline packing data structure (SkylineNode or equivalent) is understood well enough to know how to declare the pre-existing region as "occupied"
- [ ] Graphics.CopyTexture() compatibility with the Unity version used by CS2 is confirmed (format and size constraints)
- [ ] A test city with enough WE text to trigger at least one atlas expansion is available for manual validation

---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] When the atlas expands, the old pixel data is GPU-blitted into the top-left corner of the new texture using Graphics.CopyTexture() (no CPU pixel copy)
- [ ] The skyline packing state correctly treats the copied region as already occupied so no new glyph is placed over existing glyphs
- [ ] Existing FontGlyph UV coordinates are unchanged after expansion; no AtlasGenerated flags are cleared
- [ ] m_textCache entries with the old AtlasVersion remain valid (atlas version is only bumped when existing UVs are invalidated — which they no longer are after a copy-expand)
- [ ] No visible LOADING_PLACEHOLDER flash occurs during atlas expansion
- [ ] No regression in glyph rendering quality or layout
- [ ] The mod compiles and loads without errors in CS2 v1.5.6

---

## Implementation Notes

1. In FontAtlas.cs, locate the expansion path (called when AddRect fails due to insufficient space)
2. Before destroying the old Texture2D: Create the new larger Texture2D at double the old size (capped at MAX_ATLAS_SIZE). Call Graphics.CopyTexture(oldAtlas, 0, 0, 0, 0, oldWidth, oldHeight, newAtlas, 0, 0, 0, 0) to blit pixel data
3. Update the skyline node list to insert a pre-occupied horizontal "shelf" covering the old width × old height region at y=0, so the bin-packer never places new rects there (they're already occupied by copied data)
4. Do not reset AtlasGenerated flags on any existing FontGlyph
5. Do not call m_textCache.Clear() — existing cache entries remain valid
6. Increase the AtlasVersion counter only if a future fallback path still requires a destructive reset (e.g., if the atlas hits MAX_ATLAS_SIZE and cannot grow further). When at max size, the old destructive behaviour is acceptable

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| Graphics.CopyTexture() format mismatch | Low | Verify texture format (TextureFormat.Alpha8 or R8) matches between old and new |
| Skyline state corruption causes glyph overlap | Medium | Add an assertion in debug build that no two glyphs share the same atlas pixel region |
| Max-size atlas still destructively resets | Low | Acceptable edge case; document in code comment |

---

## Related Tasks

### Depends on



### Is dependent for


