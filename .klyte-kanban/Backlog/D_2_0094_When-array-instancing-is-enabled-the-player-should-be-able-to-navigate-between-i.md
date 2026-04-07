# [0094] When array instancing is enabled, the player should be able to navigate between instances with the fixed camera

**Developed by:** 

## User Story



---

## Background

There's an incomplete implementation in the code, but it was for the text tree navigation. This task would work somehow like that, but it would change the matrix transform of the fixed camera to focus into a specific instance instead of the central position. This is a complex feature, since it will require:

- Action buttons with default keys being: ←→ = X axis; ↑↓ = Y Axis; Numpad +/- = Z Axis
- UI buttons: A new section below the Pivot section UI, that only will show if any array dimension is greater than 1. Each pair of +/- button shall only be enabled if that axis have an array items value > 2
- Instancing array logic centralized: I believe it's just on the part of the code that generate the arrays templates, but now the camera shall have access to the same logic to know where each instance will be - preferrable a single point for both.
- Shall be aware about special cases when the row/column of items is not full - sometimes even a row supporting 5 items by definition, in the screen it may have less, and the control shouldn't overflow it.
- Shall be aware about the axis filling order

The task only can be considered completed if all these items are covered.

Remember to use the components already available by the game, avoiding creating `span` or other kind of HTML default node - div is the only one accepted. Others components can be found o type reference from the base game UI.

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


