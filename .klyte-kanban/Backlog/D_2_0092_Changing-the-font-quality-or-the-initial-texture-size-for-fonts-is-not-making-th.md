# [0092] Changing the font quality or the initial texture size for fonts is showing garbled text until they get regenerated

**Developed by:** 

## User Story



---

## Background

After applying these actions, the word BRI's are seeming to point to the sprite images, like if a cross reference has happened. We had issues like that with GCHandle and we strongly suspects it's a similar issue, since some GCHandles are used on Font Systems and also on BRI. Waiting more frames, the texts are fixed. Ideally, the texts should be invalidated immeditiately after these actions, so that they get regenerated with the correct data.

This task is intended to do a quick fix with few changes. If some architecture change would be a better solution, so use this task to create a research file under the RefsLibrary explaining the issue and the possible solutions in details for future analysis.

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


