**End time:** 2026-04-07 00:49 -0300
**Start time:** 2026-04-07 00:41 -0300
# [0094] When array instancing is enabled, the player should be able to navigate between instances with the fixed camera

**Developed by:** Agent-Claude-Sonnet-4.6 (agent@example.com)
## User Story

> Acting as **a player editing a WE layout that uses array instancing**, I want **to navigate between individual array instances using keyboard and UI controls while the camera is locked to the editing target**, so that I **I can inspect and verify the position and appearance of each instance in the array without having to unlock the camera and manually pan to each one**.

---

## Background

Array instancing in WE is configured via `WETextDataTransform`: `uint3 ArrayInstancing` (count per axis, 1–100), `float3 ArrayInstancingGapMeters`, and `ArrayInstancingAxisOrder` enum (XYZ, XZY, YXZ, YZX, ZXY, ZYX). The per-instance world offset is computed in `WETemplateManager.EntityProcessing.cs` as a nested loop over Z×Y×X axes using `SpacingByAxisOrder` and `InstanceCountByAxisOrder`.

The existing fixed camera (`WEWorldPickerTool.OnUpdate`, camera lock section) targets the single entity's `CurrentItemMatrix.GetPosition()`. It has no awareness of which array instance to focus.

A prior incomplete implementation existed for flat-list item navigation (`m_nextText`/`m_prevText` ProxyActions with `kActionNextText` / `kActionPreviousText`). Those actions are registered in `WEModData` but their C# consumers are commented out in `WEWorldPickerTool.cs`.

This feature requires:
1. **C# binding**: A `CurrentInstanceIdx` `MultiUIValueBinding<int>` in `WEWorldPickerController`
2. **Camera pivot offset**: In `WEWorldPickerTool.OnUpdate`, when `CameraLocked` is true and the selected entity has `ArrayInstancing.x * y * z > 1`, compute the selected instance's offset using the same `SpacingByAxisOrder` logic and add it to the camera pivot
3. **Keyboard actions**: All bindings shall be registered as ProxyActions in `WEModData.cs` to allow for user re-binding in the game options. The default keys can be Numpad +/- for the Z axis (the most common one to have multiple instances), and ←→ for X and ↑↓ for Y. The actions will increment/decrement `CurrentInstanceIdx` accordingly, respecting the `ArrayAxisGrowthOrder` (e.g. for XYZ order: incrementing goes through X first, then Y, then Z). Navigating past the last instance on a non-full row should not crash or show a position outside the actual spawned instances.
4. **UI controls**: Add a new section below the Pivot section in `WEInstancingView.tsx` — only visible when any axis count > 1 — with `+/-` buttons per axis (X, Y, Z), each button disabled if that axis count ≤ 1
5. **Axis-order awareness**: The `CurrentInstanceIdx` must be decoded into per-axis indices respecting `ArrayAxisGrowthOrder` and the non-full-row edge case

All HTML elements must use game-provided components; no `<span>` or `<input>` raw HTML elements. Use component references from the base game UI type definitions. In least case if no component is available for a specific UI element, look for existing code since it was already validated and may have solutions outside game components using some strict HTML elements.

---

## Definition of Ready (DoR)

- [ ] `WETextDataTransform.ArrayInstancing`, `SpacingByAxisOrder`, `InstanceCountByAxisOrder`, and `ArrayInstancingAxisOrder` enum values are read and understood
- [ ] Instance offset computation loop in `WETemplateManager.EntityProcessing.cs` is read — the per-instance offset formula is extracted
- [ ] `WEInstancingView.tsx` current UI structure is read — the Pivot section location is identified
- [ ] The existing `kActionNextText` / `kActionPreviousText` ProxyActions in `WEModData` are confirmed as available and unbound
- [ ] WEWorldPickerTool camera lock pivot computation is understood — the point where the offset can be injected is identified

---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] A `CurrentInstanceIdx` `MultiUIValueBinding<int>` (initialized to 0) is added to `WEWorldPickerController`
- [ ] When `CameraLocked` is true and `ArrayInstancing` total count > 1, the camera pivot targets the selected instance's world position
- [ ] ProxyAction buttons with default keys being: ←→ = X axis; ↑↓ = Y Axis; Numpad +/- = Z Axis - `CurrentInstanceIdx` is incremented/decremented accordingly from the current mapping of M, N, O indices respecting `ArrayAxisGrowthOrder` (inverse mapping from linear index to per-axis indices)
- [ ] Navigating past the last instance on a non-full row does not crash or show a position outside the actual spawned instances
- [ ] The UI shows a new 'Instance navigation' section in `WEInstancingView.tsx` below the Pivot section, visible only when at least one axis count > 1
- [ ] Each axis has a pair of `+` / `-` buttons; a button is disabled when that axis count ≤ 1
- [ ] No `<span>`, `<input>` or other raw HTML elements are introduced — only game-provided component types and `<div>` or components previously used in the WE codebase
- [ ] When `ArrayInstancing` is (1,1,1), the feature is invisible and the camera behavior is unchanged
- [ ] Project compiles without errors

---

## Implementation Notes

1. Instance offset formula (from EntityProcessing.cs): for a linear index `i`, decode into (m, n, o) based on `ArrayAxisGrowthOrder`. For XYZ order: `m = i % X`, `n = (i / X) % Y`, `o = i / (X * Y)`. Other orders follow the same modular decomposition using `InstanceCountByAxisOrder`.
2. In `WEWorldPickerTool.OnUpdate`, after computing the base pivot from `CurrentItemMatrix.GetPosition()`, add `spacingX * m + spacingY * n + spacingZ * o` where spacings come from `SpacingByAxisOrder`.
3. The `kActionNextText` and `kActionPreviousText` ProxyActions are in usage context `K45_WE.Tool`. Re-bind them to increment/decrement `CurrentInstanceIdx`. Their current keyboard bindings (Numpad +/-) match the original incomplete implementation.
4. UI: in `WEInstancingView.tsx`, conditionally render the navigation section using `transform.ArrayInstancing.x > 1 || transform.ArrayInstancing.y > 1 || transform.ArrayInstancing.z > 1`. For each axis row: label, `-` button (disabled if axis count ≤ 1), current index display, `+` button (disabled if axis count ≤ 1).
5. Non-full row edge case: total spawned instances = `min(X*Y*Z, actual array size)`. Check the `WETemplateManager` entity-processing loop for the actual break condition — instances are not created beyond the total declared count.

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|

---

## Related Tasks

### Depends on



### Is dependent for


