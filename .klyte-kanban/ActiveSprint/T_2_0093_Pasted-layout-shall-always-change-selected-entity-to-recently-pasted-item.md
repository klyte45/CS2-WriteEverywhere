**End time:** 2026-04-07 00:08 -0300
**Start time:** 2026-04-07 00:04 -0300
# [0093] Pasted layout shall always change selected entity to recently pasted item

**Developed by:** Agent-Claude-Sonnet-4.6 (agent@example.com)
## User Story

> Acting as **a player pasting a layout into the WE component tree**, I want **the newly pasted item to be automatically selected after paste**, so that I **I can immediately start editing the pasted item without having to manually find and click it in the tree**.

---

## Background

All paste operations in `WEWorldPickerController` — `CloneAsChild` (~line 302) and `ChangeParent` (~line 173) — end with `ReloadTree()` / `UpdateTree()` but do **not** update `CurrentSubEntity`. The pattern for selecting a new item does exist in `AddItem` (~line 334): `CurrentSubEntity.ChangeValueWithEffects(subref.m_weTextData); UpdateTree();`.

`WELayoutUtility.DoCreateLayoutItem(...)` returns the newly created `Entity`. This return value is currently discarded in both paste methods.

On the frontend side, `WETextHierarchyView.tsx` `doPaste` does not take any selection action after calling `WorldPickerService.cloneAsChild` or `WorldPickerService.changeParent` — it relies on the C# side to broadcast a selection change through the `CurrentSubEntity` binding.

---

## Definition of Ready (DoR)

- [ ] `WEWorldPickerController.CloneAsChild` is read — confirmed `DoCreateLayoutItem` return value is discarded
- [ ] `WEWorldPickerController.AddItem` is read — confirmed selection pattern: `CurrentSubEntity.ChangeValueWithEffects(newEntity)`
- [ ] `WELayoutUtility.DoCreateLayoutItem` signature and return type are confirmed as returning `Entity`

---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] After pasting a layout (both 'paste as child' and 'paste as sibling'), the newly pasted entity becomes the selected item in the tree
- [ ] After a cut-and-paste (ChangeParent), the moved entity is selected at its new position
- [ ] Selecting the pasted item does not cause a double-tree-reload (only one `UpdateTree` call)
- [ ] Pasting at any depth level (1, 2, 3+) correctly selects the new entity
- [ ] No regression: adding an item and pasting still behave differently in terms of undo/redo if applicable

---

## Implementation Notes

1. In `WEWorldPickerController.CloneAsChild`, capture the return value: `var newEntity = WELayoutUtility.DoCreateLayoutItem(...); ... CurrentSubEntity.ChangeValueWithEffects(newEntity); UpdateTree();` — replacing the current `ReloadTree()` call.
2. In `WEWorldPickerController.ChangeParent`, capture the new entity from the `CloneAsChild` call and ensure selection follows the clone (not the removed original).
3. If `DoCreateLayoutItem` uses an EntityCommandBuffer (deferred creation), the new entity may not be valid until playback — in that case, enqueue the selection change via `m_executionQueue` to run after the ECB replay.

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|

---

## Related Tasks

### Depends on



### Is dependent for


