# [0095] Implement actual tree navigation on while editing an item and the tree is not empty

**Developed by:** 

## User Story



---

## Background

There's an incomplete implementation in the code, at that time using Numpad +/- buttons to navigate between tree items. This time, the navigation keys will be:

- Page Up: Move selection to the immediate upper item on the tree, do nothing if first is selected
- Page Down: Move selection to the immediate lower item on the tree, do nothing if last is selected
- Space: Fold/unfold current item (if it have children, or do nothing if there are no child)
- Delete: Show a deletion confimation popup to the user, with NO default selected. NOTE: This popup shall not be shown when using the existing delete button on UI, only applies for the Delete key action.
- Home: Fold all tree
- End: Unfold all tree items recursively

Remember to use the components already available by the game, avoiding creating span or other kind of HTML default node - div is the only one accepted. Others components can be found o type reference from the base game UI.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)



---

## Implementation Notes



---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|

---

## Related Tasks

### Depends on



### Is dependent for


