**End time:** 2026-04-06 21:23 -0300
**Start time:** 2026-04-06 21:15 -0300
# [0087] Fix XZ plane camera view

**Developed by:** Leandro Klyte (klyte45@kwytco.com)
## User Story

> Acting as **a player**, I want **to see correct XZ plane orientation**, so that I **move the WE layout item accurately**.


---

## Background

It's currently 'Upside Down' due the Z axis orientation over the screen Y axis (it should be inverted - the top of screen should be the Z back part).

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [x] The camera view is corrected to show the XZ plane with the correct orientation (top of screen corresponds to positive Z direction)
- [x] All existing layout item movement and placement logic continues to work correctly with the new camera orientation
- [x] No new bugs are introduced in the layout editing functionality due to this change

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


