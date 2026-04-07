# [0089] 'Paste copied layout as sibling' fails if the current selected object is on 3rd level or deeper

**Developed by:** 

## User Story

> Acting as **a player editing WE layouts with deep nesting (3 or more levels)**, I want **'Paste copied layout as sibling' to paste the copied item alongside the currently selected item at the correct depth**, so that I **I can build deeply-nested layouts using copy/paste without the pasted item being silently placed at the wrong tree level**.

---

## Background

The 'Paste as sibling' action in `WETextHierarchyView.tsx` calls `doPaste(currentParentNode)`, where `currentParentNode` is computed by `getParentNode(currentSubEntity, treeRoot)` — a recursive function that searches the nested `WETextItemResume` tree for the entity whose `children` array contains the currently selected sub-entity.

The bug is in the recursive fallback branch:
```typescript
return treeNode.children?.find(x => getParentNode(search, x))?.id
```
When the search target is more than 2 levels deep, this branch finds the first child `x` for which the recursive call returns a truthy value, then returns **`x.id`** — the intermediate node — not the result of the recursive call. For a tree `root → A → B → target`, looking for `target`'s parent returns `A.id` instead of `B.id`.

The hierarchy itself is managed in C# via `WESubTextRef` buffers. The UI receives a `WETextItemResume[]` tree via binding. 'Paste as sibling' calls `WorldPickerService.cloneAsChild(clipboard, currentParentNode)` — if `currentParentNode` is wrong, the clone ends up under the wrong parent.

---

## Definition of Ready (DoR)

- [ ] `WETextHierarchyView.tsx` paste logic is located and the `getParentNode` function is read in full
- [ ] Confirmed the bug: for a 3-level-deep selection the function returns the grandparent instead of the direct parent
- [ ] Confirmed `WorldPickerService.cloneAsChild(source, parent)` is the correct call and does not need changes

---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] Pasting a layout as sibling when the selected item is at depth 3 or deeper places the new item alongside the selected item (same parent), not at depth 2
- [ ] Pasting at depth 1 and depth 2 still works correctly (no regression)
- [ ] The fix is purely in the `getParentNode` function in `WETextHierarchyView.tsx` — no C# changes required
- [ ] Cut-and-paste (changeParent) is also tested at depth 3+ and behaves correctly

---

## Implementation Notes

1. Fix `getParentNode` in `WETextHierarchyView.tsx` so the recursive fallback returns the result of the recursive call, not the intermediate child's id:
2. ```typescript
const getParentNode = (search: Entity, treeNode: WETextItemResume): Entity | undefined => {
    if (treeNode.children?.some(x => x.id.Index == search.Index)) return treeNode.id;
    for (const child of treeNode.children ?? []) {
        const found = getParentNode(search, child);
        if (found) return found;
    }
    return undefined;
};
```
3. The old one-liner using `.find(x => ...).id` was the source of the bug — it conflated 'which child subtree contains the target' with 'what is the parent of the target'.

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|

---

## Related Tasks

### Depends on



### Is dependent for


