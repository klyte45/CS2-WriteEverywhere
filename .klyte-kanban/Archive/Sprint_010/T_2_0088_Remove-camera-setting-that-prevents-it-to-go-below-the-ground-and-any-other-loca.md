**End time:** 2026-04-06 23:49 -0300
**Start time:** 2026-04-06 23:43 -0300
# [0088] Remove camera setting that prevents it to go below the ground (and any other location restriction) when it's focused in an WE layout

**Developed by:** Agent-Claude-Sonnet-4.6 (agent@example.com)
## User Story

> Acting as **a player or developer editing a WE layout that is located underground or inside a building**, I want **the WE editor camera to position itself without ground-clamping or terrain collision when focusing on a layout item**, so that I **I can inspect and edit text layouts in subterranean positions (tunnels, basements, underground stations) without the camera snapping to or stopping at terrain level**.

---

## Background

When the WE editor is active and the camera is locked to the selected layout (`CameraLocked = true`), `WEWorldPickerTool.OnUpdate` switches to the cinematic camera controller with `collisionsEnabled = false` and computes a pivot using the item world position (`CurrentItemMatrix.GetPosition()`). This already disables terrain collision correctly.

However, two restrictions remain:
1. The cinematic controller may still be internally clamping the Y position of the pivot when the game applies its own position safety bounds (camera Y floor from `CameraUpdateSystem`).
2. The camera transition is lerped by the cinematic controller instead of being set immediately, causing the camera to visibly slide into position — distracting while editing underground items.

The key code path is in `WEWorldPickerTool.cs` `OnUpdate` (~line 323), where `m_cameraSystem.cinematicCameraController.pivot` is assigned. The fix is purely in this pivot-assignment path — no 'WECamera' class is needed.

---

## Definition of Ready (DoR)

- [ ] WEWorldPickerTool.OnUpdate camera lock section is read and understood (lines ~323–345)
- [ ] Confirmed that `collisionsEnabled = false` is already set — the remaining restriction is the pivot Y-clamping or lerp from the cinematic controller itself
- [ ] Identified the CinematicCameraController API: whether it exposes an `immediateTransition` flag or a `snapToTarget()` method that bypasses lerping

---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] Camera pivot is set without ground-clamping when CameraLocked is true — verified by placing a layout below terrain level and entering the WE editor
- [ ] Camera moves immediately (no lerp) to the focused item when locking — no visible slide-in animation
- [ ] All existing above-ground camera behaviors are unchanged
- [ ] Camera is restored correctly to the previous controller when the WE tool is deactivated
- [ ] No new classes or camera subclasses are introduced unless strictly required

---

## Implementation Notes

1. In `WEWorldPickerTool.OnUpdate`, after setting `m_cameraSystem.cinematicCameraController.pivot`, check whether the cinematic controller exposes a method for immediate snap (e.g. setting `pivot` and `rotation` on the same frame it becomes active may already bypass lerp — verify).
2. If a Y-floor clamp is applied by the cinematic controller internally, override it by calling the assignment with a forced position override (check if `CinematicCameraController` has a `snapToTarget` or `instantTransition` property).
3. If the game does not provide an API for immediate positioning, a one-frame workaround is to set the pivot N frames in advance of enabling the locked state, or to temporarily set `activeCameraController` only after forcibly positioning the pivot.

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|

---

## Related Tasks

### Depends on



### Is dependent for


