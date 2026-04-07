# [0092] Changing the font quality or the initial texture size for fonts is showing garbled text until they get regenerated

**Developed by:** 

## User Story

> Acting as **a player who changes the font quality or initial texture size setting in WE options**, I want **all on-screen WE texts to re-render cleanly immediately after changing those settings**, so that I **I don't see garbled, cross-referenced sprite textures on text until waiting for the natural BRI regeneration cycle**.

---

## Background

When a player changes `FontQuality` or `StartTextureSizeFont` in `WEModData`, the setter calls `FontServer.Instance.OnChangeSizeParam()`. On the next `FontServer.OnUpdate`, this triggers `FontSystem.Reset()` for every loaded font.

`FontSystem.Reset()` creates a new `Texture2D` via `CurrentAtlas.Reset(width, height)`, clears `m_textCache`, and clears the per-font glyph/kerning data. However, it does **not** notify the ECS world that entities currently referencing BRIs (Basic Render Infos) built from the old atlas need to be re-rendered.

Each `WETextDataMesh` component on an entity holds a cached reference to a `PrimitiveRenderInformation` (BRI). The BRI internally holds:
- A reference to the atlas `Texture2D` in `handleCheck` (a `GCHandle` weak ref)
- UV coordinates and mesh data baked for the OLD atlas layout

After `Reset()`, the old `Texture2D` is destroyed or repopulated with a different glyph layout. Existing entities still use the old BRI, rendering with stale UV data against a changed atlas — producing garbled text. The correct resolution path (`WEWaitingRendering` trigger) is only invoked when the formula/text value changes, not on font reset.

The fix with fewest changes: When `FontSystem.Reset()` runs, add `WEWaitingRendering` to all entities that reference BRIs from the reset font. A broader option, for review: clear `m_bri` on `WETextDataMesh` components when the font changes, so entities re-enter the rendering pipeline automatically.

If a minimal-change fix is not feasible without significant architecture work, use this task to produce a `RefsLibrary` research file documenting the issue with full flow diagrams, GCHandle lifecycle, and recommended refactoring options for a larger future task.

---

## Definition of Ready (DoR)

- [ ] `FontSystem.Reset()` is read and the full list of state it clears is confirmed
- [ ] `PrimitiveRenderInformation` GCHandle usage is located and understood (handleCheck weak ref to atlas texture)
- [ ] `WETextDataMesh` component is read: confirmed it holds a BRI reference and a font name / atlas reference
- [ ] `WEWaitingRendering` add path is located — confirmed it causes `WEPostRendererSystem` to regenerate the BRI for an entity

---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] Option A (quick fix): After a font reset in `FontServer`, a `WEWaitingRendering` component is added to all entities whose `WETextDataMesh` references the reset font. All text re-renders without garbling within 1–2 frames of the settings change
- [ ] Option A: No crash or exception when the entity query runs during font reset
- [ ] Option A: The fix does not cause permanent re-render loops — `WEWaitingRendering` is only added once per reset event
- [ ] Option B (research only, if Option A requires architecture change): A `RefsLibrary/FontTextureInvalidation.md` file is created documenting the full cross-reference bug, GCHandle lifecycle, and 2–3 concrete fix approaches with pros/cons
- [ ] Project compiles without errors

---

## Implementation Notes

1. In `FontServer.ScheduleFontSystem` (or the reset trigger path), when `requiresUpdateParameter` is true, use an `EntityCommandBuffer` to add `WEWaitingRendering` to all entities with `WETextDataMesh` where `fontName == font.Name` (or unconditionally to all WE text entities, since all fonts are reset).
2. Alternatively, add a `fontVersion` int field to `FontSystem`. Bump it in `Reset()`. Store the captured version in `WETextDataMesh`. In `WEPostRendererSystem`, if `fontVersion != storedVersion`, treat the entity as waiting for re-render.
3. GCHandle note: `FontSystem.dataPointer = GCHandle.Alloc(data)` is a separate handle for the font data pointer (for Burst job access), freed in `Dispose()`. This is unrelated to the garbled-text bug — do not change it.
4. The `handleCheck` in `PrimitiveRenderInformation` is a Weak GCHandle to the atlas Texture2D. It can be used to detect stale BRIs (if `handleCheck.Target == null`, the texture was GC'd). This is an additional validity guard that could be cheaply checked in `WERendererSystem` as a defense-in-depth measure.

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|

---

## Related Tasks

### Depends on



### Is dependent for


