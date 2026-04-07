**End time:** 2026-04-07 00:52 -0300
**Start time:** 2026-04-07 00:49 -0300
# [0095] Implement actual tree navigation on while editing an item and the tree is not empty

**Developed by:** Agent-Claude-Sonnet-4.6 (agent@example.com)
## User Story

> Acting as **a player editing items in the WE component tree**, I want **to navigate the tree, fold/unfold items, and delete an item using keyboard shortcuts while the WE editor is open**, so that I **I can manage the layout tree efficiently without switching between keyboard and mouse**.

---

## Background

There is a commented-out incomplete implementation in `WEWorldPickerTool.cs` (~lines 52–54, 128, 241–242) that used Numpad +/- buttons with `kActionNextText` / `kActionPreviousText` ProxyActions to navigate by a flat `CurrentItemIdx` / `CurrentItemCount`. These bindings and that data model no longer exist in `WEWorldPickerController`.

The new navigation model replaces this with:
- **Page Up**: Move selection to the previous visible item (the item immediately above the current one in the rendered viewport order)
- **Page Down**: Move selection to the next visible item
- **Space**: Toggle fold/unfold on the selected item (no-op if it has no children)
- **Delete**: Show a deletion confirmation popup with NO default button selected — using the base game's `ConfirmationDialog` component. This popup is only triggered by the Delete key, NOT by the existing delete button in the UI.
- **Home**: Fold all tree items
- **End**: Unfold all tree items recursively

Fold/unfold state is **purely on the React frontend**: `expandedViewports: Entity[]` in `WETextHierarchyView.tsx`. The `K45HierarchyMenu` component receives `expanded` as a prop and calls `onSetExpanded` on click — but has no keyboard handler.

The `ConfirmationDialog` component from the base game UI is already imported in `WETextHierarchyView.tsx` (used for `alertToDisplay`). The deletion confirmation state needs a new pending-delete pathway that bypasses the direct `WorldPickerService.removeItem()` call used by the delete button.

Navigation must operate on the `viewport` array — the linearized visible tree built by the `ResumeToViewPort` recursive function — which already reflects fold state (collapsed nodes exclude their children from the viewport).

All keyboard handlers must be attached to a parent `div` in the React tree with `tabIndex={0}` to receive keyboard events. No `<span>` or `<input>` elements. Use base game components for confirmation dialogs. In least case if no component is available for a specific UI element, look for existing code since it was already validated and may have solutions outside game components using some strict HTML elements.

---

## Definition of Ready (DoR)

- [ ] `WETextHierarchyView.tsx` is read: `expandedViewports`, `viewport` array construction, `K45HierarchyMenu` component props, and existing `ConfirmationDialog` usage (`alertToDisplay`) are all located
- [ ] `WEWorldPickerTool.cs` commented-out navigation code is read — old ProxyAction names (`kActionNextText`, `kActionPreviousText`) are noted
- [ ] Confirmed that fold/unfold state is 100% frontend (React) — no C# binding needed for expand state
- [ ] Confirmed that `ConfirmationDialog` has `onConfirm`, `onCancel`, `confirm`, `cancel`, `message`, and no-default-button option
- [ ] Confirmed that the Delete key action must show the dialog; the UI delete button must NOT show it (two separate code paths)

---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] **Page Up**: pressing Page Up when the WE editor is open and an item is selected moves the selection to the previous item in viewport order; no-op if the first item is selected
- [ ] **Page Down**: pressing Page Down moves selection to the next item in viewport order; no-op if the last item is selected
- [ ] **Space**: pressing Space on a selected item with children toggles its fold state; no-op if the item has no children
- [ ] **Delete key**: pressing Delete shows a confirmation dialog using the base game `ConfirmationDialog`; the dialog has no default button selected; confirming deletes the item; cancelling dismisses without action
- [ ] **Existing UI delete button**: still deletes directly without a confirmation dialog
- [ ] **Home**: pressing Home collapses all tree items (sets `expandedViewports` to empty)
- [ ] **End**: pressing End recursively expands all tree items (sets `expandedViewports` to all entities that have children)
- [ ] All keyboard shortcuts are only active when the WE editor panel has focus (keyboard events are scoped to the tree container `div` with `tabIndex={0}`)
- [ ] No `<span>`, `<input>` or other raw HTML elements are introduced — only game-provided component types and `<div>` or components previously used in the WE codebase
- [ ] Commented-out C# Numpad +/- navigation code in `WEWorldPickerTool.cs` is either replaced by the new Page Up/Down mechanism or cleanly removed

---

## Implementation Notes

1. The commands must come from game backend, since they shall be customized following the game shortcuts system (registered in `WEModData.cs` as ProxyActions). The actions may send events to the React frontend (using current naming conventions) and then the React component handles the logic using the current `viewport` and `expandedViewports` state. **Do not use onKeyDown directly in the React component for these actions.**
2. Page Up/Down: find `currentIdx = viewport.findIndex(x => x.id.Index === wps.CurrentSubEntity.value?.Index)`, then call `WorldPickerService.setCurrentSubEntity(viewport[currentIdx - 1].id)` / `+1`. Clamp to bounds.
3. Space: if `viewport[currentIdx].children?.length > 0`, toggle membership of `viewport[currentIdx].id` in `expandedViewports`.
4. Home: `setExpandedViewports([])`.
5. End: collect all entity IDs that have `children.length > 0` in the full tree (not just visible viewport) and `setExpandedViewports(allParentIds)`.
6. Delete key: set a `pendingDelete: Entity | null` state; render `<ConfirmationDialog>` when `pendingDelete !== null` with `confirm` and `cancel` labels and no default focus. On confirm, call `WorldPickerService.removeItem(pendingDelete)` and clear state.
7. The existing delete button's `onClick` should call `WorldPickerService.removeItem(item.id)` directly — unchanged from current behavior, no dialog.
8. For C# side: if task 0094 reuses `kActionNextText`/`kActionPreviousText`, this task must use distinct key bindings. Coordinate key assignment. Alternatively, implement navigation entirely in the React `onKeyDown` handler (Page Up/Down keys are not used as ProxyActions in WEModData currently — they are safe to capture in the frontend).

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|

---

## Related Tasks

### Depends on



### Is dependent for



## Related Tasks

- 0094

---
